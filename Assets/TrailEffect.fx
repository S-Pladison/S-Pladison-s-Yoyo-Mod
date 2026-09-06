texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
    MagFilter = Linear;
    MinFilter = Linear;
    Mipfilter = Linear;
};

matrix TransformMatrix;

float4 Color0;
float4 Color1;
float4 Color2;
float4 Color3;
float ColorMode;
float Repeats;
float Time;
float Opacity;
float Intensity;
float FadePower;

struct VertexShaderInput
{
    float2 coord : TEXCOORD0;
    float4 color : COLOR0;
    float4 position : POSITION0;
};

struct VertexShaderOutput
{
    float2 coord : TEXCOORD0;
    float4 color : COLOR0;
    float4 position : SV_POSITION;
};

VertexShaderOutput MainVertexShader(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.coord = input.coord;
    output.color = input.color;
    output.position = mul(input.position, TransformMatrix);
    return output;
}

float4 SimpleTrail(VertexShaderOutput input) : COLOR
{
    return tex2D(TextureSampler0, input.coord) * input.color;
}

float4 FlameTrail(VertexShaderOutput input) : COLOR
{
    float4 sampled = tex2D(TextureSampler0, input.coord * float2(Repeats, 1) + float2(-Time * 2.5, 0));

    float4 mulColor = sampled * sampled.r;

    float4 sqrtColor = sampled;
    sqrtColor.a = sampled.r;
    sqrtColor.rgb *= pow(sampled.r, 0.5);

    float colorMode = step(1.0, ColorMode);
    float4 color = lerp(mulColor, sqrtColor, colorMode);
    color.rgb *= lerp(lerp(Color1.rgb, Color3.rgb, input.coord.x), lerp(Color0.rgb, Color2.rgb, input.coord.x), color.r) * Intensity;
    color *= 1 - pow(abs(input.coord.x), FadePower);

    return color * input.color * Opacity;
}

technique Technique1
{
    pass Simple
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 SimpleTrail();
    }

    pass Flame
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 FlameTrail();
    }
}
