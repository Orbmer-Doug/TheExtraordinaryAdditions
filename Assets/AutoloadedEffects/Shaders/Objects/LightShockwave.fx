sampler uvOffsetTex : register(s0);
sampler uvOffsetingNoiseTexture : register(s1);

float globalTime;
float explosionDistance;
float shockwaveOpacity;
float2 screenSize;
float2 projPosition;
float3 mainColor;

float InverseLerp(float min, float max, float x)
{
    return saturate((x - min) / (max - min));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = 0;
    
    float warpAngle = tex2D(uvOffsetingNoiseTexture, coords * 7.3 + float2(globalTime * 0.1, 0)).r * 16;
    float2 warpNoiseOffset = float2(sin(warpAngle + 1.57), sin(warpAngle));
    
    float uvOffsetAngle = tex2D(uvOffsetTex, coords) * 8 + globalTime * 10;
    float2 uvOffset = float2(sin(uvOffsetAngle + 1.57), sin(uvOffsetAngle)) * .0028;
    
    
    float offsetFromProj = length((coords + uvOffset) * screenSize - projPosition);
    
    // Distance to explosion line
    float signDistanceToExplosion = (offsetFromProj - explosionDistance) / screenSize.x;
    float distanceFromExplosion = abs(signDistanceToExplosion);
    
    // Dissipate intensity towards edges
    distanceFromExplosion += InverseLerp(.01f, .15f, signDistanceToExplosion);
    
    // Bright colors at edge of boom
    color += float4(mainColor, 1) * .041 * shockwaveOpacity / distanceFromExplosion;
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}