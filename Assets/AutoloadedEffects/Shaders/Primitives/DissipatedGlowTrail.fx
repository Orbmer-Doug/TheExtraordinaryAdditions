sampler tex0 : register(s0);
sampler tex1 : register(s1);
sampler tex2 : register(s2);

float globalTime;
float3 OutlineColor : register(c1);

matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, transformMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 baseCoords = input.TextureCoordinates;
    
    float pinchAmount = 2.75;
    float2 center = float2(0, .5);
    float2 offset = baseCoords - center;
    float dist = length(offset);
    float pinchFactor = 1 + pinchAmount * (pow(dist, 3));
    float2 coords = center + offset * pinchFactor;
    
    float time = globalTime * .8;
    float4 trail = tex2D(tex1, float2(frac(coords.x * 1.5 - time), coords.y));
    float4 trail2 = tex2D(tex2, float2(frac(coords.x * 1.5 - time), coords.y)) * (tex2D(tex2, float2(frac(coords.x * .5 - time * 2), coords.y)) + trail);
    
    // Transparency calculation
    float2 noiseCoords = coords * 0.6 + float2(-globalTime * .1f, 0);
    float4 noiseColor = tex2D(tex0, noiseCoords);
    float isTransparent = 1 - step(noiseColor.r, lerp(0, 1.2, 1 - coords.x));
    
    // Edge detection for glow
    float2 texelSize = float2(.01, .01);
    float4 noiseUp = tex2D(tex0, noiseCoords + float2(0, texelSize.y));
    float4 noiseDown = tex2D(tex0, noiseCoords + float2(0, -texelSize.y));
    float4 noiseLeft = tex2D(tex0, noiseCoords + float2(-texelSize.x, 0));
    float4 noiseRight = tex2D(tex0, noiseCoords + float2(texelSize.x, 0));
    
    float transUp = 1 - step(noiseUp.r, lerp(0, 1.2, 1 - coords.x));
    float transDown = 1 - step(noiseDown.r, lerp(0, 1.2, 1 - coords.x));
    float transLeft = 1 - step(noiseLeft.r, lerp(0, 1.2, 1 - coords.x));
    float transRight = 1 - step(noiseRight.r, lerp(0, 1.2, 1 - coords.x));
    
    // Simple edge detection: difference between current and neighbors
    float edge = abs(isTransparent - transUp) +
                 abs(isTransparent - transDown) +
                 abs(isTransparent - transLeft) +
                 abs(isTransparent - transRight);
    edge = saturate(edge); // Normalize to [0,1]
    
    // White-hot glow
    float glowIntensity = edge * (1 - isTransparent); // Glow only on opaque pixels near edges
    float glowFalloff = pow(saturate(1 - glowIntensity), 3.0); // Sharp falloff
    float3 glowColor = OutlineColor * glowIntensity * 1;
    
    // Base trail color
    float distanceFromCenter = distance(coords.y, 0.5) + tex2D(tex1, coords + float2(-0.3, 0.5) * globalTime * 2) * .1;
    float edgeGlow = 1.8 / pow(abs(distanceFromCenter), 0.9) * .5;

    float4 finalColor = (trail + trail2) * color * (1 - isTransparent);
    finalColor = pow(abs(finalColor), 1.2);
    
    finalColor = saturate(finalColor * edgeGlow);
    
    finalColor.rgb += glowColor * (1 - glowFalloff);
    finalColor.a = saturate(finalColor.a + glowIntensity); // Ensure glow doesn't break alpha
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
