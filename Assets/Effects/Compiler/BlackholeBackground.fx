texture Texture0 : register(s0);
texture Texture1 : register(s1);
texture Texture2 : register(s2);

float2 Texture0Size;
float2 Texture1Size;
float2 Texture2Size;

float4x4 EffectMatrix;
float2 ScreenPosition;
float Time;

float4 Cloud1Color;
float4 Cloud2Color;

const float Scale1 = 4;
const float Scale2 = 5;

const float PosMult1 = 0.2;
const float PosMult2 = 0.125;

const float TimeOffsetMult1 = 1;
const float TimeOffsetMult2 = 0.75;

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

sampler TextureSampler2 = sampler_state
{
	texture = <Texture2>;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MagFilter = Linear;
	MinFilter = Linear;
	Mipfilter = Linear;
};

float4 BlackholeBackground(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
	float2 uv = mul(EffectMatrix, coords - 0.5);
	float4 color = float4(0, 0, 0, 0);

	// ...

	float2 vec = 1 / Texture0Size;
	float2 vec1 = (Texture0Size / Texture1Size);
	float2 vec2 = (Texture0Size / Texture2Size);

	float2 timeOffset1 = float2(0, -Time * TimeOffsetMult1);
	float2 timeOffset2 = float2(0, -Time * TimeOffsetMult2);

	float2 effectUv1 = ScreenPosition * PosMult1 + timeOffset1;
	effectUv1 = uv + effectUv1 * vec;
	float2 effectUv2 = ScreenPosition * PosMult2 + timeOffset2;
	effectUv2 = uv + effectUv2 * vec;

	float2 spaceUv = effectUv1 * vec1 * Scale1 + 0.5;
	float4 spaceColor = tex2D(TextureSampler1, spaceUv);
	color.rgb += spaceColor.rgb;

	spaceUv = effectUv2 * vec1 * Scale2 + 0.5;
	spaceColor = tex2D(TextureSampler1, spaceUv);
	color.rgb += spaceColor.rgb;

	float2 cloudsUv = effectUv1 * vec2 * Scale1 + 0.5;
	float4 cloudsColor = tex2D(TextureSampler2, cloudsUv) * Cloud1Color;
	color.rgb += cloudsColor.rgb;

	cloudsUv = effectUv2 * vec2 * Scale2 + 0.5;
	cloudsColor = tex2D(TextureSampler2, cloudsUv) * Cloud2Color;
	color.rgb += cloudsColor.rgb;

	// ...

	float4 result = color;
	float4 maskColor = tex2D(TextureSampler0, coords);
	result.a = maskColor.rgb;
	result *= sampleColor;

	return result;
}

technique Technique1
{
	pass BlackholeBackground
	{
		PixelShader = compile ps_3_0 BlackholeBackground();
	}
}