texture sampleTexture;
sampler2D tex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};
sampler noise : register(s1);

float2 LinePoint : register(c0); // Center of the line on screen space
float2 LineDir : register(c1); // The direction of the line
float FadeDistance : register(c2); // (in pixels) how smoothed should the sequence be
float globalTime : register(c3); // animation time
float4 NoiseColor : register(c4); // Color of the noise zone
float2 Resolution : register(c5); // Resolution of the image for neat outline

float4 edgeDetect(float2 uv, float2 resolution)
{
    float step = 1.0 / resolution; // Step size based on resolution
    float4 h = tex2D(tex, uv + float2(step, 0)) - tex2D(tex, uv - float2(step, 0));
    float4 v = tex2D(tex, uv + float2(0, step)) - tex2D(tex, uv - float2(0, step));
    return sqrt(h * h + v * v);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 pos : SV_Position) : COLOR0
{
    float2 lineDir = normalize(LineDir);
    
    // Perpindicular
    float2 lineNormal = float2(-lineDir.y, lineDir.x);
    float2 toPixel = pos - LinePoint;
    float signedDistance = dot(toPixel, lineNormal); 
    
    float zoom = .6;
    
    // fBm
    float4 result = 0;
    float volumetricLayerFade = 1.0;
    for (int i = 0; i < 4; i++)
    {
        float timer = (globalTime * .3) / volumetricLayerFade;
        float2 p = coords * zoom;
        p.y += 1.5;
        p -= (lineNormal * -timer);
        p /= volumetricLayerFade;
        
        float totalChange = tex2D(noise, p);
        result += NoiseColor * totalChange * volumetricLayerFade;
        volumetricLayerFade *= 0.5;
    }
    result *= 1.8;
    
    // Ensure it fits within the texture
    result = lerp(result, float4(0, 0, 0, 0), !any(tex2D(tex, coords)));
    
    // 0 = transparent, 1 = opaque
    float fade = saturate(1.0 - signedDistance / FadeDistance);
    
    // Finally combine everything and add a little outline with edge detection when inside the noise area for a neat effect
    float4 texColor = lerp(tex2D(tex, coords) * sampleColor, result + edgeDetect(coords, Resolution).r * NoiseColor, 1 - fade);
    
    return texColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}