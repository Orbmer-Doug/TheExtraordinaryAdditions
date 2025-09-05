sampler tex : register(s0);
float globalTime;

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float noise(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 3758.5453);
}
float smoothNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = f * f * (3.0 - 2.0 * f);
    
    return lerp(lerp(noise(i + float2(0, 0)), noise(i + float2(1, 0)), u.x),
                lerp(noise(i + float2(0, 1)), noise(i + float2(1, 1)), u.x), u.y);
}
float layeredNoise(float2 uv)
{
    float n = 0.0;
    n += smoothNoise(uv * 4.0) * 0.5;
    n += smoothNoise(uv * 8.0) * 0.25;
    n += smoothNoise(uv * 16.0) * 0.125;
    return n;
}

float4 PixelShaderFunction(VertexShaderInput input) : COLOR
{
    float2 coords = input.TextureCoordinates;
    coords = round(coords * 350) / 350;
    float4 main = tex2D(tex, coords);
    
    float time = globalTime * 4;
    float2 center = float2(smoothNoise(coords + float2(time, 0)) * 1.3, smoothNoise(coords + float2(time, 0)) * 1.3) * 2;
    float splitDistance = 0.01056 / (distance(coords, center) + 1);
    main.r = tex2D(tex, coords + float2(-0.707, -0.707) * splitDistance).r;
    main.g = tex2D(tex, coords + float2(0.707, -0.707) * splitDistance).g;
    main.b = tex2D(tex, coords + float2(0, 1) * splitDistance).b;
    main *= 2.4;
    if (main.r > .9 && main.g > .9 && main.b > .9)
    {
        main.rgb /= 3 - layeredNoise(coords * .1 + float2(time, 0)) * layeredNoise(coords * .1 + float2(0, -time));
    }
    main = pow(main, 2.2);
    
    return main;
}

technique SpriteDrawing
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
};