texture Texture0 : register(s0);
texture Texture1 : register(s1);

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
float4 Color0;
float4 Color1;
float Time;
float Repeats;
float SphereRatio;
float RingInner;
float RingOuter;

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

float3 RotateAroundAxis(float3 value, float3 axis, float angle)
{
    float s = sin(angle);
    float c = cos(angle);

    return value * c + cross(axis, value) * s + axis * dot(axis, value) * (1.0 - c);
}

struct Sphere
{
    float2 p;         // Позиция на холсте в кордах от [-1, -1] до [1, 1]
    float dist;       // Расстояние от центра: 0 в середине, 1 на окружности
    float z;          // Глубина полусферы: 1 в центре, 0 на краю
    float edgeFade;   // То, насколько плавным должен быть переход от 'ничего' в сферу
    float3 normal;    // Нормаль передней полусферы (p.x, p.y, z)
};

Sphere GetSphere(float2 p)
{
    Sphere sphere;
    sphere.p = p;

    float r2 = dot(p, p);
    sphere.dist = sqrt(r2);

    const float edgeSoftness = 0.03;
    sphere.edgeFade = saturate((1.0 - sphere.dist) / edgeSoftness);
    sphere.z = sqrt(saturate(1.0 - r2));
    sphere.normal = float3(p.x, p.y, sphere.z);

    return sphere;
}

float SampleTriplanar(float3 normal, float viewZ)
{
    const float scale = 0.25;
    const float minZ = 0.55;
    float stretch = 1.0 / max(viewZ, minZ);

    float3 blend = abs(normal);
    blend = blend * blend;
    blend = blend * blend;
    blend /= (blend.x + blend.y + blend.z);

    float4 sampleX = tex2Dlod(TextureSampler0, float4(normal.yz * scale * stretch, 0, 0));
    float4 sampleY = tex2Dlod(TextureSampler0, float4(normal.xz * scale * stretch, 0, 0));
    float4 sampleZ = tex2Dlod(TextureSampler0, float4(normal.xy * scale * stretch, 0, 0));

    return (sampleX * blend.x + sampleY * blend.y + sampleZ * blend.z).r;
}

float4 SampleColor(float height)
{
    float4 color = float4(height, height, height, 1);
    color *= color.r;
    color.rgb *= lerp(Color1.rgb, Color0.rgb, color.r) * 2.0;

    return color;
}

float4 SampleSphere(float2 p, float4 vertexColor)
{
    Sphere sphere = GetSphere(p);
    clip(1.0 - sphere.dist);
    
    // Вращаем сферу, накладываем на нее маску и красим
    const float3 rotationAxis = float3(0.70710678, 0.70710678, 0.0);
    float3 rotated = RotateAroundAxis(sphere.normal, rotationAxis, -Time * 5.5);
    float height = SampleTriplanar(rotated, sphere.z);
    float4 color = SampleColor(height);

    // К центру делаем сферу прозрачной
    const float coreStart = 0.5;
    const float corePower = 1.35;
    float core = pow(saturate((sphere.dist - coreStart) * 2.0), corePower);
    color *= sphere.edgeFade * core;

    return color * vertexColor;
}

float4 SampleRing(float2 p, float dist)
{
    float2 uv;
    uv.x = atan2(p.y, p.x) / 6.2831853 * Repeats - Time * 2.5;
    uv.y = saturate((dist - RingInner) / max(RingOuter - RingInner, 0.0001));

    float4 color = tex2Dlod(TextureSampler1, float4(uv, 0, 0));
    color *= color.r;
    color.rgb *= lerp(Color1.rgb, Color0.rgb, color.r) * 2.0;

    return color;
}

float4 CascadeSphere(float2 coord : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 p = coord * 2.0 - 1.0;
    float dist = length(p);

    float4 color = 0;

    if (SphereRatio > 0.001 && dist < SphereRatio)
        color = SampleSphere(p / SphereRatio, vertexColor);

    if (dist >= RingInner && dist <= RingOuter)
    {
        float4 ring = SampleRing(p, dist);
        color = ring + color * (1.0 - saturate(ring.a));
    }

    return color;
}

technique Technique1
{
    pass CascadeSphere
    {
        VertexShader = compile vs_3_0 MainVertexShader();
        PixelShader = compile ps_3_0 CascadeSphere();
    }
}
