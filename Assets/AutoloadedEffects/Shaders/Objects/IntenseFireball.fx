float2 resolution : register(c0);
float time : register(c1);
float opacity : register(c2);

texture sampleTexture;
sampler2D noise = sampler_state
{
    texture = <sampleTexture>;
    magfilter = ANISOTROPIC;
    minfilter = ANISOTROPIC;
    mipfilter = ANISOTROPIC;
    AddressU = wrap;
    AddressV = wrap;
};

// 3D rotation matrix around an axis
float3x3 rotate(float angle, float3 axis)
{
    // Rodrigues rotation formula
    float c = cos(angle);
    float s = sin(angle);
    float3 ci = (1.0 - c) * axis;
    float3 si = s * axis;
    
    return float3x3(
        ci.x * axis.x + c, ci.x * axis.y + si.z, ci.x * axis.z - si.y,
        ci.y * axis.x - si.z, ci.y * axis.y + c, ci.y * axis.z + si.x,
        ci.z * axis.x + si.y, ci.z * axis.y - si.x, ci.z * axis.z + c
    );
}

float3 rayDirection(float fieldOfView, float2 size, float2 fragCoord)
{
    float2 xy = fragCoord - size * 0.5; // Center
    float z = size.y / tan(radians(fieldOfView) * 0.5); // Determine the distance to the projection plane
    return normalize(float3(xy, -z)); // Negative z for a forward direction
}

float intersect_ray_sphere(float3 origin, float3 direction, float3 center, float radius)
{
    float3 dirToCenter = origin - center;
    float a = dot(direction, direction);
    float b = 2.0 * dot(dirToCenter, direction);
    float c = dot(dirToCenter, dirToCenter) - radius * radius;
    float disc = b * b - 4 * a * c;
    
    // No intersection
    if (disc < 0.0)
        return -1.0;
    
    return (-b - sqrt(disc)) / (2 * a);
}

float3 fire_color(float x)
{
    return
        float3(1.0, 0.4, 0.0) * (x * 1.4)  +
        float3(1.0, 1.0, 1.0) * max(0.0, x - 0.7);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float2 fragCoord = coords * resolution;

    // Camera setup
    float3x3 cameraRot = rotate(0, float3(0.0, 0.0, 1.0)); // identity
    float3 rayDir = mul(cameraRot, rayDirection(120.0, resolution, fragCoord));
    float3 cameraPos = mul(cameraRot, float3(0.0, 0.0, 1.9));

    float3 sphere_pos = float3(0.0, 0.0, 0.0);
    float intensity = 0.0;
    
    // Roation matricies to transform the hit position later on for animation
    float3x3 tex_mat = float3x3(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0); // Identity
    float3x3 wind_mat = float3x3(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);

    // Ray-march multiple spheres
    for (int i = 0; i < 10; i++)
    {
        float radius = 1.0 - float(i) / 60.0;
        float dist = intersect_ray_sphere(cameraPos, rayDir, sphere_pos, radius);
        
        if (dist > 0.0)
        {
            float3 hitPos = cameraPos + rayDir * dist;
            float3 transformedHitPos = mul(mul(tex_mat, wind_mat), hitPos);
            float3 normal = normalize(transformedHitPos - sphere_pos);
            float2 uv = float2(atan2(normal.z, normal.x) / radians(90.0), normal.y) / float(i + 1);
            float noiseVal = tex2D(noise, uv).r;

            intensity += step(1.0 - float(i) / 4.0, noiseVal) * lerp(0, .7, opacity) * noiseVal * max(0.0, dot(float3(0.0, 0.0, 1.0), hitPos));
            
            tex_mat = mul(rotate(radians(11.0) * time, float3(0.3, 0.7, 0.1)), tex_mat);
            wind_mat = mul(rotate(radians(25.0) * time, float3(1.0, 0.0, 0.0)), wind_mat);
        }
    }
    
    float3 fireCol = fire_color(intensity);
    float3 glow = fire_color(3.0) * max(0.0, 1.1 - 2.8 * length(fragCoord - 0.5 * resolution) / resolution.y);
    
    float3 finalColor = 0.1 * glow + fireCol;
    finalColor = floor(finalColor * 32) / 32; // Quantization
    finalColor = pow(abs(finalColor), 1.4); // Contrast
    return lerp(float4(finalColor, 1.0), float4(0, 0, 0, 0), intensity <= 0.2); // Remove black background by checking intensity
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}