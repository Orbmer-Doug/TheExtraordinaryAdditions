// Global variables
float Time : register(c0);
float2 SunPosition : register(c1); // Sun position in screen coordinates (0 to 1)
float2 MoonPosition : register(c2); // Moon position in screen coordinates (0 to 1)
float4 SkyColor : register(c3);
float2 Parallax : register(c4);
bool GravDir : register(c5);
bool IsDay : register(c6);
float2 ScreenRes : register(c7);

// Simple 2D noise function
float random(float2 st)
{
    return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
}

// Smooth noise interpolation
float noise(float2 st)
{
    float2 i = floor(st);
    float2 f = frac(st);

    float a = random(i);
    float b = random(i + float2(1.0, 0.0));
    float c = random(i + float2(0.0, 1.0));
    float d = random(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(lerp(a, b, u.x),
                lerp(c, d, u.x),
                u.y);
}

// 4 looks really good, but it is a little bit too much on performance
#define NUM_OCTAVES 3

static const float2x2 rot = float2x2(0.877583, 0.479426, -0.479426, 0.877583);
float fbm(float2 noiseCoord)
{
    float value = 0.0;
    float amplitude = 0.5;
    float2 shift = float2(100.0, 100.0);
    
    for (int i = 0; i < NUM_OCTAVES; ++i)
    {
        value += amplitude * noise(noiseCoord);
        noiseCoord = mul(rot, noiseCoord) * 2.0 + shift;
        amplitude *= 0.5;
    }
    return value;
}

// Raymarching for light rays with noise to reduce banding
float raymarchLight(float2 uv, float2 lightDir)
{
    float density = 0.0;
    float stepSize = 0.02;
    int steps = 11;
    float2 rayPos = uv;
    
    for (int i = 0; i < steps; i++)
    {
        rayPos += lightDir * stepSize;
        if (rayPos.x < 0.0 || rayPos.x > 1.0 || rayPos.y < 0.0 || rayPos.y > 1.0)
            break;
        
        float cloudDensity = fbm(rayPos * 2.0 + float2(Time * 0.02, 0.0));
        cloudDensity = pow(cloudDensity, 2.0);
        cloudDensity = saturate(cloudDensity * 2.0);
        
        float noiseOffset = noise(rayPos * 10.0) * 0.05;
        density += exp(-cloudDensity * 5.0) * (stepSize + noiseOffset);
        
        if (density >= 1.0)
            break;
    }
    
    return saturate(density * 2.0);
}

float calculateShadow(float2 uv, float2 shadowDir)
{
    float shadow = 0.0;
    float stepSize = 0.02;
    float steps = 10.0;
    float2 rayPos = uv;
    
    for (int i = 0; i < int(steps); i++)
    {
        rayPos += shadowDir * stepSize;
        if (rayPos.x < 0.0 || rayPos.x > 1.0 || rayPos.y < 0.0 || rayPos.y > 1.0)
            break;
        
        float cloudDensity = fbm(rayPos * 2.0 + float2(Time * 0.02, 0.0));
        cloudDensity = pow(cloudDensity, 2.0);
        cloudDensity = saturate(cloudDensity * 2.0);
        shadow += cloudDensity * stepSize;
        
        if (shadow >= 1.0)
            break;
    }
    
    return saturate(shadow * 2.0);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    coords.x -= coords.x % (1 / float(ScreenRes.x / 4));
    coords.y -= coords.y % (1 / float(ScreenRes.y / 4));
    
    // Gravity flip thingys
    float2 adjustedTexCoord = coords;
    adjustedTexCoord.y = lerp(adjustedTexCoord.y, 1 - adjustedTexCoord.y, GravDir);
   
    float px = Parallax.x * .009;
    float py = Parallax.y * .009;
    
    float pinchAmount = 0.7;
    float2 center = float2(0.5, 0.5);
    float2 offset = adjustedTexCoord - center;
    float dist = length(offset);
    float pinchFactor = 1.0 - pinchAmount * (dist * dist); // Quadratic pinch toward center
    float2 pinchedCoord = center + offset * pinchFactor;
    pinchedCoord *= 20.0; // Apply scale after pinching
    pinchedCoord += float2(Time * 2.0 + px, py);
    float2 noiseCoord = pinchedCoord;
    
    float3 color = float3(0.0, 0.0, 0.0);

    // First layer of fBm
    float2 baseNoiseLayer = float2(0.0, 0.0);
    float height = 1 - Time * .0006;
    baseNoiseLayer.x = fbm(noiseCoord + 0.20 * Time * height);
    baseNoiseLayer.y = fbm(noiseCoord + float2(1.0, 0.0) * height);
    
    // Second layer of fBm
    float2 detailNoiseLayer = float2(0.0, 0.0);
    detailNoiseLayer.x = fbm(noiseCoord + 0.5 * baseNoiseLayer + float2(1.7, 9.2) + 0.1 * Time * height);
    detailNoiseLayer.y = fbm(noiseCoord + 0.5 * baseNoiseLayer + float2(8.3, 2.8) + 0.08 * Time * height);

    // Final fBm layer
    float finalCloudDensity = fbm(noiseCoord + detailNoiseLayer);

    // Increase contrast for some cloud shapes
    finalCloudDensity = pow(finalCloudDensity, 2.0);
    finalCloudDensity = saturate(finalCloudDensity * 2.0);

    // Color mixing for clouds with day/night adjustment
    float3 color1 = lerp(float3(0.1, 0.1, 0.15), float3(0.2, 0.2, 0.25), IsDay); // Darker at night
    float3 color2 = lerp(float3(0.4, 0.4, 0.5), float3(0.6, 0.6, 0.65), IsDay); // Cooler at night
    float3 color3 = lerp(float3(0.05, 0.05, 0.1), float3(0.1, 0.1, 0.15), IsDay); // Very dark, cooler at night
    float3 color4 = lerp(float3(0.6, 0.7, 0.8), float3(0.8, 0.8, 0.85), IsDay); // Bluish at night

    color = lerp(color1, color2, finalCloudDensity);
    float density = saturate(length(baseNoiseLayer) * 1.5);
    color = lerp(color, color3, density);
    color = lerp(color, color4, saturate(detailNoiseLayer.x * 0.5));

    float2 adjustedSunPosition = SunPosition;
    float2 adjustedMoonPosition = MoonPosition;
    adjustedSunPosition.y = lerp(adjustedSunPosition.y, 1 - adjustedSunPosition.y, GravDir);
    adjustedMoonPosition.y = lerp(adjustedMoonPosition.y, 1 - adjustedMoonPosition.y, GravDir);
    
    // Determine light source based on day/night
    float2 lightPosition = lerp(adjustedMoonPosition, adjustedSunPosition, IsDay);
    float2 lightDir = normalize(adjustedTexCoord - lightPosition);
    float lightIntensity = raymarchLight(adjustedTexCoord, lightDir);
    
    float atmo = clamp(SkyColor.a, .75, 2);
    float3 sky = clamp(SkyColor.rgb, float3(.6, .6, .6), float3(2, 2, 2));
    
    // Adjust light color based on day/night
    float3 lightColor = lerp(float3(0.8, 0.9, 1.0) * 2.5 * atmo, sky * 2.0 * atmo, IsDay); // Bluish for moon, warm for sun
    color = lerp(color, color + lightColor * lightIntensity, 0.5 * (1.1 - density));

    // Calculate shadows based on light source
    float2 shadowDir = -lightDir;
    float shadow = calculateShadow(adjustedTexCoord, shadowDir);
    float shadowStrength = 1.1;
    color *= 1.0 - shadowStrength * shadow;

    // Add glowing light source behind clouds
    float distToLight = length(adjustedTexCoord - lightPosition);
    float lightGlow = saturate(1.0 - distToLight * 2.0);
    lightGlow = pow(lightGlow, 7.0);
    float3 lightGlowColor = lerp(float3(0.8, 0.9, 1.0) * 2.6 * atmo, sky * 3.0 * atmo, IsDay); // Bluish for moon, warm for sun
    float cloudOcclusion = 1.0 - (density * 0.5); // Reuse a fixed opacity for consistency
    color = lerp(color, color + lightGlowColor * lightGlow, saturate(cloudOcclusion));
    
    return float4(saturate(color), 1.0) * sampleColor ;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}