#define TWO_PI 6.28318530718

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
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.coord = input.coord;
    output.color = input.color;
    output.position = mul(input.position, TransformMatrix);
    return output;
}

float2 ToPolar(float2 cartesian)
{
    float distance = length(cartesian);
    float angle = atan2(cartesian.y, cartesian.x);
    return float2(angle / TWO_PI, distance);
}

float4 ArteryBlackAura(VertexShaderOutput input) : COLOR
{
    float2 uv = input.coord;
    uv -= 0.5;
    
    float4 color = tex2D(textureSampler0, ToPolar(uv) + float2(0, Time) + tex2D(textureSampler0, input.coord + float2(0, Time * 0.25)).xy * 0.1);
    
    color *= smoothstep(1, 0, color.r + color.g + color.b);
    color *= smoothstep(1, 0, distance(float2(0.5, 0.5), input.coord) * 2);
    color.a *= 5;

    return color * input.color;
}

technique Technique1
{
    pass ArteryBlackAura
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 ArteryBlackAura();
    }
}