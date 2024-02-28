texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
	texture = <Texture0>;
};

float2 ScreenSize;
float4 OutlineColor;
float2 Zoom;

float4 ValorNPCOutline(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
    float4 image = tex2D(TextureSampler0, coords);
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

    return lerp(image, float4(0, 0, 0, 0.1), clamp(outline.a, 0, 1));
}

technique Technique1
{
    pass ValorNPCOutline
    {
        PixelShader = compile ps_3_0 ValorNPCOutline();
    }
}