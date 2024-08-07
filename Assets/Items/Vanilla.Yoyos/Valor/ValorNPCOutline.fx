texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
};

float2 ScreenSize;
float4 OutlineColor;
float2 Zoom;
float Time;

float Outline(sampler smp, float2 coords, float2 size)
{
    float4 image = tex2D(smp, coords);
    float4 outline = -4 * image;
    
    outline += tex2D(smp, coords + float2(size.x, 0));
    outline += tex2D(smp, coords + float2(-size.x, 0));
    outline += tex2D(smp, coords + float2(0, size.y));
    outline += tex2D(smp, coords + float2(0, -size.y));
    outline.a = min(outline.a * 255, 1);
    
    return clamp(outline.a, 0, 1);
}

float4 ValorNPCOutline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    /*float4 image = tex2D(TextureSampler0, coords);
    float2 offsetSize = coords / ScreenSize * 4 * Zoom;
    float4 outline = -4 * image;
	
    outline += tex2D(TextureSampler0, coords + float2(offsetSize.x, 0));
    outline += tex2D(TextureSampler0, coords + float2(-offsetSize.x, 0));
    outline += tex2D(TextureSampler0, coords + float2(0, offsetSize.y));
    outline += tex2D(TextureSampler0, coords + float2(0, -offsetSize.y));
    outline.a = min(outline.a * 255, 1);

    float4 result = lerp(image, OutlineColor, clamp(outline.a, 0, 1));
    
    if (any(result))
        return result;
    
    offsetSize *= 2.5;
    outline = -4 * image;
	
    outline += tex2D(TextureSampler0, coords + float2(offsetSize.x, 0));
    outline += tex2D(TextureSampler0, coords + float2(-offsetSize.x, 0));
    outline += tex2D(TextureSampler0, coords + float2(0, offsetSize.y));
    outline += tex2D(TextureSampler0, coords + float2(0, -offsetSize.y));
    outline.a = min(outline.a * 255, 1);

    return lerp(image, float4(0, 0, 0, 0.1), clamp(outline.a, 0, 1));*/
    
    
    
    /*float4 npcTextureColor = tex2D(TextureSampler0, coords);
    float2 offsetSize = coords / ScreenSize * Zoom * 2;

    float4 outline = -4 * npcTextureColor;
    outline += tex2D(TextureSampler0, coords + float2(offsetSize.x, 0));
    outline += tex2D(TextureSampler0, coords + float2(-offsetSize.x, 0));
    outline += tex2D(TextureSampler0, coords + float2(0, offsetSize.y));
    outline += tex2D(TextureSampler0, coords + float2(0, -offsetSize.y));
    outline.a = min(outline.a * 255, 1);

    return lerp(npcTextureColor, OutlineColor, clamp(outline.a, 0, 1));*/
    
    float4 image = tex2D(TextureSampler0, coords);
    float2 coordScreen = coords / ScreenSize * Zoom;
    
    float3 color = OutlineColor.rgb * 0.75;
    float outline = Outline(TextureSampler0, coords, coordScreen * 4);
    float4 result = lerp(image, float4(color, 0.5), outline);
    
    if (any(result))
        return result;
    
    color = OutlineColor * 0.33;  
    outline = Outline(TextureSampler0, coords, coordScreen * 6);
    result = lerp(image, float4(color, 0), outline);
    
    if (any(result))
        return result;
    
    color = OutlineColor * 0.1; 
    outline = Outline(TextureSampler0, coords, coordScreen * 12);
    result = lerp(image, float4(color, 0), outline);
    
    return result;
}

technique Technique1
{
    pass ValorNPCOutline
    {
        PixelShader = compile ps_3_0 ValorNPCOutline();
    }
}