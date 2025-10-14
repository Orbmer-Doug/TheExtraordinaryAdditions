// Adapted from GLSL by S. Guillitte (CC BY-NC-SA 3.0)
// It's a little scrungly in rotation cause I can't figure out how to describe quaternions in shaders

// Constants defining the galaxy's appearance
static const float Zoom = 2.75;
static const float2x2 NoiseRotationMatrix = float2x2(0.8, 0.6, -0.6, 0.8);
static const float PI = 3.14159265359;

// Uniforms for dynamic control
float2 Size : register(c1);
float Time : register(c2);
float3 ColorTint : register(c3);
float ForwardRotation : register(c4);
float SpiralArmCount : register(c5); // 3 default
float SpiralWinding : register(c6); // Tightness of the spiral arms, 12 default
float BulgeAmount : register(c7); // 16 default
float DustDensity : register(c8); // 25 default
bool Negative : register(c9);

// Creates a 2D rotation matrix for a given angle
float2x2 CreateRotationMatrix(float angle)
{
    return float2x2(cos(angle), sin(angle), -sin(angle), cos(angle));
}

// Generates 2D noise for galaxy texture variation
float Generate2DNoise(float2 position)
{
    float noiseValue = 0.0;
    float frequency = 2.0;
    for (int i = 0; i < 4; i++)
    {
        position = mul(NoiseRotationMatrix, position) * frequency + 0.6;
        frequency *= 1.0;
        noiseValue += sin(position.x + sin(2.0 * position.y));
    }
    return noiseValue / 4.0;
}

// Generates 3D noise for more complex galaxy details
float Generate3DNoise(float3 position)
{
    position *= 2.0;
    float noiseValue = 0.0;
    float frequency = 1.0;
    for (int i = 0; i < 3; i++)
    {
        position.xy = mul(NoiseRotationMatrix, position.xy);
        position = position.zxy * frequency + 0.6;
        frequency *= 1.15;
        noiseValue += sin(position.y + 1.3 * sin(1.2 * position.x) + 1.7 * sin(1.7 * position.z));
    }
    return noiseValue / 3.0;
}

// Fractal Brownian Motion for disk-like galaxy structures
float GenerateDiskFBM(float3 position)
{
    float frequency = 1.0;
    float result = 0.0;
    for (int i = 1; i < 5; i++)
    {
        result += abs(Generate3DNoise(position * frequency)) / frequency;
        frequency += frequency;
    }
    return 1.2 / (0.07 + result);
}

// FBM for dust-like galaxy effects
float GenerateDustFBM(float3 position)
{
    float frequency = 1.0;
    float result = 0.0;
    for (int i = 1; i < 5; i++)
    {
        result += 1.0 / abs(Generate3DNoise(position * frequency)) / frequency;
        frequency += frequency;
    }
    return pow(abs(1.0 - 1.0 / (0.01 + result)), 4.0);
}

// Calculates the spiral angle for galaxy arms
float CalculateSpiralAngle(float radius, float windingBase, float windingFactor)
{
    return atan(exp(1.0 / radius) / windingBase) * 2.0 * windingFactor;
}

// Defines the shape and intensity of a single spiral arm
float CalculateSpiralArm(float armCount, float armWidth, float windingBase, float windingFactor, float2 position)
{
    float angle = atan2(position.y, position.x);
    float radius = length(position);
    return pow(1.0 - 0.15 * sin((CalculateSpiralAngle(radius, windingBase, windingFactor) - angle) * armCount), armWidth) * exp(-radius * radius) * exp(-0.07 / radius);
}

// Defines the central bulge of the galaxy
float CalculateCentralBulge(float2 position)
{
    float radius = exp(-dot(position, position) * 1.2);
    return (.8 * radius + 3.0 * exp(-dot(position, position) * 16.0));
}

