sampler tex : register(s0);
float3 color;
float3 darkerColor;
float bloomSize;
float bloomMaxOpacity;
float bloomFadeStrength;
float mainOpacity;
float rotation;
float width;
float lightStrength;
float noiseOffset;
float2 resolution;

static const float Pi = 3.14159274;
static const float PiOver2 = Pi / 2;
static const float TwoPi = Pi * 2;

float realCos(float value)
{
    return sin(value + PiOver2);
}

// HLSL's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom modulo
float mod(float x, float y)
{
    return x - floor(x / y) * y;
}

// Gets the distance of a plot from a line with a specified origin and angle
float distanceFromLine(float2 origin, float angle, float2 plot)
{
    return abs(realCos(angle) * (origin.y - plot.y) - sin(angle) * (origin.x - plot.x));
}

// Gets the distance of a plot from a line with a specified origin and angle, but crops the line so it only expands towards the angle
float distanceFromLineCropped(float2 origin, float angle, float2 plot, float plotAngle)
{
    // If the angle between the line's angle and the plot's angle is less than 90, return the distance from the line
    if (abs(mod(angle - plotAngle + Pi, TwoPi) - Pi) < PiOver2)
        return distanceFromLine(origin, angle, plot);
    
    // If we are behind the line, just give the distance between the start point and the plot
    else
        return length(origin - plot);
}

float4 PixelShaderFunction(float2 uv : TEXCOORD) : COLOR
{
    uv.x -= uv.x % (1 / resolution.x);
    uv.y -= uv.y % (1 / resolution.y);
    
    float2 mappedUv = float2(uv.x - 0.5, (1 - uv.y) - 0.5);
    
    float halfLaserWidth = width / 2;
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(mappedUv) * 2;
    
    // Crop the sprite into a circle
    if (distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    // Grabs the angle (only as a positive angle, since it's a mirror image udnerneath.
    float angle = atan2(mappedUv.y, mappedUv.x);
    
    // Grabs the distance of the point from the edge line.
    float distanceFromLine = distanceFromLineCropped(float2(0, 0), rotation, mappedUv, angle);
    
    // If we are further from the line than the bloom's blending length, just don't.
    if (distanceFromLine > bloomSize + halfLaserWidth)
        return float4(0, 0, 0, 0);
    
    float4 noise = tex2D(tex, float2((distanceFromCenter + noiseOffset) % 1, distanceFromLine / (bloomSize + halfLaserWidth)));
    float3 laserColor = lerp(color, darkerColor, noise.r);
    float laserOpacity = (1 - pow(distanceFromCenter, lightStrength)) * mainOpacity;
    
    if (distanceFromLine <= halfLaserWidth)
        return float4(laserColor * laserOpacity, laserOpacity);
    
    // The higher this value is, the more we blend with the edge's opacity & color.
    float bloomBlendFactor = pow(1 - (distanceFromLine - halfLaserWidth) / bloomSize, bloomFadeStrength);
    float3 color = lerp(float3(0, 0, 0), laserColor, bloomBlendFactor);
    float opacity = lerp(0, laserOpacity, bloomBlendFactor) * mainOpacity * bloomMaxOpacity;
    
    color *= opacity;
    return float4(color, opacity);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}