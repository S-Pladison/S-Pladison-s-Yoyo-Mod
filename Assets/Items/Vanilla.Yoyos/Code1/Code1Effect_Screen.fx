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

float4 Wave0;
float4 Wave1;
float4 Wave2;
float3 WaveFill;

struct Wave
{
    float2 center;
    float radius;
    float strength;
    float fill;
};

float Random(float2 value)
{
    return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
}

Wave UnpackWave(float4 packed, float fill)
{
    Wave wave;
    wave.center = packed.xy;
    wave.radius = packed.z;
    wave.strength = packed.w;
    wave.fill = fill;
    return wave;
}

float WaveMask(Wave wave, float2 coords, float2 aspect, float thickness)
{
    // Центр и радиус из мира в координаты экрана; Ну и aspect, чтобы сделать из овала круг
    float2 center = (wave.center - uScreenPosition) / uScreenResolution;
    float radius = wave.radius / uScreenResolution.y;
    float dist = length((coords - center) * aspect);

    // Колько с плавными краями
    const float ringSoftness = 1.1;
    float ring = pow(saturate(1.0 - abs(dist - radius) / thickness), ringSoftness) * wave.strength;

    // Залитый круг с мягким краем
    float circle = saturate((radius - dist) / max(thickness, 0.0001)) * wave.strength;

    return saturate(lerp(ring, circle, wave.fill));
}

void SampleWaves(float2 coords, float2 aspect, float thickness, out float mask, out float peak)
{
    float mask0 = WaveMask(UnpackWave(Wave0, WaveFill.x), coords, aspect, thickness);
    float mask1 = WaveMask(UnpackWave(Wave1, WaveFill.y), coords, aspect, thickness);
    float mask2 = WaveMask(UnpackWave(Wave2, WaveFill.z), coords, aspect, thickness);

    mask = saturate((mask0 + mask1 + mask2) * uIntensity * uOpacity);
    peak = saturate(max(mask0, max(mask1, mask2)));
}

float4 Code1DigitalWave(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);
    float2 aspect = float2(uScreenResolution.x / uScreenResolution.y, 1.0);

    const float ringThicknessInPixels = 72.0;
    float thickness = ringThicknessInPixels / uScreenResolution.y;

    float mask;
    float peak;
    SampleWaves(coords, aspect, thickness, mask, peak);

    // Значит, искажения не нужно
    if (mask <= 0.001)
        return original;

    // Разрезаем экран на полоски и смещаем влево-вправо
    const float glitchSliceCount = 48.0;
    const float glitchScrollSpeed = 9.0;
    const float glitchOffset = 0.035;
    float slice = floor(coords.y * glitchSliceCount + uTime * glitchScrollSpeed);
    float noise = Random(float2(slice, uTime));
    float2 uv = coords;
    uv.x += (noise - 0.5) * mask * glitchOffset;

    // Хроматическая аберрация
    const float aberrationBase = 1.2;
    const float aberrationPeak = 2.5;
    const float aberrationPixels = 4.0;
    float2 pixel = 1.0 / uScreenResolution;
    float aberr = (aberrationBase + peak * aberrationPeak) * mask * aberrationPixels * pixel.x;

    float4 color = original;
    color.r = tex2D(uImage0, saturate(uv + float2(aberr, 0))).r;
    color.g = tex2D(uImage0, saturate(uv)).g;
    color.b = tex2D(uImage0, saturate(uv - float2(aberr, 0))).b;

    // Уменьшаем кол-во цветов в палитре
    const float posterizeHigh = 18.0;
    const float posterizeLow = 10.0;
    float levels = lerp(posterizeHigh, posterizeLow, mask);
    color.rgb = floor(color.rgb * levels + 0.5) / levels;

    // Полосы как на телеке/скане
    const float scanlineStrength = 0.16;
    float scan = 1.0 - mask * scanlineStrength * step(0.5, frac(coords.y * uScreenResolution.y * 0.5));
    color.rgb *= scan;

    const float effectBlend = 0.85;
    return lerp(original, color, saturate(mask * effectBlend));
}

technique Technique1
{
    pass Code1DigitalWave
    {
        PixelShader = compile ps_3_0 Code1DigitalWave();
    }
}
