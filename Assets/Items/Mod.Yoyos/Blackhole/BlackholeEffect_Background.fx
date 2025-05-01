texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
    MagFilter = Point;
    MinFilter = Point;
    Mipfilter = Point;
};

texture Texture1 : register(s1);

sampler TextureSampler1 = sampler_state
{
    texture = <Texture1>;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
    MagFilter = Linear;
    MinFilter = Linear;
    Mipfilter = Linear;
};

matrix TransformMatrix;
float2 Texture1Offset;
float2 BlurRadius;
float Transparency;

struct VertexShaderInput
{
    float2 coord : TEXCOORD0;
    float4 position : POSITION0;
};

struct VertexShaderOutput
{
    float2 coord : TEXCOORD0;
    float4 position : SV_POSITION;
};

VertexShaderOutput MainVertexShader(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.coord = input.coord;
    output.position = mul(input.position, TransformMatrix);
    return output;
}

float4 Blur(sampler sourceSampler, float2 uv, float2 radius)
{
    static const int kernelSize = 5;
    static const float weights[kernelSize][kernelSize] =
    {
        { 0.015, 0.035, 0.050, 0.035, 0.015 },
        { 0.035, 0.150, 0.240, 0.150, 0.035 },
        { 0.050, 0.240, 0.400, 0.240, 0.050 },
        { 0.035, 0.150, 0.240, 0.150, 0.035 },
        { 0.015, 0.035, 0.050, 0.035, 0.015 }
    };

    float4 color = 0;
    const int halfSize = kernelSize / 2;
    
    [unroll]
    for (int y = -halfSize; y <= halfSize; y++)
    {
        [unroll]
        for (int x = -halfSize; x <= halfSize; x++)
        {
            float2 offset = float2(x, y) * radius;
            color += tex2D(sourceSampler, uv + offset) * weights[y + halfSize][x + halfSize];
        }
    }
    
    return color;
}


float4 BlackholeBackground(VertexShaderOutput input) : COLOR
{
    //float4 backgroundColor = float4(55, 40, 95, 255) / 255;
    //float4 backgroundColor = float4(5, 5, 10, 255) / 255;
    float4 backgroundColor = float4(0, 0, 0, 255) / 255;
    float4 maskColor = 1.0f - Blur(TextureSampler0, input.coord, BlurRadius);
    float4 noiseColor = tex2D(TextureSampler1, input.coord + Texture1Offset);
    
    noiseColor.rgb *= lerp(float3(15, 15, 45) / 255, float3(55, 55, 115) / 255, noiseColor.r);
    
    backgroundColor += noiseColor;
    backgroundColor *= maskColor.a;
    
    return backgroundColor * Transparency;
}

technique Technique1
{
    pass BlackholeBackground
    {
        VertexShader = compile vs_3_0 MainVertexShader();
        PixelShader = compile ps_3_0 BlackholeBackground();
    }
}