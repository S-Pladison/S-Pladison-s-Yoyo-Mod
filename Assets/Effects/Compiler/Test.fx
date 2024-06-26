matrix TransformMatrix;

float Time;

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
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.coord = input.coord;
    output.color = input.color;
    output.position = mul(input.position, TransformMatrix);
    return output;
}

float4 Test(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(textureSampler0, input.coord + float2(Time, 0));
    color.rgb *= 2;
    color.a = color.r;
    return color * input.color;
}

technique Technique1
{
    pass Test
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 Test();
    }
}