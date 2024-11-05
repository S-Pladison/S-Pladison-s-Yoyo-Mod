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

texture Texture1 : register(s1);

sampler TextureSampler1 = sampler_state
{
    texture = <Texture1>;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
    MagFilter = Point;
    MinFilter = Point;
    Mipfilter = Point;
};

float4x4 EffectMatrix;
float2 ScreenSize;
float2 ScreenPosition;
float4 OutlineColor;
float Time;

float Noise(float2 coords)
{
    float2 noiseUv = mul((float4x2) EffectMatrix, coords * ScreenSize);
    noiseUv -= ScreenSize * 0.5;
    noiseUv += ScreenPosition;
    noiseUv *= ScreenSize / ScreenSize.y * 6;
    noiseUv += ScreenSize * 0.5;
    noiseUv /= ScreenSize;
    return tex2D(TextureSampler1, noiseUv).r;
}

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

float FlameOutline(sampler smp, float2 coords, float2 size)
{
    float4 image = tex2D(smp, coords);
    float4 outline = -3 * image;
    
    outline += tex2D(smp, coords + float2(size.x, 0));
    outline += tex2D(smp, coords + float2(-size.x, 0));
    outline += tex2D(smp, coords + float2(0, size.y));
    outline.a = outline.a >= 0.15 ? 1 : 0;
    
    return clamp(outline.a, 0, 1);
}

float4 ValorOutline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 screenColor = tex2D(TextureSampler0, coords);
    float2 pixelSize = float2(2, 2) / ScreenSize;
    
    float noise = Noise(coords + float2(Time * 0.025, Time * 0.05));
    float4 outlineColor = float4(lerp(OutlineColor.rgb, OutlineColor.rgb * 0.25, noise), 1);
    
    // Основная обводка
    {
        float outline = Outline(TextureSampler0, coords, pixelSize);
        float4 result = lerp(screenColor, outlineColor, outline);
    
        if (any(result))
            return result;
    }
    
    // Обводка-пламя
    {
        float outline = FlameOutline(TextureSampler0, coords, pixelSize * 6 * noise);
        float4 result = lerp(screenColor, outlineColor, outline);
    
        if (any(result))
            return result;
    }
    
    // Теневая обводка
    {
        float outline = Outline(TextureSampler0, coords, pixelSize * 2);
        float4 result = lerp(screenColor, float4(0, 0, 0, 0.1), outline);
    
        if (any(result))
            return result;
        
        outline = Outline(TextureSampler0, coords, pixelSize * 4);
        result = lerp(screenColor, float4(0, 0, 0, 0.025), outline);
        
        if (any(result))
            return result;
    }
    
    return screenColor;
}

technique Technique1
{
    pass ValorOutline
    {
        PixelShader = compile ps_3_0 ValorOutline();
    }
}