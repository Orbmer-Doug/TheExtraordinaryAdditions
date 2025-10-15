sampler targetContents : register(s1);
sampler overlayTexture : register(s2);

float2 screenSize : register(c0);
float2 layerSize : register(c1);
float2 layerOffset : register(c2);
float4 edgeColor : register(c3);
float2 singleFrameScreenOffset : register(c4);

// usage of these two methods seemingly prevents imprecision problems for some reason...
float2 convertToScreenCoords(float2 coords)
{
    return coords * screenSize;
}

float2 convertFromScreenCoords(float2 coords)
{
    return coords / screenSize;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // this is the calculated from the raw objects in the render target
    float4 baseColor = tex2D(targetContents, coords);
    
    // removes the need for an inverted if (baseColor.a > 0) by ensuring all of the edge checks fail
    float alphaOffset = (1 - any(baseColor.a));
    
    // check if there are any empty pixels nearby
    float left = tex2D(targetContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(-2, 0))).a + alphaOffset;
    float right = tex2D(targetContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(2, 0))).a + alphaOffset;
    float top = tex2D(targetContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(0, -2))).a + alphaOffset;
    float bottom = tex2D(targetContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(0, 2))).a + alphaOffset;
    
    // use step instead of branching in order to determine whether neighboring pixels are invisible
    float leftHasNoAlpha = step(left, 0);
    float rightHasNoAlpha = step(right, 0);
    float topHasNoAlpha = step(top, 0);
    float bottomHasNoAlpha = step(bottom, 0);
    
    // use addition instead of the OR boolean operator to get a 0-1 value for whether an edge is invisible
    float conditionOpacityFactor = 1 - saturate(leftHasNoAlpha + rightHasNoAlpha + topHasNoAlpha + bottomHasNoAlpha);
    
    // make layer colors
    float2 layerCoords = (coords + layerOffset + singleFrameScreenOffset) * screenSize / layerSize;
    float2 res = float2(layerCoords.x * 2, layerCoords.y * 2);
    layerCoords.x -= layerCoords.x % (1 / res);
    layerCoords.y -= layerCoords.y % (1 / res);

    float4 layerColor = tex2D(overlayTexture, layerCoords);
    float4 defaultColor = layerColor * tex2D(targetContents, coords) * sampleColor;
    
    // if conditionOpacityFactor is 1, the default color is zeroed out and the edge is toggled, and vice versa
    return (defaultColor * conditionOpacityFactor) + (edgeColor * sampleColor * (1 - conditionOpacityFactor));
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}