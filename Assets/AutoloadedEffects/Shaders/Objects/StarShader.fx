float globalTime : register(c0);
float3 coreColor : register(c1);
float3 coronaColor : register(c2);

float coolnessInterpolant : register(c3);
float redness : register(c4);

float2 resolution = float2(500, 500);
sampler2D iChannel0 : register(s0);
static const float TwoPi = 6.28318530718;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float simplexNoise(float3 coordinates, float resolution)
{
    const float3 scale = float3(1.0, 10.0, 100000.0);
    
    coordinates *= resolution;
    
    // Calculate grid cell coordinates
    float3 cell0 = floor(fmod(coordinates, resolution)) * scale;
    float3 cell1 = floor(fmod(coordinates + float3(1.0, 1.0, 1.0), resolution)) * scale;
    
    // Fractional part for interpolation
    float3 fract = frac(coordinates);
    fract = fract * fract * (3.0 - 2.0 * fract); // Smoothstep interpolation
    
    // Generate four corner noise values
    float4 corners = float4(
        cell0.x + cell0.y + cell0.z,
        cell1.x + cell0.y + cell0.z,
        cell0.x + cell1.y + cell0.z,
        cell1.x + cell1.y + cell0.z
    );
    
    // Compute noise values at corners
    float4 noise = frac(sin(corners * 0.001) * 100000.0);
    float interpolated0 = lerp(lerp(noise.x, noise.y, fract.x), lerp(noise.z, noise.w, fract.x), fract.y);
    
    // Repeat for the next z-layer
    noise = frac(sin((corners + cell1.z - cell0.z) * 0.001) * 100000.0);
    float interpolated1 = lerp(lerp(noise.x, noise.y, fract.x), lerp(noise.z, noise.w, fract.x), fract.y);
    
    // Interpolate between layers and normalize to [-1, 1]
    return lerp(interpolated0, interpolated1, fract.z) * 2.0 - 1.0;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // Star appearance parameters
    float baseBrightness = 0.01; // Controls overall star intensity
    float starRadius = 0.24 + baseBrightness * 0.2; // Star size
    float inverseRadius = 1.0 / starRadius; // Precomputed
    
    // Color definitions
    //float3 starCoreColor = float3(0.8, 0.25, 0.0); // Orange hue
    //float3 coronaColor = float3(0.4, 0.05, 0.1); // Orange-red glow
    
    // Animation and aspect ratio
    float animationTime = globalTime * 0.005; // Slowed-down time
    float aspectRatio = resolution.x / resolution.y; // Aspect correction
    
    // Normalized and centered coordinates
    float2 uv = coords; // [0,1] UVs
    float2 centeredPos = uv - 0.5; // Center at (0,0)
    centeredPos.x *= aspectRatio; // Adjust for aspect ratio
    
    // Distance-based fade effect
    float distanceFromCenter = length(2.0 * centeredPos);
    float fadeFactor = pow(distanceFromCenter, 0.5); // Softer fade
    float coreInfluence = 1.0 - fadeFactor;
    float coronaInfluence = 1.0 - fadeFactor;
    
    // Polar coordinates for noise
    float angle = atan2(centeredPos.x, centeredPos.y) / TwoPi; // Normalized angle [0,1]
    float radialDistance = length(centeredPos);
    float3 noiseCoord = float3(angle, radialDistance, animationTime * 0.1);
    
    // Generate base noise for corona
    float noiseTime1 = abs(simplexNoise(
        noiseCoord + float3(0.0, -animationTime * (0.35 + baseBrightness * 0.001), animationTime * 0.015),
        15.0
    ));
    float noiseTime2 = abs(simplexNoise(
        noiseCoord + float3(0.0, -animationTime * (0.15 + baseBrightness * 0.001), animationTime * 0.015),
        45.0
    ));
    
    // Accumulate multi-octave noise
    for (int i = 1; i <= 7; i++)
    {
        float octave = float(i + 1);
        float power = pow(3.0, octave); // Exponential scale
        coreInfluence += (0.5 / power) * simplexNoise(
            noiseCoord + float3(0.0, -animationTime, animationTime * 0.2),
            power * 10.0 * (noiseTime1 + 1.0)
        );
        
        coronaInfluence += (0.5 / power) * simplexNoise(
            noiseCoord + float3(0.0, -animationTime, animationTime * 0.2),
            power * 25.0 * (noiseTime2 + 1.0)
        );
    }
    
    // Calculate corona intensity
    float coronaIntensity = pow(coreInfluence * max(1.1 - fadeFactor, 0.0), 2.0) * 50.0;
    coronaIntensity += pow(coronaInfluence * max(1.1 - fadeFactor, 0.0), 2.0) * 50.0;
    coronaIntensity *= 1.2 - noiseTime1; // Modulate with noise
    
    // Star surface effect
    float2 starSurfacePos = (uv * 2.0 - 1.0); // [-1,1] normalized
    starSurfacePos.x *= aspectRatio;
    starSurfacePos *= (2.0 - baseBrightness); // Scale with brightness
    
    float surfaceRadius = dot(starSurfacePos, starSurfacePos);
    
    // Distortion effect for spherical surface
    float surfaceDistortion = (1.0 - sqrt(abs(1.0 - surfaceRadius))) / surfaceRadius + baseBrightness * 0.5;
    
    float3 starSurfaceColor = float3(0.0, 0.0, 0.0);
    if (radialDistance < starRadius)
    {
        // Fade corona near star core
        coronaIntensity *= pow(radialDistance * inverseRadius, 10.0);
        
        // Texture coordinates for star surface
        float2 textureUV = starSurfacePos * surfaceDistortion * 0.5;
        textureUV += float2(animationTime, 0.0); // Animate scrolling
        
        // Sample texture
        float3 textureSample = tex2D(iChannel0, textureUV).rgb;
        float textureOffset = textureSample.g * baseBrightness * 4.5 + animationTime;
        
        // Secondary texture sample
        float2 starTextureUV = textureUV + float2(textureOffset, 0.0);
        starSurfaceColor = tex2D(iChannel0, starTextureUV).rgb;
    }
    
    // Glow effect
    float starGlow = min(max(1.0 - radialDistance * (1.0 - baseBrightness), 0.0), .60);
    float coolnessAmt = lerp(1.0, 3.0, coolnessInterpolant);
    
    float coronaFadeOut = InverseLerp(0.2, 0.5, distanceFromCenter) * InverseLerp(1.11, 0.98, distanceFromCenter) * .09;
    float coronaBrightness = coronaFadeOut / abs(distanceFromCenter - 0.5);
    
    // Combine components
    float3 color = (
        surfaceDistortion * (0.75 + baseBrightness * 0.3) * coreColor + // Base star
        pow(starSurfaceColor * 3.4, coolnessAmt) + // Surface texture
        coronaIntensity * coreColor * 2.5 * coronaBrightness * smoothstep(.9, .7, distanceFromCenter) + // Corona
        starGlow * coronaColor // Outer glow
    );
    
    // Additional glow
    //color += float3(1.0, 0.5, 0.25) * exp(-surfaceRadius * 0.05) * 0.4;
    color = pow(color, redness);
    
    color += float3(1.0, 0.5, 0.25) * exp(-surfaceRadius * 0.2) * 0.4;
    float alpha = InverseLerp(.8, .7, distanceFromCenter);
    color *= alpha;
    
    return float4(color, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}