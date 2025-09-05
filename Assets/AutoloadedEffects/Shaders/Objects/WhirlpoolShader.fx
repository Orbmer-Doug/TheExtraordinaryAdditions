sampler uImage0 : register(s0);
float globalTime;
float swirlPower = 1;
float swirlSpeed = 1;
float flowSpeed = .5;

// Simple hash function for noise
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// 2D value noise
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i + float2(0.0, 0.0));
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p)
{
    float v = 0.0;
    float a = 2;
    float2 shift = float2(100.0, 100.0);

    for (int i = 0; i < 12; i++)
    {
        v += a * noise(p);
        p = p * 2.0 + shift;
        a *= 0.5;
    }
    return v;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Swirl coordinates
    float2 centeredCoords = coords - 0.5;
    float r = length(centeredCoords);
    float epsilon = 2.22e-16;
    float swirlRotation = swirlPower / (r + epsilon) - swirlSpeed * globalTime;
    float swirlSine = sin(swirlRotation);
    float swirlCosine = sin(swirlRotation + 1.5707);
    float2x2 swirlRotationMatrix = float2x2(swirlCosine, -swirlSine, swirlSine, swirlCosine);
    float2 swirlCoordinates = mul(centeredCoords, swirlRotationMatrix) + 0.5;

    // Perturb coordinates with fBm for water-like flow
    float2 flowOffset = float2(0.0, 0.0);
    float flowScale = 4.0;
    flowOffset.x = fbm(swirlCoordinates * flowScale + float2(globalTime * flowSpeed, 0.0));
    flowOffset.y = fbm(swirlCoordinates * flowScale + float2(0.0, globalTime * flowSpeed));
    swirlCoordinates += (flowOffset - 0.5) * 0.1; // Offset coordinates, scaled down for subtlety
    
    float4 texColor = tex2D(uImage0, swirlCoordinates);

    // Center fade to black
    float r0 = 0.1;
    float centerFade = saturate(r / r0);

    // Circular edge fade
    float edgeFade = 1.0 - smoothstep(0.4, 0.5, r);

    // Compute depth using fBm
    float depthScale = 3.0;
    float depth = fbm(coords * depthScale + globalTime * 0.2);
    depth = depth * (1.0 - r * 0.5); // Bias depth to be deeper near the center
    depth = saturate(depth * 1.5); // Adjust contrast of depth
    
    // Adjust brightness based on depth
    float depthBrightness = lerp(0.5, 1.5, depth); // Darker in deeper areas, brighter in shallower

    // Compute final color
    float3 rgb = texColor.rgb * edgeFade * sampleColor.rgb * centerFade * depthBrightness;
    float alpha = edgeFade * sampleColor.a;

    return float4(rgb, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}