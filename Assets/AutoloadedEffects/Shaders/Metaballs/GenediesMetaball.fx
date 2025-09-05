sampler2D InfluenceMap : register(s1);
float globalTime;

float threshold;
float epsilon;
float4 color;

static const float iterations = 17;
static const float formuparam = 0.7; // Controls fractal shape deformation

static const float volsteps = 20;
static const float stepsize = .1;

static const float zoom = .900;
static const float tileSize = .850;
static const float speed = .010;

static const float brightness = .0015;
static const float darkmatter = .400;
static const float distfading = .730;
static const float saturation = .850;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float influence = tex2D(InfluenceMap, uv).r;
    float alpha = smoothstep(threshold - epsilon, threshold + epsilon, influence);
    
    uv = round(uv * 600) / 600;
    float3 rayDir = float3(uv * zoom, 1);
    float time = globalTime * speed + .45;
    
    float angle1 = .5 * 2;
    float angle2 = .8 * 2;
    float2x2 rotation1 = float2x2(cos(angle1), sin(angle1), -sin(angle1), cos(angle1));
    float2x2 rotation2 = float2x2(cos(angle2), sin(angle2), -sin(angle2), cos(angle2));
    rayDir.xz = mul(rayDir.xz, rotation1);
    rayDir.xy = mul(rayDir.xy, rotation2);
    float3 rayOrigin = float3(1., .5, .5);
    rayOrigin += float3(time * 2., time, -2.);
    rayOrigin.xz = mul(rayOrigin.xz, rotation1);
    rayOrigin.xy = mul(rayOrigin.xy, rotation2);
	
	// Volumetric rendering
    float stepDistance = 0.4;
    float fade = 1;
    float3 accumulatedColor = float3(0, 0, 0);
    for (int step = 0; step < volsteps; step++)
    {
        float3 position = rayOrigin + stepDistance * rayDir * .5;
        
        // Tiling fold
        position = abs(float3(tileSize, tileSize, tileSize) - fmod(position, float3(tileSize * 2, tileSize * 2, tileSize * 2)));
        float prevDistance, a = prevDistance = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            // The magical fractal formula
            position = abs(position) / dot(position, position) - formuparam;
            
            // Absolute sum of average change
            a += abs(length(position) - prevDistance);
            prevDistance = length(position);
        }
        
        // Dark matter based on distance
        float darkMatter = max(0., darkmatter - a * a * .001); 
        
        // Contrast
        a *= a * a;
        
        if (step > 6)
            fade *= 1 - darkMatter;
        
        accumulatedColor += fade;
        
        // Coloring based on distance
        accumulatedColor += float3(stepDistance, stepDistance * stepDistance, stepDistance * stepDistance * stepDistance * stepDistance * stepDistance) * a * brightness * fade;
        fade *= distfading;
        stepDistance += stepsize;
    }
    
    float gray = length(accumulatedColor);
    accumulatedColor = lerp(float3(gray, gray, gray), accumulatedColor, saturation);
    return float4(float4(accumulatedColor * .008, 1).rgb, alpha) * (alpha * 2);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}