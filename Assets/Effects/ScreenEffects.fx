sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float2 uImageSize1;
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float4 Flash(float2 coords : TEXCOORD0) : COLOR0
{
    float step = 0.1;
    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float4 result = tex2D(uImage0, coords);
    float2 offset = (coords - targetCoords) * step;

    for (float i = step; i <= 1; i += step)
    {
        float2 coordOffset = offset * i;
        float strength = uIntensity * (1 - i - step);
        
        result += tex2D(uImage0, coords + coordOffset) * strength;
        result += tex2D(uImage0, coords - coordOffset) * strength;
    }

    return result;
}

float4 Distortion(float2 coords : TEXCOORD0) : COLOR0
{
    float4 distortionColor = tex2D(uImage1, coords);
    float2 offset = float2(distortionColor.r - 0.5, distortionColor.g - 0.5) * distortionColor.b * 0.0225;

    return tex2D(uImage0, coords + offset);
}

technique Technique1
{
    pass FlashPass
    {
        PixelShader = compile ps_3_0 Flash();
    }

    pass DistortionPass
    {
        PixelShader = compile ps_3_0 Distortion();
    }
}