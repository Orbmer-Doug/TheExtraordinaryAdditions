sampler targetContents : register(s1);

float2 screenSize : register(c0);
float2 layerOffset : register(c1);
float2 singleFrameScreenOffset : register(c2);

float4 PixelShaderFunction(float4 sampleColor : TEXCOORD, float2 coords : TEXCOORD0) : COLOR0
{
    float2 originalCoords = coords;
    originalCoords += layerOffset + singleFrameScreenOffset;
    
    float2 offset = 2;
    float4 color = tex2D(targetContents, coords);
    float alphaOffset = (1 - any(color.a));
    float4 leftColor = tex2D(targetContents, coords + float2(-offset.x, 0));
    float4 rightColor = tex2D(targetContents, coords + float2(offset.x, 0));
    float4 topColor = tex2D(targetContents, coords + float2(0, -offset.y));
    float4 bottomColor = tex2D(targetContents, coords + float2(0, offset.y));
    float4 avergeColor = (color + leftColor + rightColor + topColor + bottomColor) / 5;
    float lowestColorValue = min(avergeColor.r, avergeColor.g);
    lowestColorValue = min(lowestColorValue, avergeColor.b);
    avergeColor = lerp(avergeColor, float4(1, 1, 1, 1), pow(lowestColorValue, 0.5)) * avergeColor.a * 1.5;
    return avergeColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}