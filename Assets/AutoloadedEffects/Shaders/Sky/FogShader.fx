// Inspired from: https://github.com/InfernumTeam/InfernumMode/blob/master/Assets/Effects/Overlays/CosmicBackgroundShader.fx

float time;
sampler2D clouds : register(s0);

// Simple 2D noise function
float noise(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// Smooth noise interpolation
float smoothNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = f * f * (3.0 - 2.0 * f);
    
    return lerp(lerp(noise(i + float2(0, 0)), noise(i + float2(1, 0)), u.x),
                lerp(noise(i + float2(0, 1)), noise(i + float2(1, 1)), u.x), u.y);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 TexCoord : TEXCOORD0) : COLOR0
{
    float2 coords = TexCoord + float2(time * .05, 0);
    float zoom = .9;
    float scroll = 1;
    
    float3 crimson = float3(.69, .062, .082);
    float3 blood = float3(.478, 0, .015);
    
    float brightness = .5;
    
    float globalTime = time * .07;
    
    // Use some fBm
    float4 result = 0;
    float volumetricLayerFade = 1.0;
    float amplitude = .4;
    for (int i = 0; i < 16; i++)
    {
        float timer = globalTime / volumetricLayerFade;
        float2 p = coords * zoom;
        p.y += 1.5;
        
        // Blot out patches and darken to make it more dynamic
        result -= smoothNoise(coords * volumetricLayerFade) * amplitude;
        amplitude *= .5;
        
        // Perform scrolling behaviors. Each layer should scroll a bit slower than the previous one, to give an illusion of 3D
        p += float2(timer * scroll, timer * scroll);
        scroll *= .98;
        p /= volumetricLayerFade;

        float totalChange = tex2D(clouds, p);
        
        // Add a subtle shift of hue
        float4 layerColor = float4(lerp(crimson, blood, i / 16.0), 1.0);
        result += layerColor * totalChange * volumetricLayerFade;

        // Make the next layer exponentially weaker in intensity to increase depth
        volumetricLayerFade *= 0.9;
    }

    // Increase contrast to make the clouds pop out more
    result.rgb = pow(result.rgb, 1.6) * brightness;
    return result * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}