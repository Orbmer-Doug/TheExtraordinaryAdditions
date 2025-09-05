sampler noise : register(s0);
float3 color;
float3 secondColor;
float opacity : register(C0);
float globalTime;

static const float PiOver2 = 1.570796327;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float2 res = float2(500, 500);
    coords.x -= coords.x % (1 / res);
    coords.y -= coords.y % (1 / res);
    
    float distanceFromTargetPosition = distance(coords, 0.5);
    
    // Calculate the swirl coordinates.
    float2 centeredCoords = coords - 0.5;
    float swirlRotation = length(centeredCoords) * 41.2 - globalTime * 6;
    
    // tangent will give pulsating ripples, atan will gave wavy formations
    // top down, atan and tan give both effects at once
    // multiplying the original sine and cosine in that order from the mentioned top down above gives expanding craters
    float swirlSine = sin(swirlRotation);
    float swirlCosine = sin(swirlRotation + PiOver2);
    float2x2 swirlRotationMatrix = float2x2(swirlCosine, -swirlSine, swirlSine, swirlCosine);
    float2 swirlCoordinates = mul(centeredCoords, swirlRotationMatrix) + 0.5;
    
    // Calculate fade, swirl arm colors, and draw the portal to the screen.
    float swirlColorFade = saturate(distanceFromTargetPosition * 3) / (opacity + 0.0001);
    float3 swirlBaseColor = lerp(color, secondColor, pow(swirlColorFade, 0.33));
    float4 swirlNoiseColor = tex2D(noise, swirlCoordinates) * (1 - swirlColorFade);
    float4 endColor = lerp(float4(swirlBaseColor, 0.1), 0, swirlColorFade);
    float4 finalColor = lerp(0, endColor * (1 + (1 - swirlColorFade) * 2), saturate(swirlNoiseColor.r));
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}