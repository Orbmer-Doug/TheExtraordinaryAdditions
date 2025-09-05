uniform float time;
uniform float2 mouse;
uniform float2 resolution;

static const float StarThreshold = .97;
static const float3 NebulaBaseColor = float3(.03, .08, .25);
static const float3 NebulaHighlightColor = float3(.15, .35, .55);
static const float3 StarBaseColor = float3(.0, .2, .4);

// Random number generator based on sine
float random(float2 coords)
{
    float2 r = frac(sin(coords) * 2.7644437);
    return frac(r.y * 276.44437 + r.x);
}

// For pseudo-random values
float hash(float value)
{
    return frac(sin(value) * 43758.5453123);
}

float generateStar(float2 gridCoords, float time)
{
    float randomVal = random(floor(gridCoords));
    float twinkle = .7 + .3 * sin(time * 2 + randomVal * 123);
    return 0.004 + smoothstep(StarThreshold, 1.0, randomVal) * max(0, sin(randomVal * 34433 + time)) * twinkle;
}

float3 averageStarBrightness(float2 coords, float offset, float3 baseColor)
{
    float2 offsetVec = float2(0, offset);
    return baseColor * (
    generateStar(coords, time) +
    generateStar(coords + offsetVec, time) +
    generateStar(coords + offsetVec.yx, time) +
    generateStar(coords - offsetVec, time) +
    generateStar(coords - offsetVec.yx, time));
}

float3 renderStarfield(float2 coords, float scale, float brightness)
{
    float3 color = float3(.35, .35, .35) * brightness;
    for (float i = 5; i > 0; --i)
        color += lerp(color, averageStarBrightness(coords, i * scale, StarBaseColor), 1.44);
    return color + generateStar(coords, time) * brightness;
}

// Nebula calculations
float noise(float2 coords)
{
    float2 integerPart = floor(coords);
    float2 fractionalPart = frac(coords);
    
    // Sample noise at four corners
    float a = random(integerPart);
    float b = random(integerPart + float2(1, 0));
    float c = random(integerPart + float2(0, 1));
    float d = random(integerPart + float2(1, 1));
    
    // Smooth interpolation
    float2 u = fractionalPart * fractionalPart * (3 - 2 * fractionalPart);
    
    return lerp(a, b, u.x) + (c - a) * u.y * (1 - u.x) + (d - b) * u.x * u.y;
}

static const float2x2 rot = float2x2(0.877583, 0.479426, -0.479426, 0.877583);
float fbm(float2 coords, int octaves)
{
    float value = 0;
    float amplitude = 0.5;
    float frequency = 1.0;
    float2 shift = float2(100, 100); // prevent tiling artifacts
    
    for (int i = 0; i < octaves; ++i)
    {
        value += amplitude * noise(coords * frequency);
        coords = mul(rot, coords * 2 + shift);
        amplitude *= .5;
        frequency *= 2;
    }
    return value;
}

struct StarLayer
{
    float scale;
    float density;
    float speed;
    float brightness;
};

static StarLayer starLayers[3] =
{
    {
        125, .5, -.14, .4 // Farthest, dim layer
    },
    {
        100, .5, -.08, .7 // Middle layer
    },
    {
        50, 1, -.01, 1 // Closest, bright layer
    }
};

float3 renderNebula(float2 coords, float time)
{
    float2 normalCoords = (coords - .5);
    normalCoords += mouse * .1;
    
    // Scale and scroll
    normalCoords *= 2;
    coords += float2(time * starLayers[1].speed, 0);
    
    // Generate the cloudy texture
    float noiseValue = fbm(coords, 3);
    noiseValue = smoothstep(.3, .85, noiseValue) * .4; // Wispy clouds
    
    float3 nebulaColor = lerp(NebulaBaseColor, NebulaHighlightColor, noiseValue);
    float glow = pow(noiseValue, 1.5) * 1.5;
    nebulaColor += glow * NebulaHighlightColor;
    
    return nebulaColor;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords.x *= resolution.x / resolution.y;
    
    float3 finalColor = float3(0, 0, 0);
    
    // Add the nebula
    finalColor += renderNebula(coords, time);

    // Render all layers
    for (int i = 0; i < 3; ++i)
    {
        StarLayer layer = starLayers[i];
        float2 layerCoords = coords + float2(fmod(time * layer.speed, 120000), 0);
        layerCoords += mouse * layer.scale * .000001;

        float2 normalCoords = layerCoords - .5;
        
        float2 scaledCoords = normalCoords * layer.scale;
        float pixelSize = 1 / layer.scale;
        scaledCoords = round(scaledCoords / pixelSize) * pixelSize;
        
        float3 layerColor = renderStarfield(scaledCoords, layer.density, layer.brightness);
        finalColor += layerColor;
    }

    // Gamma correction
    finalColor = pow(abs(finalColor), float3(1.8, 1.8, 1.8));
    
    return float4(finalColor, 1);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}