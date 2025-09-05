sampler image : register(s0);
float time;
float2 resolution;
float2 uScreenPosition;
float radians;
float scale;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float effectRadius = scale;
    float effectAngle = radians * 3.14;
    
    float2 center = uScreenPosition.xy / resolution.xy;
    center = center == float2(0., 0.) ? float2(.5, .5) : center;
    
    float2 uv = sampleColor.xy / resolution.xy - center;
    
    float len = length(uv * float2(resolution.x / resolution.y, 1.));
    float angle = atan(uv.y + uv.x) + effectAngle * smoothstep(effectRadius, 0., len);
    float radius = length(uv);

    return sampleColor = tex2D(image, float2(radius * cos(angle), radius * sin(angle)) + center);
}


technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}