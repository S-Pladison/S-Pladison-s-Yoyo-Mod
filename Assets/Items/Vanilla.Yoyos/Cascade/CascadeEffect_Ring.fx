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
float Repeats;
float Time;

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

float4 CascadeRing(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(TextureSampler0, input.coord * float2(Repeats, 1) + float2(-Time * 2.5, 0));
    
    color *= color.r;
    color.rgb *= lerp(Color1.rgb, Color0.rgb, color.r) * 2;
    
    return color * input.color;
}

technique Technique1
{
    pass CascadeRing
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 CascadeRing();
    }
}