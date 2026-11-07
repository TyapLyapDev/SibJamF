// Экран старого телевизора: выпуклость кинескопа, сканлайны, дёрганье строк, цветной сдвиг, шум и виньетка.
// _Glitch (0..1) и _Talk (0..1) задаёт TVScreen: помеха при смене лица и покачивание лица, пока телик говорит
Shader "SibJam/TVScreen"
{
    Properties
    {
        _BaseMap ("Лицо", 2D) = "black" {}
        [HDR] _BaseColor ("Цвет и яркость", Color) = (1.6, 1.6, 1.6, 1)

        [Header(Kinescope)]
        _ScreenRect ("Область экрана в UV модели (x, y, ширина, высота)", Vector) = (0, 0, 1, 1)
        _Curvature ("Выпуклость", Range(0, 0.5)) = 0.12
        _Vignette ("Затемнение углов", Range(0, 2)) = 0.8

        [Header(Scanlines)]
        _ScanlineCount ("Число строк", Float) = 140
        _ScanlineStrength ("Сила строк", Range(0, 1)) = 0.3
        _ScanSpeed ("Скорость строк", Float) = 3

        [Header(Jitter)]
        _JitterChance ("Как часто дёргаются строки", Range(0, 0.3)) = 0.04
        _JitterStrength ("Сила дёрганья", Range(0, 0.1)) = 0.02
        _JitterRate ("Смен дёрганья в секунду", Float) = 9
        _ChromaShift ("Цветной сдвиг", Range(0, 0.02)) = 0.003
        _Noise ("Шум", Range(0, 0.5)) = 0.06

        [Header(Driven by TVScreen)]
        _Glitch ("Помеха", Range(0, 1)) = 0
        _Talk ("Говорит", Range(0, 1)) = 0
        _TalkBounce ("Покачивание при речи", Range(0, 0.1)) = 0.015
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ScreenRect;
                float _Curvature;
                float _Vignette;
                float _ScanlineCount;
                float _ScanlineStrength;
                float _ScanSpeed;
                float _JitterChance;
                float _JitterStrength;
                float _JitterRate;
                float _ChromaShift;
                float _Noise;
                float _Glitch;
                float _Talk;
                float _TalkBounce;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            float Hash(float n) { return frac(sin(n) * 43758.5453); }
            float Hash2(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                // UV экрана в модели занимают не весь квадрат: растягиваем их на 0..1, чтобы лицо заполняло экран
                float2 screenUV = (input.uv - _ScreenRect.xy) / max(_ScreenRect.zw, 0.0001);

                // Выпуклость кинескопа: картинка чуть вздувается к центру (края не обрезаются — текстура в Clamp)
                float2 centered = screenUV - 0.5;
                float2 uv = 0.5 + centered / (1.0 + _Curvature * dot(centered, centered) * 4.0);

                // Дёрганье отдельных строк и сильная помеха при смене лица
                float row = floor(uv.y * _ScanlineCount * 0.25);
                float tick = floor(time * _JitterRate);
                float jitter = step(1.0 - _JitterChance, Hash(row + tick * 17.0)) * (Hash(row * 3.1 + tick) - 0.5) * _JitterStrength;
                jitter += (Hash(row * 7.3 + time * 40.0) - 0.5) * _Glitch * 0.12;
                uv.x += jitter;

                // Пока говорит — лицо слегка подпрыгивает
                uv.y += sin(time * 17.0) * _Talk * _TalkBounce;

                float2 faceUV = TRANSFORM_TEX(uv, _BaseMap);
                float shift = _ChromaShift * (1.0 + _Glitch * 8.0);
                half3 color;
                color.r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, faceUV + float2(shift, 0)).r;
                color.g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, faceUV).g;
                color.b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, faceUV - float2(shift, 0)).b;

                float scan = 0.5 + 0.5 * sin((uv.y * _ScanlineCount + time * _ScanSpeed) * 6.28318);
                color *= 1.0 - _ScanlineStrength * scan;

                float noise = Hash2(floor(screenUV * 420.0) + frac(time * 13.0) * 100.0);
                color += (noise - 0.5) * (_Noise + _Glitch * 0.6);

                color *= saturate(1.0 - dot(centered, centered) * _Vignette * 2.0);
                color = max(0.0, color) * _BaseColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
