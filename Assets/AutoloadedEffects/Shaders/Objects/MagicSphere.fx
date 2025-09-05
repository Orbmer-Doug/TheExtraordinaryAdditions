sampler baseTexture : register(s0);
sampler edgeShapeNoiseTexture : register(s1);
sampler innerElectricityNoiseTexture : register(s2);

float globalTime;
float posterizationPrecision;
float2 resolution;
float4 mainColor;

float2 ConvertToPolar(in float2 coords)
{
    coords -= 0.5;
    float r = sqrt(coords.x * coords.x + coords.y * coords.y);
    float theta = atan(coords.y / coords.x);

    return float2(r, theta);
}

// Credit to this method goes to Lucille Karma
float2 GetFakeSphereCoords(float2 coords)
{
    float2 coordsNormalizedToCenter = (coords - 0.5) * 2;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    
    // Calculate coordinates relative to the sphere.
    // This pinch factor effectively ensures that the UVs are relative to a circle, rather than a rectangle.
    // This helps SIGNIFICANTLY for making the texturing look realistic, as it will appear to be traveling on a
    // sphere rather than on a sheet that happens to overlay a circle.
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.001;
    
    // Exaggerate the pinch slightly.
    spherePinchFactor = pow(spherePinchFactor, 1.5);
    
    float2 sphereCoords = frac((coords - 0.5) * spherePinchFactor + 0.5);
    return sphereCoords;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 pixelationFactor = resolution * 0.5;
    coords = floor(coords * pixelationFactor) / pixelationFactor;
     
    float2 oldCoords = coords;
    coords = ConvertToPolar(coords);
    float2 sphereCoords = GetFakeSphereCoords(oldCoords);
    
    float4 tex = tex2D(edgeShapeNoiseTexture, coords - float2(globalTime, 0));
    
    float distanceFromCenter = distance(oldCoords, 0.5) + tex2D(edgeShapeNoiseTexture, oldCoords * 1.5 + float2(0, globalTime)) * 0.05;
    float brightness = smoothstep(.5, .1, distanceFromCenter) / distanceFromCenter * 0.5;
    
    sampleColor = saturate(sampleColor * brightness);
    sampleColor *= tex + mainColor;
    sampleColor *= tex2D(innerElectricityNoiseTexture, sphereCoords - float2(0, -globalTime)) + mainColor;
    
    sampleColor = float4(floor(sampleColor.rgb * posterizationPrecision) / posterizationPrecision, 1) * sampleColor.a;
    
    return sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}