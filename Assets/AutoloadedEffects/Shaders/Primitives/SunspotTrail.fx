float globalTime;
uniform bool flip;
sampler noiseTex : register(s1);

// Colors for each zone
static const float4 whiteHot = float4(1.0, 1.0, 1.0, 1.0);
static const float4 redOrange = float4(1.0, 0.3, 0.0, 1.0);
static const float4 coolRed = float4(0.6, 0.1, 0.1, 1.0);

static const float zone1End = 0.1; // White-hot zone
static const float zone2End = 0.7; // Red-orange zone

static const float streakThreshold = 0.9;

matrix transformMatrix;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, transformMatrix);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float QuadraticBezier(float t, float p0, float p1, float p2)
{
    float u = 1.0 - t;
    float tt = t * t;
    float uu = u * u;
    return (uu * p0) + (2.0 * u * t * p1) + (tt * p2);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    coords.y = lerp(coords.y, 1 - coords.y, flip);
    
    // Calculate the y-coordinate of the Bezier curve at this x
    float curveV = QuadraticBezier(coords.y, 0.0, 0.5, 1.0);

    // Discard pixels above the curve
    if (coords.x > curveV)
        discard;
    
    // Scroll the noise textures horizontally
    float2 noiseCoords = coords + float2(-globalTime * 0.5, 0.0);
    float noise1 = tex2D(noiseTex, noiseCoords).r;
    float noise2 = tex2D(noiseTex, noiseCoords * 2).r;
    
    float4 color = float4(0, 0, 0, 0);
    
    // Instead of an inefficient conditional chain, painters method the layers with linear interpolation
    color = lerp(color, lerp(redOrange, coolRed, (coords.x - zone2End) / (1.0 - zone2End)), coords.x <= 1);
    color.rgb = lerp(color.rgb, color.rgb + noise1 * 0.4, coords.x <= 1);
    color = lerp(color, lerp(whiteHot, redOrange, (coords.x - zone1End) / (zone2End - zone1End)), coords.x <= zone2End);
    color.rgb = lerp(color.rgb, color.rgb + noise1 * 0.4, coords.x <= zone2End);
    color = lerp(color, whiteHot, coords.x <= zone1End);
    
    // Create a streak along the top of the trail
    float streakIntensity = lerp(streakThreshold, (coords.y - streakThreshold) / (1.0 - streakThreshold), coords.y); // Fade in
    color = lerp(color, float4(1.0, 1.0, 1.0, 1.0), streakIntensity * lerp(.6, .1, coords.y)); // Blend with white
    
    // Sharpen contrast and add some texture. Additionally apply a fade for a sense of motion
    color *= (noise2 - .2 + step(.35, noise1 + (0.5 - distance(coords.x / curveV, .5))));
    color *= smoothstep(1, .7, coords.x);
    color *= input.Color.a;
    color = pow(abs(color), 2.4);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}