float2 NoiseOffset;
float brightness;
float MainScale;
float2 CenterPoint;
float2 TrailDirection;
float width;
float distort;
float time;
float progMult;

float3 startColor;
float3 endColor;

sampler tex1 : register(s0);
sampler tex2 : register(s1);

matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, transformMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TexCoords = input.TexCoords;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = float2(input.TexCoords.y, input.TexCoords.x);
    
    float3 mainColor = lerp(startColor, endColor, uv.y * uv.y);
    float4 original = tex2D(tex1, uv);

    float progress = (input.TexCoords.x + input.TexCoords.y) * progMult;
    float r = 9.0 + (1.0 + sin(time * 0.5 + progress * 4.0));
    float g = 3.0;
    float b = 1.5;

    float2 centerDistance = uv - CenterPoint;

    float2 inputDirection = normalize(TrailDirection);
    float2 uvDirection = normalize(centerDistance);
    
    float dist = length(centerDistance);
    float distScale = dist / MainScale;

    mainColor *= (float3(r, g, b) * 0.2);
    mainColor += float3(1.0, 0.8, 0.2) * (1.0 - distScale) * 0.5; // Glow effect
	
    float checkDir = dot(inputDirection, uvDirection);

    float widthTaper = width + (distScale * (1 - (width)));

    if (checkDir > widthTaper)
    {
        float4 secondColor = tex2D(tex2, (uv - CenterPoint + NoiseOffset) + ((inputDirection + uvDirection) * checkDir * distort));
        float distanceFromCenter = distance(uv.y, 0.5);

        return secondColor * float4(mainColor, 1) * (checkDir - widthTaper) * brightness * 2;
    }

    return float4(0, 0, 0, 0);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
};