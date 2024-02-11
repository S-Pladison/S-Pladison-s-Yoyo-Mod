texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
	texture = <Texture0>;
};

float2 ScreenSize;
float4 OutlineColor;
float2 Zoom;

float4 ValorEffect(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
	float2 offsetSize = coords / ScreenSize * 4 * Zoom;
	float4 imageColor = tex2D(TextureSampler0, coords);
	float4 outline = -4.0 * imageColor;

	outline += tex2D(TextureSampler0, coords + float2(offsetSize.x, 0));
	outline += tex2D(TextureSampler0, coords + float2(-offsetSize.x, 0));
	outline += tex2D(TextureSampler0, coords + float2(0, offsetSize.y));
	outline += tex2D(TextureSampler0, coords + float2(0, -offsetSize.y));

	if (any(imageColor))
		return float4(0, 0, 0, 0);

	float4 color = lerp(imageColor, OutlineColor, clamp(outline.a, 0, 1));

	return lerp(imageColor, OutlineColor, clamp(outline.a, 0, 1)) * sampleColor;
}

technique Technique1
{
	pass ValorEffect
	{
		PixelShader = compile ps_3_0 ValorEffect();
	}
}