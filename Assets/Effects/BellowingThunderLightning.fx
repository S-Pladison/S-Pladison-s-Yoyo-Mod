texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
	texture = <Texture0>;
};

float2 ScreenSize;
float4 Color;

float4 BellowingThunderLightning(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0) : COLOR0
{
	float2 offset = coords / ScreenSize * 2;

	float4 redOffset = tex2D(TextureSampler0, coords + offset);
	float4 blueOffset = tex2D(TextureSampler0, coords - offset);

	float4 colorOffset = float4(redOffset.r, (redOffset.r + blueOffset.b) / 4.0f, blueOffset.b, 1.0f);
	colorOffset.a = (colorOffset.r + colorOffset.g + colorOffset.b) / 3.0f;

	float4 color = tex2D(TextureSampler0, coords);
	color.a = color.r;
	color.rgb *= lerp(Color.rgb, float3(1, 1, 1), color.r);

	return (color + colorOffset) * sampleColor;
}

technique Technique1
{
    pass BellowingThunderLightning
    {
        PixelShader = compile ps_3_0 BellowingThunderLightning();
    }
}