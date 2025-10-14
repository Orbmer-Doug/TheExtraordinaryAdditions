sampler tex : register(s0);
uniform float time;
uniform bool findingChannel;
uniform float2 resolution;

static const float scanlineIntensity = .8;
static const float fringingAmount = 0.001;
static const float crosstalkAmount = .105;
static const float interferenceFreq = 50;
static const float interferenceAmp = .012;
static const float phaseShift = .5;
static const float fisheyeStrength = .5;
static const float animSpeed = 12;
static const float outline = .05;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords.x -= coords.x % (1 / resolution.x);
    coords.y -= coords.y % (1 / resolution.y);
    
    // barrel distortion
    float2 centeredUV = coords * 2 - 1; // [-1, 1]
    float dist = length(centeredUV);
    float distortion = 1 + fisheyeStrength * dist * dist;
    float2 warpedUV = (centeredUV * distortion + 1) * .5;

    // keep in bounds but respect the outline
    if (warpedUV.x < -outline || warpedUV.x > (1 + outline) || warpedUV.y < -outline || warpedUV.y > (1 + outline))
        discard;
    
    // add a beveled edge
    if (warpedUV.x < 0 || warpedUV.x > 1 || warpedUV.y < 0 || warpedUV.y > 1)
    {
        float2 edgeDist = float2(
        max(0 - warpedUV.x, warpedUV.x - 1), // x distance to edges
        max(0 - warpedUV.y, warpedUV.y - 1) // y distance to edges
        );
        float maxDist = max(edgeDist.x, edgeDist.y); // closest distance to any edge
        float outlineDist = max(0, outline - maxDist) / outline;
        return floor(lerp(float4(0.8, 0.8, 0.8, 1), float4(0.1, 0.1, 0.1, 1), smoothstep(0, 1, outlineDist)) * 8) / 8;
    }
    
    float4 color = tex2D(tex, warpedUV) * sampleColor;
    
    float scanline = sin(warpedUV.y * 600 + time * animSpeed) * scanlineIntensity;
    color.rgb *= (1 - scanline * 0.5);
    
    float2 fringeOffset = float2(fringingAmount, 0);
    float4 rColor = tex2D(tex, warpedUV + fringeOffset);
    float4 bColor = tex2D(tex, warpedUV - fringeOffset);
    color.r = rColor.r;
    color.b = bColor.b;

    // dot crawl 
    float2 crosstalkOffset = float2(crosstalkAmount * sin(warpedUV.y * 100 + time * animSpeed), 0);
    float4 crosstalkColor = tex2D(tex, warpedUV + crosstalkOffset);
    color.rgb += crosstalkColor.rgb * crosstalkAmount;

    // phase alternation
    float phase = sin(warpedUV.y * 600 + phaseShift + time * animSpeed) * 0.1;
    color.rgb += float3(phase, -phase, phase) * 0.1;

    // high-frequency noise
    float interference = sin(warpedUV.x * interferenceFreq + warpedUV.y * 1000 + time * animSpeed * 2) * interferenceAmp;
    color.rgb += interference;
    
    // dont go wacky
    color.rgb = saturate(color.rgb);
    
    float noise = abs(frac(sin(dot(coords * 0.013 + float2(0, frac(time)), float2(17.8342, 74.8819))) * 53648));
    return lerp(color, float4(noise, noise, noise, 1), findingChannel);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}