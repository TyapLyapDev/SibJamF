Shader "SibJam/Enemy Hologram Spawn"
{
    Properties
    {
        [MainTexture] _BaseMap ("Surface Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Surface Color", Color) = (1,1,1,1)
        [HDR] _HologramColor ("Hologram Color", Color) = (0.05,2,3,1)
        _Progress ("Materialization", Range(0,1)) = 0
        _BoundsY ("World Bottom / Height", Vector) = (0,2,0,0)
        _ScanDensity ("Scan Lines Per Meter", Float) = 35
        _ScanSpeed ("Scan Speed", Float) = 2
        _GlitchStrength ("Glitch Displacement", Range(0,0.2)) = 0.035
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Name "Hologram"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _HologramColor;
                float4 _BoundsY;
                float _Progress, _ScanDensity, _ScanSpeed, _GlitchStrength;
                half _Smoothness, _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
            };
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float SolidAmount(float y)
            {
                float height = saturate((y - _BoundsY.x) / max(_BoundsY.y, 0.001));
                return 1 - smoothstep(_Progress * 1.2 - 0.2, _Progress * 1.2, height);
            }
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                float slice = floor((world.y - _BoundsY.x) * 24);
                float noise = Hash(float2(slice, floor(_Time.y * 18)));
                world.x += (noise - 0.5) * step(0.86, noise) * _GlitchStrength
                    * (1 - SolidAmount(world.y)) * (1 - _Progress);
                o.positionWS = world;
                o.positionCS = TransformWorldToHClip(world);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float solid = SolidAmount(i.positionWS.y);
                float localY = i.positionWS.y - _BoundsY.x;
                float scan = pow(saturate(sin((localY * _ScanDensity - _Time.y * _ScanSpeed) * TWO_PI) * 0.5 + 0.5), 8);
                float flicker = 0.85 + 0.15 * sin(_Time.y * 47 + localY * 23);
                half3 normal = NormalizeNormalPerPixel(i.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float rim = pow(1 - saturate(dot(normal, view)), 2.5);
                float coverage = lerp(saturate((0.18 + scan * 0.45 + rim * 0.35) * flicker), 1, solid);
                // Screen-door transparency keeps depth ordering stable on animated meshes.
                clip(coverage - InterleavedGradientNoise(i.positionCS.xy, 0) - 0.001);

                InputData lighting = (InputData)0;
                lighting.positionWS = i.positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = view;
                lighting.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                lighting.bakedGI = SampleSH(normal);
                lighting.vertexLighting = VertexLighting(i.positionWS, normal);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                half3 body = UniversalFragmentPBR(lighting, surface).rgb;
                half3 hologram = _HologramColor.rgb * (0.2 + rim * 1.8 + scan * 0.8) * flicker;
                float edge = 4 * solid * (1 - solid);
                half3 color = lerp(hologram, body, solid) + _HologramColor.rgb * edge * 1.6;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
