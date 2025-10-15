sampler circle : register(s0);
sampler noise : register(s1);

float3 mainColor;
float3 secondColor;

float opacity;
float globalTime;

float origRotation;
float rotation;

float2 imageSize;
float2 overallImageSize;

matrix transformMatrix;
float2x2 scalingMatrix;

// Unfortunately on the X and Y planes there is giant white lines that can only be covered up
// Why??? no clue, just use a right tex

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 Coordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 Coordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float2 coords = input.Coordinates - 0.5;
    
    float rotationSine = sin(rotation);
    float rotationCosine = sin(rotation + 1.57079);
    
    float rotationOriginSine = sin(origRotation);
    float rotationOriginCosine = sin(origRotation + 1.57079);
    
    float2x2 circularRotationMatrix = float2x2(rotationCosine, -rotationSine, rotationSine, rotationCosine);
    float2x2 rotationMatrix = float2x2(rotationOriginCosine, -rotationOriginSine, rotationOriginSine, rotationOriginCosine);
    
    output.Color = input.Color;
    
    // Rotate based on direction, squash the result, and then rotate the squashed result by the circular rotation.
    output.Coordinates = mul(input.Coordinates - 0.5, rotationMatrix) + 0.5;
    output.Coordinates = mul(output.Coordinates - 0.5, scalingMatrix) + 0.5;
    output.Coordinates = mul(output.Coordinates - 0.5, circularRotationMatrix) + 0.5;
    output.Position = mul(input.Position, transformMatrix);

    return output;
}

float4 PixelFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.Coordinates;
    
    float overallAdjustedXCoord = lerp(-imageSize.x / overallImageSize.x, imageSize.x / overallImageSize.x, coords.x) + .5 * 0.5;    
    float overallAdjustedYCoord = lerp(-imageSize.y / overallImageSize.y, imageSize.y / overallImageSize.y, coords.y) + .5 * 0.5;
    float2 finalCoords = float2(overallAdjustedXCoord, overallAdjustedYCoord);
    
    float4 baseColor = tex2D(circle, coords);
    float colorFade = abs(sin(overallAdjustedYCoord + globalTime * .5));
    float luminosity = (baseColor.r + baseColor.g + baseColor.b) / 3;
    float4 endColor = baseColor * float4(lerp(mainColor, secondColor, colorFade), 1);
    endColor *= 1 + luminosity * .4;
    float4 finalColor = (endColor * .7 + baseColor * .5) * baseColor.a;
    
    return finalColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelFunction();
    }
}