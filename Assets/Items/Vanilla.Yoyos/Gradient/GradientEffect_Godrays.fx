// Source: https://godotshaders.com/shader/god-rays/

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
float2 Position;
float Time;
float Opacity;

const float speed = 2.0;
const float ray1Density = 10.0;
const float ray2Density = 32.0;
const float ray2Intensity = 0.5;
const float cutoff = 0.1;
const float falloff = 0.35;
const float edgeFade = 0.2;
const float seed = 1508;

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

float Random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
}

float Noise(in float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);

    float a = Random(i);
    float b = Random(i + float2(1.0, 0.0));
    float c = Random(i + float2(0.0, 1.0));
    float d = Random(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float4 GradientGodrays(VertexShaderOutput input) : COLOR
{
    float2 uv = float2(input.coord.y, input.coord.x);
    
    float2 ray1 = float2(uv.x * ray1Density + Position.x * 0.025 + sin(Time * 0.2 * speed) * (ray1Density * 0.2) + seed, 1.0);
    float2 ray2 = float2(uv.x * ray2Density + Position.x * 0.05 + sin(Time * 0.4 * speed) * (ray1Density * 0.2) + seed, 1.0);
    
    float rays = clamp(Noise(ray1) + (Noise(ray2) * ray2Intensity), 0.0, 1.0);
    rays *= smoothstep(0.0, falloff, (1.0 - uv.y));
    rays *= smoothstep(0.0 + cutoff, edgeFade + cutoff, uv.x);
    rays *= smoothstep(0.0 + cutoff, edgeFade + cutoff, 1.0 - uv.x);
    rays *= 1.0 - pow(1.0 - input.coord.x, 3.0);
    
    float4 color = input.color * clamp((lerp(float4(255, 190, 0, 255), float4(255, 250, 185, 255), rays) / 255), 0.0, 1.0) * 1.1;
    return color * rays * Opacity;
}

technique Technique1
{
    pass GradientGodrays
    {
        VertexShader = compile vs_3_0 MainVertexShader();
        PixelShader = compile ps_3_0 GradientGodrays();
    }
}