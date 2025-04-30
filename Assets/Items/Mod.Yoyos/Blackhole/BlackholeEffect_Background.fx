texture Texture0 : register(s0);

sampler TextureSampler0 = sampler_state
{
    texture = <Texture0>;
    MagFilter = Point;
    MinFilter = Point;
    Mipfilter = Point;
};

matrix TransformMatrix;
float2 Texture0Offset;
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
    return (float4(0.0, 0.0, 1.0, 1.0) + Blur(TextureSampler0, input.coord + Texture0Offset, BlurRadius)) * Transparency;
}

technique Technique1
{
    pass BlackholeBackground
    {
        VertexShader = compile vs_3_0 MainVertexShader();
        PixelShader = compile ps_3_0 BlackholeBackground();
    }
}