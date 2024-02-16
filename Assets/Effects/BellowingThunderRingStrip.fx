matrix TransformMatrix;

float UvRepeat;
float Time;
bool Fade;

texture Texture0 : register(s0);
sampler textureSampler0 = sampler_state
{
    texture = <Texture0>;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
    MagFilter = Linear;
    MinFilter = Linear;
    Mipfilter = Linear;
};

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

float4 BellowingThunderRingStrip(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(textureSampler0, (input.coord * float2(UvRepeat, 1) + float2(Time, 0)));
    color.a = color.r;

    if (!Fade) return color * input.color;

    color.a *= (1 - pow(1 - input.coord.x, 5));

    return color * input.color;
}

technique Technique1
{
    pass BellowingThunderRingStrip
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 BellowingThunderRingStrip();
    }
}