Shader "SibJam/Enemy Hologram Spawn"
{
    Properties
    {
        [MainTexture] _BaseMap ("Surface Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Surface Color", Color) = (1,1,1,1)
        [HDR] _HologramColor ("Construction Grid Color", Color) = (3,0.05,0.05,1)
        _Progress ("Materialization", Range(0,1)) = 0
        _BoundsY ("World Bottom / Height", Vector) = (0,2,0,0)
        _SpawnOrigin ("Spawn Origin", Vector) = (0,0,0,0)
        _CoarseGridScale ("Large Grid Cells Per Height", Range(2,16)) = 6
        _FineGridScale ("Small Grid Cells Per Height", Range(8,64)) = 24
        _FragmentScale ("Model Fragments Per Height", Range(4,32)) = 14
        _GridIntensity ("Grid Glow", Range(0,5)) = 1.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _HologramColor;
                float4 _BoundsY, _SpawnOrigin;
                float _Progress, _CoarseGridScale, _FineGridScale, _FragmentScale, _GridIntensity;
                half _Smoothness, _Metallic;
            CBUFFER_END

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float Grid2D(float2 position, float width)
            {
                float2 distanceToLine = abs(frac(position + 0.5) - 0.5);
                float2 antialias = max(fwidth(position), float2(0.001, 0.001));
                float2 lines = 1 - smoothstep(width - antialias, width + antialias, distanceToLine);
                return max(lines.x, lines.y);
            }
            float Grid(float3 position, half3 normal, float scale, float width)
            {
                // Project the grid onto all sides, including surfaces parallel to an axis.
                float3 weights = pow(abs(normal), 6);
                weights /= max(weights.x + weights.y + weights.z, 0.001);
                float3 p = position * scale;
                return Grid2D(p.yz, width) * weights.x
                    + Grid2D(p.xz, width) * weights.y
                    + Grid2D(p.xy, width) * weights.z;
            }
            struct SpawnMask
            {
                float CoarseGrid, FineGrid, FragmentEdges, Model;
            };
            // Three staggered fronts travel upward: large grid, fine grid, then model pieces.
            SpawnMask EvaluateSpawn(float3 positionWS, half3 normal)
            {
                float height = max(_BoundsY.y, 0.001);
                float3 localPosition = (positionWS - _SpawnOrigin.xyz) / height;
                float normalizedY = saturate((positionWS.y - _BoundsY.x) / height);
                float front = lerp(-0.05, 1.85, saturate(_Progress));
                float age = front - normalizedY;
                float coarseBand = smoothstep(0, 0.04, age) * (1 - smoothstep(0.22, 0.30, age));
                float fineBand = smoothstep(0.18, 0.24, age) * (1 - smoothstep(0.58, 0.72, age));
                float fragmentProgress = saturate((age - 0.38) / 0.38);
                float fragmentSeed = Hash(floor(localPosition * _FragmentScale));
                SpawnMask mask;
                mask.CoarseGrid = Grid(localPosition, normal, _CoarseGridScale, 0.025) * coarseBand;
                mask.FineGrid = Grid(localPosition, normal, _FineGridScale, 0.018) * fineBand;
                mask.Model = step(0.0001, fragmentProgress) * step(fragmentSeed, fragmentProgress);
                mask.FragmentEdges = Grid(localPosition, normal, _FragmentScale, 0.02)
                    * mask.Model * (1 - smoothstep(0.58, 0.78, age));
                return mask;
            }
            void ClipSpawn(SpawnMask mask)
            {
                clip(max(mask.Model, mask.CoarseGrid + mask.FineGrid) - 0.02);
            }
        ENDHLSL
        Pass
        {
            Name "Construction"
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
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                #ifdef DYNAMICLIGHTMAP_ON
                    float2 dynamicLightmapUV : TEXCOORD5;
                #endif
                half3 vertexLighting : TEXCOORD6;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = world;
                o.positionCS = TransformWorldToHClip(world);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS, o.vertexSH);
                #ifdef DYNAMICLIGHTMAP_ON
                    o.dynamicLightmapUV = v.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                o.vertexLighting = VertexLighting(world, o.normalWS);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 normal = NormalizeNormalPerPixel(i.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float rim = pow(1 - saturate(dot(normal, view)), 2.5);
                SpawnMask mask = EvaluateSpawn(i.positionWS, normal);
                ClipSpawn(mask);

                InputData lighting = (InputData)0;
                lighting.positionWS = i.positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = view;
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    lighting.shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.positionWS));
                #else
                    lighting.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #endif
                #ifdef DYNAMICLIGHTMAP_ON
                    lighting.bakedGI = SAMPLE_GI(i.lightmapUV, i.dynamicLightmapUV, i.vertexSH, normal);
                #else
                    lighting.bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, normal);
                #endif
                lighting.vertexLighting = i.vertexLighting;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                lighting.shadowMask = SAMPLE_SHADOWMASK(i.lightmapUV);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                half3 body = UniversalFragmentPBR(lighting, surface).rgb;
                half3 construction = _HologramColor.rgb
                    * (mask.CoarseGrid * 1.2 + mask.FineGrid * 0.7 + mask.FragmentEdges * 0.45)
                    * (0.8 + rim * 0.4) * _GridIntensity;
                half3 color = body * mask.Model + construction;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
        // Same mask as the visible surface, so shadows and SSAO grow with the model
        // and nothing pops when the original materials come back.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };

            ShadowVaryings ShadowVert(ShadowAttributes v)
            {
                ShadowVaryings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirection = normalize(_LightPosition - o.positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS, o.normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                // The thin glowing grid casts no shadow, only assembled pieces do.
                clip(EvaluateSpawn(i.positionWS, NormalizeNormalPerPixel(i.normalWS)).Model - 0.5);
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct DepthVaryings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };

            DepthVaryings DepthVert(DepthAttributes v)
            {
                DepthVaryings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 DepthFrag(DepthVaryings i) : SV_Target
            {
                ClipSpawn(EvaluateSpawn(i.positionWS, NormalizeNormalPerPixel(i.normalWS)));
                return i.positionCS.z;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct DepthNormalsAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct DepthNormalsVaryings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes v)
            {
                DepthNormalsVaryings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 DepthNormalsFrag(DepthNormalsVaryings i) : SV_Target
            {
                half3 normal = NormalizeNormalPerPixel(i.normalWS);
                ClipSpawn(EvaluateSpawn(i.positionWS, normal));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octahedral = PackNormalOctQuadEncode(normal);
                    return half4(PackFloat2To888(saturate(octahedral * 0.5 + 0.5)), 0);
                #else
                    return half4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
