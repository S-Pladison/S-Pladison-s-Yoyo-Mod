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

float4 BowOfDivinePriestessRibbon(VertexShaderOutput input) : COLOR
{
    float2 uv = input.coord;
    float across = 1.0 - abs(uv.y * 2.0 - 1.0);

    float2 noiseUv = float2(uv.x * Repeats - Time * 0.45, uv.y);
    noiseUv.y += sin(uv.x * 12 + Time * 1.5) * 0.1;
    
    float noise = tex2D(TextureSampler0, noiseUv).r;
    noise *= noise;

    float mask = saturate(across * lerp(0.75, 1.15, noise));
    mask *= mask;

    float3 color = lerp(lerp(Color1.rgb, Color3.rgb, uv.x), lerp(Color0.rgb, Color2.rgb, uv.x), mask) * 2;
    return float4(color * mask, mask) * input.color;
}

technique Technique1
{
    pass BowOfDivinePriestessRibbon
    {
        VertexShader = compile vs_2_0 MainVertexShader();
        PixelShader = compile ps_2_0 BowOfDivinePriestessRibbon();
    }
}