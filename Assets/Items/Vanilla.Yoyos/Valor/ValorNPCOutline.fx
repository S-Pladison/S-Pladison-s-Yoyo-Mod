texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
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

float4 ValorNPCOutline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 npcColor = tex2D(TextureSampler0, coords);
    float2 coordScreen = coords / ScreenSize * Zoom;
    
    float3 color = OutlineColor.rgb;
    float outline = Outline(TextureSampler0, coords, coordScreen * 4);
    float4 result = lerp(npcColor, float4(color, 0), outline);
    
    if (any(result))
        return result;
    
    color = OutlineColor * 0.15;
    outline = Outline(TextureSampler0, coords, coordScreen * 10);
    result = lerp(npcColor, float4(color, 0), outline);
    
    if (any(result))
        return result;
    
    color = OutlineColor * 0.05;
    outline = Outline(TextureSampler0, coords, coordScreen * 20);
    result = lerp(npcColor, float4(color, 0), outline);
    
    return result;
}

technique Technique1
{
    pass ValorNPCOutline
    {
        PixelShader = compile ps_3_0 ValorNPCOutline();
    }
}