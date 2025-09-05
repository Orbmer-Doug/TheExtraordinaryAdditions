sampler2D tex : register(s0);
float2 CrackCenter1 = float2(0.3, 0.3);
float2 CrackCenter2 = float2(0.7, 0.4);
float2 CrackCenter3 = float2(0.5, 0.7);
float Completion : register(c0);

// Constants for controlling the effect
static const float CrackScale = 4.0; // Controls the density of cracks
static const float CrackWidth = 0.6; // Width of the cracks
static const float CrackGlowIntensity = 1; // Intensity of the crack glow
static const float AlphaThreshold = 0.4; // Alpha threshold for discarding pixels
static const float NumCracks = 2.0; // Number of radiating cracks

static const float DistortionStrength = .015; // Strength of distortion around cracks
static const float CrackRemovalThreshold = 0; // Threshold for removing pixels near cracks

float2 Random(float2 p)
{
    return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f); // Smooth interpolation
    return lerp(
        lerp(Random(i + float2(0.0, 0.0)), Random(i + float2(1.0, 0.0)), u.x),
        lerp(Random(i + float2(0.0, 1.0)), Random(i + float2(1.0, 1.0)), u.x),
        u.y
    );
}

// Function to compute crack effect for a given center
float ComputeCrackGlow(float2 coords, float2 center)
{
    float2 uv = coords - center; // UV relative to this center
    float r = length(uv); // Radius from this center
    float theta = atan2(uv.y, uv.x); // Angle

    // Scale theta to control the number of cracks
    float crackAngle = theta * NumCracks;

    // Add noise to perturb the cracks
    float noise = Noise(coords * CrackScale);
    crackAngle += noise * .85; // Perturb the angle for jagged cracks

    // Create radiating cracks
    float crack = frac(crackAngle); // Fractional part of the angle
    float crackDist = abs(crack - 0.5) * 1.0; // Distance from the crack center
    float crackLine = smoothstep(CrackWidth, CrackWidth * 0.5, crackDist); // Crack line
    
    // Glow along the cracks
    float crackGlow = (1.0 - crackLine) * CrackGlowIntensity;

    // Modulate crack visibility based on completion and radius
    float maxRadius = 0.71; // Max distance in texture space
    float completionRadius = (Completion) * maxRadius;
    float crackFade = smoothstep(completionRadius + 0.1, completionRadius - 0.1, r);
    crackGlow *= crackFade;

    return crackGlow;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Sample the input texture
    float4 texColor = tex2D(tex, coords);
    
    // Discard transparent pixels
    clip(texColor.a - AlphaThreshold);

    // Compute crack glow for each center
    float crackGlow1 = ComputeCrackGlow(coords, CrackCenter1);
    float crackGlow2 = ComputeCrackGlow(coords, CrackCenter2);
    float crackGlow3 = ComputeCrackGlow(coords, CrackCenter3);

    // Combine crack glows (e.g., take the maximum to avoid overlap artifacts)
    float crackGlow = max(max(crackGlow1, crackGlow2), crackGlow3);

    // Use the closest center for distortion (optional refinement)
    float2 closestCenter = CrackCenter1;
    float minDist = length(coords - CrackCenter1);
    float dist2 = length(coords - CrackCenter2);
    if (dist2 < minDist)
    {
        minDist = dist2;
        closestCenter = CrackCenter2;
    }
    float dist3 = length(coords - CrackCenter3);
    if (dist3 < minDist)
    {
        closestCenter = CrackCenter3;
    }

    float2 uv = coords - closestCenter; // UV relative to closest center

    // Distortion: Offset texture coordinates based on crackGlow
    float2 distortion = uv * crackGlow * DistortionStrength;
    float2 distortedCoords = coords + distortion;

    // Sample the texture with distorted coordinates
    float4 distortedColor = tex2D(tex, distortedCoords);

    // Simulate pixel removal near cracks
    float removalFactor = smoothstep(CrackRemovalThreshold, 1.0, crackGlow);
    float alpha = texColor.a * (1.0 - removalFactor); // Fade out near cracks

    // Combine original color with distortion and removal
    float3 finalColor = distortedColor.rgb;
    float4 outputColor = float4(finalColor + crackGlow, alpha);

    // Ensure the output respects the original alpha threshold
    clip(outputColor.a - AlphaThreshold);

    return outputColor * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

/*
float Voronoi(float2 uv)
{
    float2 i = floor(uv); // Integer part of UV
    float2 f = frac(uv); // Fractional part of UV
    float minDist = 1.0; // Minimum distance to a point

    // Check neighboring cells (3x3 grid)
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(float(x), float(y));
            float2 spot = Random(i + neighbor); // Random point in the cell
            float2 diff = neighbor + spot - f;
            float dist = length(diff);
            minDist = min(minDist, dist);
        }
    }
    return minDist;
}
*/