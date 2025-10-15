texture sampleTexture;
sampler2D texture1 = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float time;
float blowUpPower;
float blowUpSize;
float3 color;
float opacity;
float3 edgeColor;
float edgeBlendStrength;
float noiseScale;

float4 PixelShaderFunction(float2 uv: TEXCOORD) : COLOR
{
    // .5 our saviour
    
    // Crop in a circle
    float distanceFromCenter = length(uv - float2(0.5, 0.5)) * 2;
    if (distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    // "Blow up" the noise map so it looks circular.
    float blownUpUVX = pow((abs(uv.x - 0.5)) * 2, blowUpPower);
    float blownUpUVY = pow((abs(uv.y - 0.5)) * 2, blowUpPower);
    float2 blownUpUV = float2(-blownUpUVY * blowUpSize * 0.5 + uv.x * (1 + blownUpUVY * blowUpSize), -blownUpUVX * blowUpSize * 0.5 + uv.y * (1 + blownUpUVX * blowUpSize));
    
    // Rescale
    blownUpUV *= noiseScale;
    
    // Scroll effect
    blownUpUV.x = (blownUpUV.x + time) % 1;
    
    // Get the noise color
    float4 noiseColor = tex2D(texture1, blownUpUV);

    // Apply a layers of fake fresnel
    noiseColor += pow(distanceFromCenter, 6);
    
    // Fade the edges
    if (distanceFromCenter > 0.95)
        noiseColor *= (1 - ((distanceFromCenter - 0.95) / 0.05));
    
    return noiseColor * float4(lerp(color, edgeColor, pow(distanceFromCenter, edgeBlendStrength)), opacity);
}


technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}