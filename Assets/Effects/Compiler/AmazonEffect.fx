texture Texture0 : register(s0);
texture Texture1 : register(s1);

sampler TextureSampler0 = sampler_state
{
	texture = <Texture0>;
};

sampler TextureSampler1 = sampler_state
{
	texture = <Texture1>;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MagFilter = Linear;
	MinFilter = Linear;
	Mipfilter = Linear;
};

float2 ScreenSize;
float2 Zoom;

float4 AmazonEffect(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
	float2 offsetSize = coords / ScreenSize * 4 * Zoom;

	float4 imageColor = tex2D(TextureSampler0, coords);

	float4 alpha = -4.0 * imageColor;

	alpha += tex2D(TextureSampler0, coords + float2(offsetSize.x, 0));
	alpha += tex2D(TextureSampler0, coords + float2(-offsetSize.x, 0));
	alpha += tex2D(TextureSampler0, coords + float2(0, offsetSize.y));
	alpha += tex2D(TextureSampler0, coords + float2(0, -offsetSize.y));

	float4 outlineColor = float4(0, 0, 0, 0.75);
	float4 color = lerp(imageColor, outlineColor, clamp(alpha.a, 0, 1));

	return color * tex2D(TextureSampler1, coords) * sampleColor;
}

technique Technique1
{
	pass AmazonEffect
	{
		PixelShader = compile ps_3_0 AmazonEffect();
	}
}