// Maps a 3D point to a density value for the galaxy shape
float MapGalaxyDensity(float3 position)
{
    float2 xzPosition = position.xz;
    float radius = length(xzPosition);
    
    // Add radial falloff to limit galaxy extent (fade out beyond radius 1.5)
    float radialFalloff = exp(-max(0.0, radius - 1.5) * 4.0);
    float armDensity = CalculateSpiralArm(SpiralArmCount, 6.0, 0.7, SpiralWinding, xzPosition);
    float bulgeDensity = max(armDensity + 0.5, CalculateCentralBulge(xzPosition));
    float density = 4.0 * exp(-DustDensity * (abs(position.y) - bulgeDensity / BulgeAmount));
    return density * radialFalloff; // Apply falloff to constrain galaxy
}

// Raymarches through the galaxy to compute color and density
float3 RaymarchGalaxy(float3 rayOrigin, float3 rayDirection)
{
    float rayDistance = 1.5;
    float stepSize = 0.065;
    float3 color = float3(0.0, 0.0, 0.0);
    float density = 0.0, diskScale = 0.0, dustScale = 0.0;
    for (int i = 0; i < 60; i++)
    {
        rayDistance += stepSize * exp(-0.2 * density * diskScale);
        if (rayDistance > 6.0)
            break;
        float3 position = rayOrigin + rayDistance * rayDirection;
        
        density = MapGalaxyDensity(position);
        if (density > 0.2)
        {
            diskScale = GenerateDiskFBM(position * 32.0) / 1.5;
            dustScale = GenerateDustFBM(position * 4.0) / 4.0;
            density *= dustScale;
        }
        
        // 0, 1.3, 1.8 = whispy blue
        // 3, 1.3, 1.8 = fiery red
        float3 temp = ColorTint;
        color = 0.98 * color + 0.02 * float3(temp.r * density * density, temp.g * density * diskScale, temp.b * diskScale) * density;
    }
    return 0.8 * color;
}

struct VertexInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 PixelShaderFunction(VertexInput input) : COLOR
{
    // Normalize texture coordinates for rendering
    float2 normalizedCoords = input.TextureCoordinates * Zoom;
    float2 screenSpacePosition = -Zoom + 2.0 * normalizedCoords;
    screenSpacePosition.x *= Size.x / Size.y; // Adjust for aspect ratio
    
    // Adjust camera rotation based on mouse position
    float2 cameraRotation = ForwardRotation; // Range: [-pi, pi]
    cameraRotation.x += Time;
    
    // Set up camera for viewing the galaxy
    float3 cameraPosition = float3(2.0, 2.0, 2.0); // Position camera diagonally
    cameraPosition.yz = mul(CreateRotationMatrix(cameraRotation.y - 0.70710678118), cameraPosition.yz);
    cameraPosition.xz = mul(CreateRotationMatrix(cameraRotation.x), cameraPosition.xz);
    float3 cameraTarget = float3(0.0, 0.0, 0.0);
    float3 forward = normalize(cameraTarget - cameraPosition);
    float3 right = normalize(cross(forward, float3(0.0, 1.0, 0.0)));
    float3 up = normalize(cross(right, forward));
    float3 rayDirection = normalize(screenSpacePosition.x * right + screenSpacePosition.y * up + 4.0 * forward);
    
    // Raymarch to compute galaxy color
    float3 galaxyColor = RaymarchGalaxy(cameraPosition, rayDirection);
    
    // Apply shading and clamp colors
    galaxyColor = 0.5 * log(1.0 + galaxyColor);
    galaxyColor = clamp(galaxyColor, 0.0, 1.0);
    
    // Compute alpha for smooth transparency
    float alphaThreshold = 0.1;
    float alpha = saturate((length(galaxyColor) - alphaThreshold) / alphaThreshold);
    
    galaxyColor = lerp(galaxyColor, (1.0 - galaxyColor) * alpha, Negative);
    
    return float4(galaxyColor, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}