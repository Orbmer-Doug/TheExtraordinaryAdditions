sampler noise : register(s1);

float direction;
float angle;
float4 color;
float globalTime;

static const float PI = 3.1415926535;
static const float ViewFieldRadius = .5;
static const float FadeWidth = 0.2;

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
    float angleDiff = abs(direction - pixelAngle);
    angleDiff = lerp(angleDiff, 2.0 * PI - angleDiff, angleDiff > PI);
    
    // Check if pixel is within the view field angle range
    if (angleDiff > angle)
        discard;
    
    // Calculate fade from outer edge to center
    float fade = pow((distance / ViewFieldRadius), 5);
    float4 final = color * fade;
    
    float distFromCenter = distance * 2;
    float4 noise1 = tex2D(noise, ConvertToPolar(delta) * float2(2, 2) + float2(globalTime, 0));
    noise1 += tex2D(noise, ConvertToPolar(delta) * float2(1, 3) + float2(globalTime * .3, 0)) * .7;
    noise1 *= 1.4;
    noise1 += pow(distFromCenter, 4) + pow(distFromCenter, 3) * 0.2;
    if (distFromCenter > 0.95)
        noise1 *= (1 - ((distFromCenter - 0.95) / 0.05));
    noise1 = pow(abs(noise1), 2.2);
    final *= noise1;
    
    // Fade the edges
    final *= 1.0 - smoothstep(angle - FadeWidth, angle, angleDiff);
    
    return final;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}