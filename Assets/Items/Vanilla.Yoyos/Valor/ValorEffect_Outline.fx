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
float2 Zoom;

float Outline(sampler smp, float2 coords, float2 size)
{
    float4 image = tex2D(smp, coords);
    float4 outline = -4 * image;
    
    outline += tex2D(smp, coords + float2(size.x, 0));
    outline += tex2D(smp, coords + float2(-size.x, 0));
    outline += tex2D(smp, coords + float2(0, size.y));
    outline += tex2D(smp, coords + float2(0, -size.y));
    outline.a = outline.a >= 0.5 ? 1 : 0;
    
    return clamp(outline.a, 0, 1);
}

float4 ValorOutline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 npcColor = tex2D(TextureSampler0, coords);
    float2 pixelSize = float2(2, 2) / ScreenSize * Zoom;
    
    float outline = Outline(TextureSampler0, coords, pixelSize);
    float4 result = lerp(npcColor, float4(OutlineColor.rgb, 1), outline);
    
    if (any(result))
        return result;
    
    outline = Outline(TextureSampler0, coords, pixelSize * 2);
    result = lerp(npcColor, float4(0, 0, 0, 0.1), outline);
    
    if (any(result))
        return result;
    
    outline = Outline(TextureSampler0, coords, pixelSize * 4);
    result = lerp(npcColor, float4(0, 0, 0, 0.025), outline);
    
    return result;
}

technique Technique1
{
    pass ValorOutline
    {
        PixelShader = compile ps_3_0 ValorOutline();
    }
}