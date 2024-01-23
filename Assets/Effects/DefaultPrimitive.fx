matrix TransformMatrix;

texture Texture0 : register(s0);
sampler textureSampler0 = sampler_state
{
    texture = <Texture0>;
};

float4 ColorTL;
float4 ColorTR;
float4 ColorBL;
float4 ColorBR;

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

float4 DefaultPrimitive(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(textureSampler0, input.coord);
    color *= lerp(lerp(ColorTL, ColorTR, input.coord.x), lerp(ColorBL, ColorBR, input.coord.x), input.coord.y);
    return color * input.color;
}

technique Technique1
{
    pass DefaultPrimitive
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 DefaultPrimitive();
    }
}