sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

// Parameters for animation and customization
float globalTime; // Time variable for scrolling animation
float scrollSpeed; // Speed of texture scrolling
float fadeRadius; // Radius within which the center fades out
float3 innerColor; // Color at the center of the shield
float3 outerColor; // Color at the edge of the shield
float blendExponent; // Exponent to control the blending curve between colors
float viewAngle;

// Simple hash function for noise
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// 2D value noise
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i + float2(0.0, 0.0));
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p)
{
    float v = 0.0;
    float a = 2;
    float2 shift = float2(100.0, 100.0);

    for (int i = 0; i < 12; i++)
    {
        v += a * noise(p);
        p = p * 2.0 + shift;
        a *= 0.5;
    }
    return v;
}

static const float PI = 3.141592654;
static const float PIOver2 = 1.570796327;
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float res = 250;
    coords = round(coords * res) / res;
    
    // Center the coordinates (range [-0.5, 0.5])
    float2 centeredCoords = coords - 0.5;
    float u = centeredCoords.x; // z-axis in 3D (horizontal)
    float v = centeredCoords.y; // y-axis in 3D (vertical)
    float r = length(centeredCoords); // Radial distance from center
    
    // Clip pixels outside the sphere
    if (r > 0.5)
        discard;

    // Map viewAngle (-1 to 1) to rotation angle (-Pi/2 to Pi/2)
    float rotationAngle = viewAngle * PIOver2;

    // Compute sine and cosine for rotation
    float cosAngle = sin(rotationAngle + PIOver2);
    float sinAngle = sin(rotationAngle);

    // Rotate the (x, y) coordinates around the z-axis
    float z = u;
    float y = v * cosAngle; // Temporary y before x is computed
    float x_unrotated = sqrt(0.25 - u * u - v * v); // x before rotation
    float x = x_unrotated * cosAngle;
    y = v * cosAngle - x_unrotated * sinAngle; // Final y after rotation

    // Normalize the 3D position to get a point on the unit sphere
    float3 pos = float3(x, y, z) / 0.5; // Scale to unit sphere (radius 1)

    // Compute texture coordinates using equirectangular projection
    float phi = atan2(pos.z, pos.x);
    float theta = acos(pos.y); // Angle from the north pole

    // Map to texture coordinates
    float u_tex = phi / (2 * PI) + 0.5;
    float v_tex = theta / PI; // Map theta to [0, 1]

    // Smooth out the seam near the poles
    float poleBlend = smoothstep(0.9, 1.0, abs(pos.y)); // Blend when |y| is close to 1
    u_tex = lerp(u_tex, 0.5, poleBlend); // Blend u_tex towards a constant value near poles

    // Adjust scrolling to respect the view angle
    float scrollOffset = globalTime * scrollSpeed * .3;
    u_tex = frac(u_tex + scrollOffset - viewAngle * 0.5);
    
    // Sample the texture
    float2 flowOffset = float2(0.0, 0.0);
    float flowScale = .6;
    flowOffset.x = fbm(float2(u_tex, v_tex) * flowScale + float2(globalTime * .1, 0.0));
    flowOffset.y = fbm(float2(u_tex, v_tex) * flowScale + float2(0.0, globalTime * .1));
    
    float4 texColor = tex2D(uImage1, flowOffset);
    float4 texColor2 = tex2D(uImage0, float2(u_tex, v_tex));
    texColor += pow(r, 9) + pow(r, 4) * 0.6;
    
    float brightness = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
    texColor.rgb = pow(texColor.rgb, 1.5) * 2; // Increase contrast
    texColor.rgb += brightness * 0.5; // Add glow effect
    texColor2 = lerp(texColor2, texColor * .2, r * 2);
    
    // Color blending from inner to outer
    float t = r / 0.5; // Normalize r to [0, 1]
    float3 shieldColor = lerp(innerColor, outerColor, pow(t, blendExponent));

    // Central fade (transparent near center)
    float opacity = smoothstep(0, fadeRadius, r);

    float h = .90;
    float j = 1 - h;
    if (r * 2 > h)
        opacity *= (1 - ((r * 2 - h) / j));
    
    // Combine texture and shield color
    float3 finalColor = texColor2.rgb * opacity * shieldColor;
    finalColor = pow(finalColor, .9) * 4;
    
    // Return final color with opacity
    return float4(finalColor, opacity);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

/*
// Pixel shader function
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Shift texture coordinates to center the origin at (0.5, 0.5)
    float2 centeredCoords = coords - 0.5;
    
    // Compute radial distance from the center
    float r = length(centeredCoords);
    
    // Clip pixels outside the circular boundary (radius = 0.5)
    clip(0.5 - r);
    
    // Compute spherical coordinate theta (azimuthal angle)
    float theta = atan2(centeredCoords.y, centeredCoords.x);
    
    // Texture u-coordinate with scrolling (rotates around the sphere)
    float u = frac(theta / (2 * 3.1415926535) + 0.5 + globalTime * scrollSpeed);
    
    // Texture v-coordinate based on radial distance (0 at center, 1 at edge)
    float v = r / 0.5;
    
    // Sample the texture
    float4 texColor = tex2D(uImage0, float2(u, v));
    
    // Compute blending factor with exponent for customizable transition
    float t = pow(r / 0.5, blendExponent);
    
    // Blend between inner and outer colors
    float3 shieldColor = lerp(innerColor, outerColor, t);
    
    // Compute opacity with a smooth fade-out at the center
    float opacity = smoothstep(0, fadeRadius, r);
    
    // Combine texture color with shield color
    float3 finalColor = texColor.rgb * shieldColor;
    
    // Return final color with opacity
    return float4(finalColor, opacity);
}
*/
