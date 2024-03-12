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

float4 BellowingThunderSilhouette(float2 coords : TEXCOORD0) : COLOR0
{
    float4 screenColor = tex2D(uImage0, coords); 
    float4 maskColor = tex2D(uImage1, coords);
    
    float factor = pow(1.0 - (maskColor.r + maskColor.g + maskColor.b) / 3.0, 5.0);
    
    return lerp(screenColor, float4(factor, factor, factor, 1.0), uIntensity);
}

technique Technique1
{
    pass BellowingThunderSilhouettePass
    {
        PixelShader = compile ps_3_0 BellowingThunderSilhouette();
    }
}