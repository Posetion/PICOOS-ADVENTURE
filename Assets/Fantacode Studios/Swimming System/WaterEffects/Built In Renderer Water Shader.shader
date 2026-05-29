Shader "Custom/Built In Renderer Water Shader"
{
    Properties
    {
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _WaterColor ("Water Color", Color) = (0.2, 0.5, 0.7, 0.85)
        _DeepWaterColor ("Deep Water Color", Color) = (0.05, 0.2, 0.35, 1.0)
        _Transparency ("Transparency", Range(0, 1)) = 0.75
        _WaveSpeed ("Wave Speed", Float) = 0.8
        _WaveScale ("Wave Scale", Float) = 0.5
        _WaveHeight ("Wave Height", Float) = 0.15
        _FresnelPower ("Fresnel Power", Float) = 3.5
        _Reflectivity ("Reflectivity", Range(0, 1)) = 0.7
        _Refraction ("Refraction Strength", Float) = 0.08
        _EdgeFade ("Edge Fade Distance", Float) = 3.0
        _Shininess ("Shininess", Range(0.03, 1)) = 0.2
        _ReflectionTex ("Reflection", 2D) = "black" {}
        _ReflectionDistortion ("Reflection Distortion", Float) = 0.15
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        GrabPass { "_GrabTexture" }
        
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back  // Render front faces
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBinormal : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float3 viewDir : TEXCOORD6;
                UNITY_FOG_COORDS(7)
                LIGHTING_COORDS(8,9)
            };
            
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            sampler2D _ReflectionTex;
            sampler2D _CameraDepthTexture;
            sampler2D _GrabTexture;
            
            fixed4 _WaterColor;
            fixed4 _DeepWaterColor;
            float _Transparency;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveHeight;
            float _FresnelPower;
            float _Reflectivity;
            float _Refraction;
            float _EdgeFade;
            float _Shininess;
            float _ReflectionDistortion;
            
            // Simplex noise functions
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
            float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }
            
            float snoise(float3 v) {
                const float2 C = float2(1.0/6.0, 1.0/3.0);
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);
                
                float3 i = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);
                
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);
                
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - D.yyy;
                
                i = mod289(i);
                float4 p = permute(permute(permute(
                           i.z + float4(0.0, i1.z, i2.z, 1.0))
                         + i.y + float4(0.0, i1.y, i2.y, 1.0))
                         + i.x + float4(0.0, i1.x, i2.x, 1.0));
                
                float n_ = 0.142857142857;
                float3 ns = n_ * D.wyz - D.xzx;
                float4 j = p - 49.0 * floor(p * ns.z * ns.z);
                
                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_);
                
                float4 x = x_ *ns.x + ns.yyyy;
                float4 y = y_ *ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);
                
                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);
                
                float4 s0 = floor(b0)*2.0 + 1.0;
                float4 s1 = floor(b1)*2.0 + 1.0;
                float4 sh = -step(h, float4(0,0,0,0));
                
                float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw*sh.zzww;
                
                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);
                
                float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;
                
                float4 m = max(0.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
                m = m * m;
                return 42.0 * dot(m*m, float4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3)));
            }
            
            // Generate base ocean waves
            float GetWaveHeight(float3 worldPos) {
                float time = _Time.y * _WaveSpeed;
                float wave1 = snoise(float3(worldPos.x * _WaveScale, worldPos.z * _WaveScale, time * 0.5)) * _WaveHeight;
                float wave2 = snoise(float3(worldPos.x * _WaveScale * 1.7, worldPos.z * _WaveScale * 1.3, time * 0.7)) * _WaveHeight * 0.5;
                float wave3 = snoise(float3(worldPos.x * _WaveScale * 2.3, worldPos.z * _WaveScale * 2.1, time * 1.1)) * _WaveHeight * 0.25;
                return wave1 + wave2 + wave3;
            }
            
            // Calculate wave normal with improved stability
            float3 GetWaveNormal(float3 worldPos, float offset = 0.02) {  // Increased offset for stability
                float h1 = GetWaveHeight(worldPos + float3(offset, 0, 0));
                float h2 = GetWaveHeight(worldPos - float3(offset, 0, 0));
                float h3 = GetWaveHeight(worldPos + float3(0, 0, offset));
                float h4 = GetWaveHeight(worldPos - float3(0, 0, offset));
                
                float3 tangentX = float3(2.0 * offset, h1 - h2, 0);
                float3 tangentZ = float3(0, h3 - h4, 2.0 * offset);
                return normalize(cross(tangentZ, tangentX));  // Proper cross product order
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                // Apply wave displacement
                float waveHeight = GetWaveHeight(worldPos.xyz);
                worldPos.y += waveHeight;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos = worldPos.xyz;
                
                // Calculate proper normals without flipping in vertex shader
                float3 originalNormal = UnityObjectToWorldNormal(v.normal);
                float3 waveNormal = GetWaveNormal(worldPos.xyz);
                o.worldNormal = normalize(lerp(originalNormal, waveNormal, 0.5));  // Blend instead of add
                
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangent.w * unity_WorldTransformParams.w;
                o.uv = TRANSFORM_TEX(v.uv, _NormalMap);
                o.screenPos = ComputeScreenPos(o.pos);
                o.viewDir = WorldSpaceViewDir(worldPos);
                
                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float3 worldPos = i.worldPos;
                float3 viewDir = normalize(i.viewDir);
                
                // Depth calculation
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                float surfaceZ = i.screenPos.z;
                float depthDifference = sceneZ - surfaceZ;
                
                // Animated normal maps with reduced speed multiplier
                float timeScale = _Time.y * _WaveSpeed * 0.5;  // Reduced multiplier
                float2 animatedUV1 = i.uv + timeScale * float2(0.02, 0.03);
                float2 animatedUV2 = i.uv + timeScale * float2(-0.015, 0.025);
                
                float3 normal1 = UnpackNormal(tex2D(_NormalMap, animatedUV1 * 2.0));
                float3 normal2 = UnpackNormal(tex2D(_NormalMap, animatedUV2 * 1.5));
                float3 tangentNormal = normalize(normal1 + normal2);
                
                // Transform to world space
                float3x3 TBN = float3x3(normalize(i.worldTangent), normalize(i.worldBinormal), normalize(i.worldNormal));
                float3 worldNormal = normalize(mul(tangentNormal, TBN));
                
                // Combine with procedural waves more carefully
                float3 waveNormal = GetWaveNormal(worldPos);
                worldNormal = normalize(lerp(worldNormal, waveNormal, 0.3));  // Use lerp for smoother blending
                
                // Single normal flip check (only if needed)
                float normalDot = dot(worldNormal, viewDir);
                if (normalDot < 0) {
                    worldNormal = -worldNormal;
                }
                
                // Lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotL = saturate(dot(worldNormal, lightDir));
                float NdotH = saturate(dot(worldNormal, halfVector));
                
                float3 diffuse = _LightColor0.rgb * NdotL * 0.6;
                float specPower = exp2(10 * _Shininess + 1);
                float3 specular = _LightColor0.rgb * pow(NdotH, specPower) * _Shininess * 1.5;
                
                // Fresnel effect
                float fresnel = pow(1.0 - saturate(abs(normalDot)), _FresnelPower);  // Use abs for stability
                
                // Enhanced refraction
                float2 refractionUV = screenUV + worldNormal.xz * _Refraction * saturate(depthDifference * 0.1);
                refractionUV = saturate(refractionUV);  // Clamp to prevent sampling outside texture
                float3 refractedColor = tex2D(_GrabTexture, refractionUV).rgb;
                
                // Water color
                float depthFactor = saturate(depthDifference / _EdgeFade);
                float3 waterColor = lerp(_WaterColor.rgb, _DeepWaterColor.rgb, depthFactor);
                
                // Enhanced reflection with distortion
                float2 reflectionUV = screenUV + worldNormal.xz * _ReflectionDistortion;
                reflectionUV = saturate(reflectionUV);  // Clamp to prevent sampling outside texture
                float3 reflection = tex2D(_ReflectionTex, reflectionUV).rgb;
                
                // Combine colors
                float3 finalColor = lerp(refractedColor * waterColor, waterColor, depthFactor * 0.7);
                finalColor += diffuse * waterColor * 0.8;
                finalColor += specular * (1.0 + fresnel * 0.5);
                
                // Apply reflection with fresnel
                float reflectionStrength = _Reflectivity * (fresnel * 0.7 + 0.3);
                finalColor = lerp(finalColor, reflection, reflectionStrength);
                
                // Alpha
                float alpha = lerp(_Transparency, 1.0, depthFactor);
                alpha = saturate(alpha);
                
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
        
        // Second pass for back faces
        Pass
        {
            Name "FORWARD_BACK"
            Tags { "LightMode" = "ForwardBase" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front  // Render back faces
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBinormal : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float3 viewDir : TEXCOORD6;
                UNITY_FOG_COORDS(7)
                LIGHTING_COORDS(8,9)
            };
            
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            sampler2D _ReflectionTex;
            sampler2D _CameraDepthTexture;
            sampler2D _GrabTexture;
            
            fixed4 _WaterColor;
            fixed4 _DeepWaterColor;
            float _Transparency;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveHeight;
            float _FresnelPower;
            float _Reflectivity;
            float _Refraction;
            float _EdgeFade;
            float _Shininess;
            float _ReflectionDistortion;
            
            // Simplex noise functions
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
            float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }
            
            float snoise(float3 v) {
                const float2 C = float2(1.0/6.0, 1.0/3.0);
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);
                
                float3 i = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);
                
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);
                
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - D.yyy;
                
                i = mod289(i);
                float4 p = permute(permute(permute(
                           i.z + float4(0.0, i1.z, i2.z, 1.0))
                         + i.y + float4(0.0, i1.y, i2.y, 1.0))
                         + i.x + float4(0.0, i1.x, i2.x, 1.0));
                
                float n_ = 0.142857142857;
                float3 ns = n_ * D.wyz - D.xzx;
                float4 j = p - 49.0 * floor(p * ns.z * ns.z);
                
                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_);
                
                float4 x = x_ *ns.x + ns.yyyy;
                float4 y = y_ *ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);
                
                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);
                
                float4 s0 = floor(b0)*2.0 + 1.0;
                float4 s1 = floor(b1)*2.0 + 1.0;
                float4 sh = -step(h, float4(0,0,0,0));
                
                float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw*sh.zzww;
                
                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);
                
                float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;
                
                float4 m = max(0.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
                m = m * m;
                return 42.0 * dot(m*m, float4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3)));
            }
            
            // Generate base ocean waves
            float GetWaveHeight(float3 worldPos) {
                float time = _Time.y * _WaveSpeed;
                float wave1 = snoise(float3(worldPos.x * _WaveScale, worldPos.z * _WaveScale, time * 0.5)) * _WaveHeight;
                float wave2 = snoise(float3(worldPos.x * _WaveScale * 1.7, worldPos.z * _WaveScale * 1.3, time * 0.7)) * _WaveHeight * 0.5;
                float wave3 = snoise(float3(worldPos.x * _WaveScale * 2.3, worldPos.z * _WaveScale * 2.1, time * 1.1)) * _WaveHeight * 0.25;
                return wave1 + wave2 + wave3;
            }
            
            // Calculate wave normal with improved stability
            float3 GetWaveNormal(float3 worldPos, float offset = 0.02) {
                float h1 = GetWaveHeight(worldPos + float3(offset, 0, 0));
                float h2 = GetWaveHeight(worldPos - float3(offset, 0, 0));
                float h3 = GetWaveHeight(worldPos + float3(0, 0, offset));
                float h4 = GetWaveHeight(worldPos - float3(0, 0, offset));
                
                float3 tangentX = float3(2.0 * offset, h1 - h2, 0);
                float3 tangentZ = float3(0, h3 - h4, 2.0 * offset);
                return normalize(cross(tangentZ, tangentX));
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                // Apply wave displacement
                float waveHeight = GetWaveHeight(worldPos.xyz);
                worldPos.y += waveHeight;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos = worldPos.xyz;
                
                // Calculate proper normals - flip for back faces
                float3 originalNormal = UnityObjectToWorldNormal(v.normal);
                float3 waveNormal = GetWaveNormal(worldPos.xyz);
                o.worldNormal = -normalize(lerp(originalNormal, waveNormal, 0.5));  // Flip for back face
                
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangent.w * unity_WorldTransformParams.w;
                o.uv = TRANSFORM_TEX(v.uv, _NormalMap);
                o.screenPos = ComputeScreenPos(o.pos);
                o.viewDir = WorldSpaceViewDir(worldPos);
                
                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float3 worldPos = i.worldPos;
                float3 viewDir = normalize(i.viewDir);
                
                // Depth calculation
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                float surfaceZ = i.screenPos.z;
                float depthDifference = sceneZ - surfaceZ;
                
                // Animated normal maps with reduced speed multiplier
                float timeScale = _Time.y * _WaveSpeed * 0.5;
                float2 animatedUV1 = i.uv + timeScale * float2(0.02, 0.03);
                float2 animatedUV2 = i.uv + timeScale * float2(-0.015, 0.025);
                
                float3 normal1 = UnpackNormal(tex2D(_NormalMap, animatedUV1 * 2.0));
                float3 normal2 = UnpackNormal(tex2D(_NormalMap, animatedUV2 * 1.5));
                float3 tangentNormal = normalize(normal1 + normal2);
                
                // Transform to world space
                float3x3 TBN = float3x3(normalize(i.worldTangent), normalize(i.worldBinormal), normalize(i.worldNormal));
                float3 worldNormal = normalize(mul(tangentNormal, TBN));
                
                // Combine with procedural waves more carefully
                float3 waveNormal = GetWaveNormal(worldPos);
                worldNormal = normalize(lerp(worldNormal, waveNormal, 0.3));
                
                // Lighting (same as front face)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotL = saturate(dot(worldNormal, lightDir));
                float NdotH = saturate(dot(worldNormal, halfVector));
                
                float3 diffuse = _LightColor0.rgb * NdotL * 0.6;
                float specPower = exp2(10 * _Shininess + 1);
                float3 specular = _LightColor0.rgb * pow(NdotH, specPower) * _Shininess * 1.5;
                
                // Fresnel effect
                float normalDot = dot(worldNormal, viewDir);
                float fresnel = pow(1.0 - saturate(abs(normalDot)), _FresnelPower);
                
                // Enhanced refraction
                float2 refractionUV = screenUV + worldNormal.xz * _Refraction * saturate(depthDifference * 0.1);
                refractionUV = saturate(refractionUV);
                float3 refractedColor = tex2D(_GrabTexture, refractionUV).rgb;
                
                // Water color
                float depthFactor = saturate(depthDifference / _EdgeFade);
                float3 waterColor = lerp(_WaterColor.rgb, _DeepWaterColor.rgb, depthFactor);
                
                // Enhanced reflection with distortion
                float2 reflectionUV = screenUV + worldNormal.xz * _ReflectionDistortion;
                reflectionUV = saturate(reflectionUV);
                float3 reflection = tex2D(_ReflectionTex, reflectionUV).rgb;
                
                // Combine colors
                float3 finalColor = lerp(refractedColor * waterColor, waterColor, depthFactor * 0.7);
                finalColor += diffuse * waterColor * 0.8;
                finalColor += specular * (1.0 + fresnel * 0.5);
                
                // Apply reflection with fresnel
                float reflectionStrength = _Reflectivity * (fresnel * 0.7 + 0.3);
                finalColor = lerp(finalColor, reflection, reflectionStrength);
                
                // Alpha
                float alpha = lerp(_Transparency, 1.0, depthFactor);
                alpha = saturate(alpha);
                
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    
    Fallback "Transparent/VertexLit"
}
