texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
    MagFilter = Point;
    MinFilter = Point;
    Mipfilter = Point;
};

matrix TransformMatrix;

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

float4 RallyTrail(VertexShaderOutput input) : COLOR
{
    return tex2D(TextureSampler0, input.coord) * input.color;
}

technique Technique1
{
    pass RallyTrail
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 RallyTrail();
    }
}
