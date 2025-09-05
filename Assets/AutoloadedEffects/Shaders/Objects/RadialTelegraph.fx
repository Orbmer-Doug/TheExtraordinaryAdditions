texture sampleTexture;
sampler2D noise = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float direction;
float angle;
float4 color;
float globalTime;

static const float PI = 3.1415926535;
static const float ViewFieldRadius = .5;
static const float FadeWidth = 0.15;

float2 ConvertToPolar(in float2 coords)
{
    float r = sqrt(coords.x * coords.x + coords.y * coords.y);
    float theta = atan(coords.y / coords.x);

    return float2(r, theta);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 pixelPos = coords;
    float2 delta = pixelPos - float2(.5, .5);
    
    // Calculate distance from center
    float distance = length(delta);
    
    // Check if pixel is outside the radius
    if (distance > ViewFieldRadius)
        discard;
    
    // Calculate angle of the pixel relative to the center
    float pixelAngle = atan2(delta.y, delta.x);
    
    // Normalize angles to handle wraparound
    float rawAngleDiff = direction - pixelAngle;
    rawAngleDiff = rawAngleDiff - 2.0 * PI * floor((rawAngleDiff + PI) / (2.0 * PI));
    float angleDiff = abs(rawAngleDiff);
    
    // Check if pixel is within the view field angle range
    if (angleDiff > angle)
        discard;
    
    // Calculate fade from outer edge to center
    float fade = 1 - pow((distance / ViewFieldRadius), .4);
    float4 final = color * fade;
    
    float distFromCenter = 1 - distance * 2;
    float4 noise1 = tex2D(noise, ConvertToPolar(delta) * float2(.7, .7) - float2(globalTime * .6, 0));
    noise1 += tex2D(noise, ConvertToPolar(delta) * float2(1, 1) - float2(globalTime * .3, 0)) * .7;
    noise1 *= 1.4;
    noise1 += pow(distFromCenter, 4) + pow(distFromCenter, 3) * 0.2;
    noise1 = pow(abs(noise1), 2.2);
    final *= noise1;
    
    // Brighten the edges
    float edgeNoise = tex2D(noise, ConvertToPolar(delta) * float2(4.3, 1.3) - float2(globalTime * 0.8, 0)).r;
    final += color * (smoothstep(angle - FadeWidth, angle, angleDiff) * fade * 2) / edgeNoise;
    final *= 1.0 - smoothstep(angle - (FadeWidth / 2.5), angle, angleDiff);
    
    return final;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}