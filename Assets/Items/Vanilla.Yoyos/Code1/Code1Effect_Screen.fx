sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float2 uImageSize1;
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

// xy - центр; z — радиус кольца в мировых коорд; w — сила (0..1)
float4 Wave0;
float4 Wave1;
float4 Wave2;

const float RingThicknessInPixels = 72.0;
const float RingSoftness = 1.1;
const float InteriorStrength = 0.28;
const float InteriorFalloff = 1.5;
const float GlitchSliceCount = 48.0;
const float GlitchScrollSpeed = 9.0;
const float GlitchOffset = 0.035;
const float AberrationBase = 1.2;
const float AberrationFront = 2.5;
const float AberrationPixels = 4.0;
const float PosterizeHigh = 18.0;
const float PosterizeLow = 10.0;
const float ScanlineStrength = 0.16;
const float EffectBlend = 0.85;

float Random(float2 value)
{
    return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
}

float RingStrength(float4 wave, float2 coords, float2 aspect, float thickness)
{
    // Центр и радиус из мира в координаты экрана; aspect, чтобы круг не стал овалом
    float2 center = (wave.xy - uScreenPosition) / uScreenResolution;
    float radius = wave.z / uScreenResolution.y;
    float dist = length((coords - center) * aspect);

    // Там где кольцо - 1 или ниже (ну, в зависимости от силы искажения); если силы нет, то и искажения нет, т.е. в остальных местах вне кольца
    float ring = pow(saturate(1.0 - abs(dist - radius) / thickness), RingSoftness) * wave.w;

    // Хоть мы тут про кольцо говорим, всеж искажаем немного и центр, для красоты
    float inner = saturate((radius - dist) / max(radius, 0.0001));
    inner = pow(inner, InteriorFalloff) * InteriorStrength * wave.w;

    // Сила искажения кольца + немнога центра
    return saturate(ring + inner);
}

float4 Code1DigitalWave(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);
    float2 aspect = float2(uScreenResolution.x / uScreenResolution.y, 1.0);
    float thickness = RingThicknessInPixels / uScreenResolution.y;

    // Три волны: насколько этот пиксель попал на кольцо или в центр
    float ring0 = RingStrength(Wave0, coords, aspect, thickness);
    float ring1 = RingStrength(Wave1, coords, aspect, thickness);
    float ring2 = RingStrength(Wave2, coords, aspect, thickness);
    float mask = saturate((ring0 + ring1 + ring2) * uIntensity * uOpacity);

    // Значит, искажения не нужно
    if (mask <= 0.001)
        return original;

    // Разрезаем экран на полоски и смещаем влево-вправо
    float slice = floor(coords.y * GlitchSliceCount + uTime * GlitchScrollSpeed);
    float noise = Random(float2(slice, uTime));
    float2 uv = coords;
    uv.x += (noise - 0.5) * mask * GlitchOffset;

    // Хроматическая аберрация
    float2 pixel = 1.0 / uScreenResolution;
    float front = saturate(max(ring0, max(ring1, ring2)));
    float aberr = (AberrationBase + front * AberrationFront) * mask * AberrationPixels * pixel.x;

    float4 color = original;
    color.r = tex2D(uImage0, saturate(uv + float2(aberr, 0))).r;
    color.g = tex2D(uImage0, saturate(uv)).g;
    color.b = tex2D(uImage0, saturate(uv - float2(aberr, 0))).b;

    // Уменьшаем кол-во цветов в палитре
    float levels = lerp(PosterizeHigh, PosterizeLow, mask);
    color.rgb = floor(color.rgb * levels + 0.5) / levels;

    // Полосы как на телеке
    float scan = 1.0 - mask * ScanlineStrength * step(0.5, frac(coords.y * uScreenResolution.y * 0.5));
    color.rgb *= scan;

    return lerp(original, color, saturate(mask * EffectBlend));
}

technique Technique1
{
    pass Code1DigitalWave
    {
        PixelShader = compile ps_3_0 Code1DigitalWave();
    }
}
