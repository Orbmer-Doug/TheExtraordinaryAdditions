sampler noiseTexture : register(s1);

int sides : register(c0);
float opacity : register(c1);
float time : register(c2);
float3 col1 : register(c3);
float3 col2 : register(c4);

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 pos : SV_Position) : COLOR0
{
    float2 centered = coords * 2.0 - 1.0;
    
    // Get angle and distance from center
    float angle = atan2(centered.y, centered.x);
    float dist = length(centered);
    
    // Calculate polygon radius at this angle
    float polygonAngle = 6.28318530718 / sides; // 2*PI / sides
    float segment = floor(angle / polygonAngle + 0.5);
    float angleToEdge = segment * polygonAngle;
    float polygonRadius = cos(polygonAngle / 2.0);
    float radius = polygonRadius / cos(angle - angleToEdge);
    
    // Check if inside polygon
    float inside = step(dist, radius);
    float3 col = float3(lerp(col2, col1, opacity));
    float4 final = float4(col, inside);
    coords = round(coords * 10.) / 10.;
    final += tex2D(noiseTexture, coords * .12 + float2(time * .7, 0.)) * float4(col, 1.) * inside * opacity;
    final += tex2D(noiseTexture, coords * .09 + float2(time * .47, 0.)) * float4(col, 1.) * inside * opacity;
    final *= lerp(1., 1.6, opacity);
    final *= inside * opacity;
    
    return final;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}