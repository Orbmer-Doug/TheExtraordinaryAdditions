float3 centerColor;
float3 edgeColor;

float edgeBlendLength;
float edgeBlendStrength;

float mainOpacity;
float centerOpacity;
float halfSpreadAngle;

static const float Pi = 3.14159274;
static const float PiOver2 = Pi / 2;
static const float TwoPi = Pi * 2;

float realCos(float value)
{
    return sin(value + PiOver2);
}

//Gets the distance of a plot from a line with a specified origin and angle
float distanceFromLine(float2 origin, float angle, float2 plot)
{
    return abs(realCos(angle) * (origin.y - plot.y) - sin(angle) * (origin.x - plot.x));
}

//Gets the distance of a plot from a line with a specified origin and angle, but crops the line so it only expands towards the angle
float distanceFromLineCropped(float2 origin, float angle, float2 plot, float plotAngle)
{
    // If the angle between the line's angle and the plot's angle is less than 90, return the distance from the line
    if (abs(angle - plotAngle) < PiOver2)
        return distanceFromLine(origin, angle, plot);
    
    // If we are behind the line, just give the distance between the start point and the plot
    else
        return length(origin - plot);

}

float4 PixelShaderFunction(float2 uv : TEXCOORD) : COLOR
{
    // Set the uv's Y position to be only contained within the upper half of the texture, to mirror it around a horizontal axis
    if (uv.y > 0.5)
        uv.y = 1 - uv.y;
    
    float2 mappedUv = float2(uv.x - 0.5, abs(uv.y - 0.5));
    
    float2 resolution = float2(850, 850);
    mappedUv.x -= mappedUv.x % (1 / resolution);
    mappedUv.y -= mappedUv.y % (1 / resolution);
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(mappedUv) * 2;
    
    // Crop the sprite into a circle
    if (distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    // Grabs the angle (only as a positive angle, since it's a mirror image udnerneath.
    float angle = atan2(mappedUv.y, mappedUv.x);
    // Grabs the distance of the point from the edge line.
    float distanceFromLine = distanceFromLineCropped(float2(0, 0), halfSpreadAngle, mappedUv, angle);
    
    // Grab the colors to blend the edge lines with.
    float3 color = float3(0, 0, 0);
    float opacity = 0;
    
    // If we are inside the spread's radius.
    if (angle <= halfSpreadAngle)
    {
        color = lerp(centerColor, edgeColor, pow(angle / halfSpreadAngle, 9));
        opacity = centerOpacity * pow(1 - distanceFromCenter, 2);
    }
    
    // If we are further from the line than the edge's blending length, just don't even do the blending.
    if (distanceFromLine > edgeBlendLength)
        return float4(color * opacity * mainOpacity, opacity * mainOpacity);
    
    // The higher this value is, the more we blend with the edge's opacity & color.
    float edgeBlendFactor = pow(1 - distanceFromLine / edgeBlendLength, edgeBlendStrength);
    
    color = lerp(color, edgeColor, edgeBlendFactor);
    opacity = lerp(opacity, 1 * pow(1 - distanceFromCenter, 3), edgeBlendFactor) * mainOpacity;
    
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