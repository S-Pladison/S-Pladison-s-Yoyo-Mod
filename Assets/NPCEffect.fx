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
float2 Zoom;
float OutlineThickness;
float4 OutlineColor;
float4 NPCColor;

const float MinAlpha = 0.5;

float HasSprite(float2 uv)
{
    return tex2D(TextureSampler0, uv).a >= MinAlpha ? 1.0 : 0.0;
}

float NearSprite(float2 uv, float2 size)
{
    float hit = 0.0;
    hit += HasSprite(uv + float2(size.x, 0));
    hit += HasSprite(uv + float2(-size.x, 0));
    hit += HasSprite(uv + float2(0, size.y));
    hit += HasSprite(uv + float2(0, -size.y));
    hit += HasSprite(uv + float2(size.x, size.y));
    hit += HasSprite(uv + float2(size.x, -size.y));
    hit += HasSprite(uv + float2(-size.x, size.y));
    hit += HasSprite(uv + float2(-size.x, -size.y));
    return saturate(hit);
}

float4 Outline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 screenColor = tex2D(TextureSampler0, coords);
    float2 pixel = (1.0 / ScreenSize) * Zoom;
    float2 outlineSize = pixel * OutlineThickness;

    // Заливка
    if (screenColor.a >= MinAlpha)
        return NPCColor;

    // Основная обводка
    if (NearSprite(coords, outlineSize) > 0.0)
        return OutlineColor;

    // Тень снизу-справа
    if (NearSprite(coords - outlineSize, outlineSize) > 0.0)
        return float4(0, 0, 0, 0.28 * OutlineColor.a);

    return screenColor;
}

technique Technique1
{
    pass Outline
    {
        PixelShader = compile ps_3_0 Outline();
    }
}
