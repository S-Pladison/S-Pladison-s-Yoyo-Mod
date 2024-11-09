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

float4 ScreenFlash(float2 coords : TEXCOORD0) : COLOR0
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

technique Technique1
{
    pass ScreenFlash
    {
        PixelShader = compile ps_3_0 ScreenFlash();
    }
}