Shader "Custom/FlowerSway"
{
    Properties
    {
        [MainTexture] _BaseMap  ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        [MainColor]   _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff      ("Alpha Cutoff", Range(0,1)) = 0.303
        _BumpMap     ("Normal Map", 2D) = "bump" {}
        _BumpScale   ("Normal Scale", Float) = 1.0
        _Metallic    ("Metallic", Range(0,1)) = 0.0
        _Smoothness  ("Smoothness", Range(0,1)) = 0.145

        [Header(Sway Animation)]
        [Space]
        _SwaySpeed   ("Sway Speed",    Range(0.1, 5.0)) = 1.8
        _SwayStrength("Sway Strength", Range(0.0, 0.4)) = 0.12
        _BobStrength ("Bob Strength",  Range(0.0, 0.15)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }
        LOD 300
        Cull Off

        // ── Forward Lit ──────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile        _ LIGHTMAP_ON
            #pragma multi_compile        _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                half   _BumpScale;
                half   _Metallic;
                half   _Smoothness;
                half   _SwaySpeed;
                half   _SwayStrength;
                half   _BobStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS  : POSITION;
                float3 normalOS    : NORMAL;
                float4 tangentOS   : TANGENT;
                float2 texcoord    : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
                float3 positionWS  : TEXCOORD2;
                float3 normalWS    : TEXCOORD3;
                float4 tangentWS   : TEXCOORD4;  // xyz=tangent  w=sign
                half4  fogAndLight : TEXCOORD5;   // x=fog  yzw=vertexLight
                float4 shadowCoord : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Unique per-flower phase from its world-space grid cell.
            float FlowerPhase(float3 posWS)
            {
                return dot(floor(posWS.xz * 0.5), float2(1.618, 2.399));
            }

            // Wizard-of-Oz dance sway: displaces posOS in place.
            void ApplySway(inout float3 posOS)
            {
                float3 wsBase  = TransformObjectToWorld(float3(0, 0, 0));
                float  phase   = FlowerPhase(wsBase);
                float  t       = _Time.y * _SwaySpeed;
                // Height-weighted so the root is anchored and the top dances
                float  hf      = saturate(posOS.y * 1.5);

                // Primary side-to-side sway
                posOS.x += sin(t          + phase       ) * _SwayStrength * hf;
                // Secondary front-back sway at a different frequency (golden ratio offset)
                posOS.z += sin(t * 0.78   + phase + 1.9 ) * _SwayStrength * 0.55 * hf;
                // Playful vertical bob at the tip
                posOS.y += sin(t * 1.41   + phase + 0.7 ) * _BobStrength  * hf;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                ApplySway(IN.positionOS.xyz);

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vpi.positionCS;
                OUT.positionWS = vpi.positionWS;
                OUT.uv         = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                OUT.normalWS   = vni.normalWS;
                OUT.tangentWS  = float4(vni.tangentWS, IN.tangentOS.w);

                OUTPUT_LIGHTMAP_UV(IN.staticLightmapUV, unity_LightmapST, OUT.staticLightmapUV);
                OUTPUT_SH(vni.normalWS, OUT.vertexSH);

                half3 vl        = VertexLighting(vpi.positionWS, vni.normalWS);
                half  fogFactor = ComputeFogFactor(vpi.positionCS.z);
                OUT.fogAndLight = half4(fogFactor, vl);
                OUT.shadowCoord = GetShadowCoord(vpi);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 baseAlpha = SampleAlbedoAlpha(IN.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half  alpha     = baseAlpha.a * _BaseColor.a;
                clip(alpha - _Cutoff);

                half3 normalTS = SampleNormal(IN.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

                float3 biTan   = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                float3x3 tbn   = float3x3(IN.tangentWS.xyz, biTan, IN.normalWS);
                float3 normalWS = TransformTangentToWorld(normalTS, tbn, true);

                InputData id = (InputData)0;
                id.positionWS            = IN.positionWS;
                id.normalWS              = normalWS;
                id.viewDirectionWS       = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                id.shadowCoord           = IN.shadowCoord;
                id.fogCoord              = IN.fogAndLight.x;
                id.vertexLighting        = IN.fogAndLight.yzw;
                id.bakedGI               = SAMPLE_GI(IN.staticLightmapUV, IN.vertexSH, normalWS);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                id.shadowMask            = SAMPLE_SHADOWMASK(IN.staticLightmapUV);

                SurfaceData sd = (SurfaceData)0;
                sd.albedo    = baseAlpha.rgb * _BaseColor.rgb;
                sd.alpha     = alpha;
                sd.metallic  = _Metallic;
                sd.smoothness= _Smoothness;
                sd.normalTS  = normalTS;
                sd.occlusion = 1.0h;

                half4 color = UniversalFragmentPBR(id, sd);
                color.rgb   = MixFog(color.rgb, id.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ── Shadow Caster ─────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                half   _BumpScale;
                half   _Metallic;
                half   _Smoothness;
                half   _SwaySpeed;
                half   _SwayStrength;
                half   _BobStrength;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv         : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void ApplySway(inout float3 posOS)
            {
                float3 wsBase = TransformObjectToWorld(float3(0, 0, 0));
                float  phase  = dot(floor(wsBase.xz * 0.5), float2(1.618, 2.399));
                float  t      = _Time.y * _SwaySpeed;
                float  hf     = saturate(posOS.y * 1.5);
                posOS.x += sin(t         + phase      ) * _SwayStrength * hf;
                posOS.z += sin(t * 0.78  + phase + 1.9) * _SwayStrength * 0.55 * hf;
                posOS.y += sin(t * 1.41  + phase + 0.7) * _BobStrength  * hf;
            }

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                ApplySway(IN.positionOS.xyz);

                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.uv        = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                OUT.positionCS= posCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 s = SampleAlbedoAlpha(IN.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                clip(s.a * _BaseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ── Depth Only ────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                half   _BumpScale;
                half   _Metallic;
                half   _Smoothness;
                half   _SwaySpeed;
                half   _SwayStrength;
                half   _BobStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv         : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void ApplySway(inout float3 posOS)
            {
                float3 wsBase = TransformObjectToWorld(float3(0, 0, 0));
                float  phase  = dot(floor(wsBase.xz * 0.5), float2(1.618, 2.399));
                float  t      = _Time.y * _SwaySpeed;
                float  hf     = saturate(posOS.y * 1.5);
                posOS.x += sin(t         + phase      ) * _SwayStrength * hf;
                posOS.z += sin(t * 0.78  + phase + 1.9) * _SwayStrength * 0.55 * hf;
                posOS.y += sin(t * 1.41  + phase + 0.7) * _BobStrength  * hf;
            }

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                ApplySway(IN.positionOS.xyz);
                OUT.uv        = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                OUT.positionCS= TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 s = SampleAlbedoAlpha(IN.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                clip(s.a * _BaseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
