sampler2D noise : register(s1);
float globalTime : register(c0);
float2 res : register(c1);
float ratio : register(c2);
bool golden : register(c3);

static const float zone1End = 0.01;
static const float zone2End = .7;
static const float notchWidth = .05;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    coords.x -= coords.x % (1 / float(res.x));
    coords.y -= coords.y % (1 / float(res.y));
    
    coords = 1 - coords;
    
    float2 noiseCoords = coords + float2(globalTime * -0.15, 0.0);
    float noise1 = tex2D(noise, noiseCoords).r + tex2D(noise, noiseCoords * .7 + float2(1.4, 0) + float2(globalTime * -.2, 0)).r + tex2D(noise, noiseCoords * 1.7 + float2(.5, 0) + float2(globalTime * -.6, 0)).r;
    
    float4 whiteHot = float4(1.0, 1.0, 1.0, 1.0);
    float4 light = lerp(float4(0.153, 0.753, 1, 1.0), float4(1, 0.949, 0.192, 1), golden);
    float4 dark = lerp(float4(0, 0.486, 0.69, 1.0), float4(0.569, 0.533, 0, 1), golden);
    
    float4 color = float4(0, 0, 0, 0);
    color = lerp(color, lerp(light, dark, (coords.x - zone2End) / (1.0 - zone2End)), coords.x <= 1);
    color.rgb = lerp(color.rgb, color.rgb + noise1 * 0.4, coords.x <= 1);
    color = lerp(color, lerp(whiteHot, light, (coords.x - zone1End) / (zone2End - zone1End)), coords.x <= zone2End);
    color.rgb = lerp(color.rgb, color.rgb + noise1 * 0.4, coords.x <= zone2End);
    color = lerp(color, whiteHot, coords.x <= zone1End);
    
    // Makes the notch at the edge
    color = lerp(color, whiteHot, smoothstep(notchWidth, notchWidth * 0.15, abs(coords.x - ratio)));
    
    clip(coords.x - ratio);
    
    color = pow(abs(color), 3.4);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}