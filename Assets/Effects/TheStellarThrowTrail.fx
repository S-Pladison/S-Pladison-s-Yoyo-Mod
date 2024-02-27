matrix TransformMatrix;

float UvRepeat;
float Time;

float4 Color0;
float4 Color1;
float4 Color2;

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

texture Texture1 : register(s1);
sampler textureSampler1 = sampler_state
{
    texture = <Texture1>;
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

float4 TheStellarThrowTrail(VertexShaderOutput input) : COLOR
{
    float4 color1 = tex2D(textureSampler0, (input.coord * float2(UvRepeat, 1) + float2(Time, 0)));
    color1.a = color1.r;
    color1.rgb *= Color0.rgb;
    
    float4 color2 = tex2D(textureSampler1, (input.coord * float2(UvRepeat, 1) + float2(Time * 1.5, 0)));
    color2 *= color2.r * (1 - input.coord.x);
    color2 *= lerp(lerp(Color2, Color1, 1 - abs(input.coord.y - 0.5) * 2) * 1.2, Color2, input.coord.x);
    
    return (color1 + color2) * input.color * (1 - input.coord.x);
}

technique Technique1
{
    pass TheStellarThrowTrail
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 TheStellarThrowTrail();
    }
}