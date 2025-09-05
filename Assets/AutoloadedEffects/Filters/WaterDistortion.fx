static const float4 u_WaveStrengthX = float4(4.15, 4.66, 0.0016, 0.0015);
static const float4 u_WaveStrengthY = float4(2.54, 6.33, 0.00102, 0.0025);

// Textures and sampler
sampler2D screenTex : register(s0);
sampler2D noiseTex : register(s1);

uniform float globalTime;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float noise = tex2D(noiseTex, globalTime + coords).r;
    
    coords.y += (cos((coords.y + globalTime * u_WaveStrengthY.y + u_WaveStrengthY.x * noise)) * u_WaveStrengthY.z) +
            (cos((coords.y + globalTime) * 10.0) * u_WaveStrengthY.w);

    coords.x += (sin((coords.y + globalTime * u_WaveStrengthX.y + u_WaveStrengthX.x * noise)) * u_WaveStrengthX.z) +
            (sin((coords.y + globalTime) * 15.0) * u_WaveStrengthX.w);

    // Sample texture at distorted UV coordinates
    return tex2D(screenTex, coords);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}