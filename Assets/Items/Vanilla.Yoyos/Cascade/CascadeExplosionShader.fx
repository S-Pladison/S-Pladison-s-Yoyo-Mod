matrix TransformMatrix;

float UvRepeat;
float Time;

float4 Color0;
float4 Color1;

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

float4 CascadeExplosionRing(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(textureSampler0, (input.coord * float2(UvRepeat, 1) + float2(Time, 0)));
    color.a = 0;
    color.rgb *= lerp(Color1.rgb, Color0.rgb, color.r);
    return color * input.color;
}

technique Technique1
{
    pass CascadeExplosionRing
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 CascadeExplosionRing();
    }
}