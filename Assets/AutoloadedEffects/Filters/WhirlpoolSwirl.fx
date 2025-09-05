// Inspired from: https://github.com/LucilleKarma/WrathOfTheGodsPublic/blob/28003f4c342cdb5122dcb45541500bbffa66f321/Assets/AutoloadedEffects/Filters/AvatarRiftSpaghettificationShader.fx

sampler screenTexture : register(s0);
sampler whirlTexture : register(s1);

float distortionIntensity;
float zoom;
float distortionRadius;
float2 distortionPosition;
float2 screenSize;
float blackSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float distanceFromDistortion = distance(position.xy, distortionPosition) / zoom;
    float localDistortionIntensity = smoothstep(distortionRadius, 0, distanceFromDistortion);
    
    localDistortionIntensity = exp(pow(localDistortionIntensity, 2)) - 1;
    
    float2 distortedCoords = lerp(coords, distortionPosition / screenSize, -distortionIntensity * localDistortionIntensity);
    float4 baseColor = tex2D(screenTexture, coords);
    float4 poolColor = tex2D(whirlTexture, coords);
    float4 distortedColor = tex2D(screenTexture, distortedCoords);
    float4 distortedPoolColor = tex2D(whirlTexture, distortedCoords);
    bool inPool = length(distortedColor - distortedPoolColor) <= 0.01 && length(baseColor - poolColor) <= 0.01;
    float4 finalColor = lerp(distortedColor, baseColor, inPool);
    finalColor.rgb *= (1 - localDistortionIntensity * blackSize);
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}