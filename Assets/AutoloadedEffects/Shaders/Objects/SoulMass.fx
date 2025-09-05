float time : register(c0);

// Quality Settings
static const float MarchSteps = 8;

// Scene Settings
static const float3 ExpPosition = float3(0.0, 0.0, 0.0);
static const float Radius = 2.2;

// Noise Settings
static const float NoiseSteps = 10;
static const float NoiseAmplitude = 0.07;
static const float NoiseFrequency = 1.5;
static const float3 Animation = float3(0.0, 3.0, 0.5);

// Color Gradient
static const float4 Color1 = float4(1.0, 3.0, 1.0, 1.0);
static const float4 Color2 = float4(1.0, 1.0, 0.0, 1.0);
static const float4 Color3 = float4(0.7, 0.3, 0.0, 1.0);
static const float4 Color4 = float4(0.55, 0.25, 0.0, 1.0);

float simplexNoise(float3 coordinates)
{
    const float3 scale = float3(1.0, 10.0, 100.0);
    
    float resolution = 5;
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
    float4 noise = frac(sin(corners * 0.01) * 100000.0);
    float interpolated0 = lerp(lerp(noise.x, noise.y, fract.x), lerp(noise.z, noise.w, fract.x), fract.y);
    
    // Repeat for the next z-layer
    noise = frac(sin((corners + cell1.z - cell0.z) * 0.01) * 100000.0);
    float interpolated1 = lerp(lerp(noise.x, noise.y, fract.x), lerp(noise.z, noise.w, fract.x), fract.y);
    
    // Interpolate between layers and normalize to [-1, 1]
    return lerp(interpolated0, interpolated1, fract.z) * 2.0 - 1.0;
}

float Turbulence(float3 position, float minFreq, float maxFreq, float qWidth)
{
    float value = 0.0;
    float cutoff = clamp(0.5 / qWidth, 0, maxFreq);
    float fade;
    float fOut = minFreq;
    for (int i = NoiseSteps; i >= 0; i--)
    {
        if (fOut >= 0.65 * cutoff)
            break;
        fOut *= 2.0;
        value += abs(simplexNoise(position * fOut)) / fOut;
    }
    fade = clamp(2 * (cutoff - fOut) / cutoff, 0, 1);
    value += fade * abs(simplexNoise(position * fOut)) / fOut;
    return 2.0 - value; // lerp(-6, 2, sin(time) * .5 + .5) RIGHT HERE, make smaller for fade out animation (reduces 'mountain' height)
}

float SphereDist(float3 position)
{
    return length(position - ExpPosition) - Radius;
}

float4 Shade(float distance)
{
    float c1 = saturate(distance * 8.0 + 0.5);
    float c2 = saturate(distance * 5.0);
    float c3 = saturate(distance * 3.4 - 0.5);
    float4 a = lerp(Color1, Color2, c1);
    float4 b = lerp(a, Color3, c2);
    return lerp(b, Color4, c3);
}

float RenderScene(float3 position, out float distance)
{
    float noise = Turbulence(position * NoiseFrequency + Animation * time, 0.08, 1.5, 0.01) * NoiseAmplitude;
    noise = saturate(abs(noise));
    distance = SphereDist(position) + noise;
    return noise;
}

float4 March(float3 rayOrigin, float3 rayStep)
{
    float3 position = rayOrigin;
    float distance;
    float displacement;
    for (int step = MarchSteps; step >= 0; --step)
    {
        displacement = RenderScene(position, distance);
        if (distance < 0.015)
            break;
        position += rayStep * distance;
    }
    return lerp(Shade(displacement), float4(0, 0, 0, 0), distance >= 0.5);
}

bool IntersectSphere(float3 ro, float3 rd, float3 pos, float radius, out float3 intersectPoint)
{
    float3 relDistance = (ro - pos);
    float b = dot(relDistance, rd);
    float c = dot(relDistance, relDistance) - radius * radius;
    float d = b * b - c;
    intersectPoint = ro + rd * (-b - sqrt(d));
    return d >= 0.0;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = coords * 2.0 - 1.0;
    
    // Camera setup
    float rotx = -cos(time) * .34;
    float roty = sin(time) * .5;
    float zoom = 5.0;

    float3 ro = zoom * normalize(float3(cos(roty), cos(rotx), sin(roty)));
    float3 ww = normalize(float3(0.0, 0.0, 0.0) - ro);
    float3 uu = normalize(cross(float3(0.0, 1.0, 0.0), ww));
    float3 vv = normalize(cross(ww, uu));
    float3 rayDir = normalize(p.x * uu + p.y * vv + 1.5 * ww);

    float4 col = float4(0, 0, 0, 0);
    float3 origin;
    if (IntersectSphere(ro, rayDir, ExpPosition, Radius + NoiseAmplitude * 6.0, origin))
    {
        col = March(origin, rayDir);
    }

    // Apply color quantization
    col = floor(col * 16.0) / 16.0;
    
    return col;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}