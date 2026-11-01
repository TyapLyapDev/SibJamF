// Экраны телевизоров как одна видеостена: картинка проецируется по мировым координатам на плоскость стены,
// каждый экран показывает свой кусок. Плоскость задаётся прямоугольником в мировых Z (ширина) и Y (высота).
Shader "SibJam/TVProjected"
{
    Properties
    {
        [MainTexture] _BaseMap ("Image", 2D) = "white" {}
        [HDR] _EmissionColor ("Tint", Color) = (1.2, 1.2, 1.2, 1)
        _Rect ("Projection Rect (Z min, Z max, Y min, Y max)", Vector) = (39, 54, 2, 12)
        [Toggle] _FlipU ("Flip Horizontally", Float) = 1
        _Scanlines ("Scanlines", Range(0, 1)) = 0.25
        _ScanDensity ("Scanline Density", Float) = 220
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _EmissionColor;
            float4 _Rect;
            float _FlipU;
            half _Scanlines;
            float _ScanDensity;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float u = (input.positionWS.z - _Rect.x) / max(_Rect.y - _Rect.x, 0.001);
                float v = (input.positionWS.y - _Rect.z) / max(_Rect.w - _Rect.z, 0.001);
                u = lerp(u, 1.0 - u, _FlipU);

                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, float2(u, v) * _BaseMap_ST.xy + _BaseMap_ST.zw).rgb;
                half scan = 1.0 - _Scanlines * (0.5 + 0.5 * sin(input.positionWS.y * _ScanDensity));
                color *= _EmissionColor.rgb * scan;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
