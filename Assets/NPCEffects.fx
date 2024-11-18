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
float OutlineThickness;
float4 OutlineColor;
float4 NPCColor;

float4 Outline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 screenColor = tex2D(TextureSampler0, coords);
    float2 outlineSize = float2(OutlineThickness, OutlineThickness) / ScreenSize;
    
    if (any(screenColor))
        return NPCColor;
    
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
    pass Outline
    {
        PixelShader = compile ps_3_0 Outline();
    }
}