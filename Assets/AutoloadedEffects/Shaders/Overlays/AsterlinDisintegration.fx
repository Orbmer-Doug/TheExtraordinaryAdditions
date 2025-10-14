sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float interpolant : register(c0);
float2 center : register(c1);
float2 screenPosition : register(c2);
float2 texSize : register(c3);
float2 direction : register(c4);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float2 screenUV = (position.xy + screenPosition) / texSize;
    
    // Skew the coords in the direction based on the interpolant
    float dissolveDirectionNoise = pow(abs(tex2D(noiseTexture, screenUV * float2(9, 0)) * tex2D(noiseTexture, screenUV * float2(17, 0.04))), 0.75);
    float2 dissolveOffset = direction * dissolveDirectionNoise * interpolant * 2.4;
    coords += dissolveOffset;
    
    // Remove pixels under a certain threshold to give that effect of disintegration
    float dissolveNoise = (tex2D(noiseTexture, screenUV * texSize * 0.002) + tex2D(noiseTexture, screenUV * texSize * 0.0032)) * 0.5;
    clip(dissolveNoise - interpolant - distance(coords, center) + 0.05);
    
    // Return only a solid color
    float4 color = tex2D(baseTexture, coords);
    return any(color) * sampleColor * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}