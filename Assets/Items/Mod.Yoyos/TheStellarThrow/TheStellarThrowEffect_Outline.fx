texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
    MagFilter = Point;
    MinFilter = Point;
    Mipfilter = Point;
};

float2 ScreenSize;
float4 OutlineColor;

float4 TheStellarThrowOutline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 screenColor = tex2D(TextureSampler0, coords);
    float2 outlineSize = float2(1.5, 1.5) / ScreenSize;
    
    if (any(screenColor))
        return OutlineColor * 0.4;
    
    if (any(tex2D(TextureSampler0, coords + float2(outlineSize.x, outlineSize.y))))
        return OutlineColor;
    
    if (any(tex2D(TextureSampler0, coords + float2(outlineSize.x, -outlineSize.y))))
        return OutlineColor;
    
    if (any(tex2D(TextureSampler0, coords + float2(-outlineSize.x, outlineSize.y))))
        return OutlineColor;
    
    if (any(tex2D(TextureSampler0, coords + float2(-outlineSize.x, -outlineSize.y))))
        return OutlineColor;
    
    return screenColor;
}

technique Technique1
{
    pass TheStellarThrowOutline
    {
        PixelShader = compile ps_3_0 TheStellarThrowOutline();
    }
}