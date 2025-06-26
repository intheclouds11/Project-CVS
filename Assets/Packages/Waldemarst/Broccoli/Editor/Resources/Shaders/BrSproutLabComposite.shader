// Based on Unity Nature/SpeedTree8

Shader "Hidden/Broccoli/SproutLabComposite"
{
    Properties
    {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Toggle(EFFECT_HUE_VARIATION)] _HueVariationKwToggle("Hue Variation", Float) = 0
        _HueVariationColor ("Hue Variation Color", Color) = (1.0,0.5,0.0,0.1)

        [Toggle(EFFECT_BUMP)] _NormalMapKwToggle("Normal Mapping", Float) = 0
        _BumpMap ("Normalmap", 2D) = "bump" {}

        _ExtraTex ("Smoothness (R), Metallic (G), AO (B)", 2D) = "(0.5, 0.0, 1.0)" {}
        _Glossiness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0

        [Toggle(EFFECT_SUBSURFACE)] _SubsurfaceKwToggle("Subsurface", Float) = 0
        _SubsurfaceTex ("Subsurface (RGB)", 2D) = "white" {}
        _SubsurfaceColor ("Subsurface Color", Color) = (1,1,1,1)
        _SubsurfaceIndirect ("Subsurface Indirect", Range(0.0, 1.0)) = 0.25

        [Toggle(EFFECT_BILLBOARD)] _BillboardKwToggle("Billboard", Float) = 0
        _BillboardShadowFade ("Billboard Shadow Fade", Range(0.0, 1.0)) = 0.5

        [Enum(No,2,Yes,0)] _TwoSided ("Two Sided", Int) = 2 // enum matches cull mode
        [KeywordEnum(None,Fastest,Fast,Better,Best,Palm)] _WindQuality ("Wind Quality", Range(0,5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "DisableBatching"="LODFading"
        }
        LOD 400
        Cull [_TwoSided]

        CGPROGRAM
            #pragma surface SpeedTreeSurfA SpeedTreeSubsurface vertex:SpeedTreeVertA dithercrossfade addshadow
            #pragma target 3.0
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature_local _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
            #pragma shader_feature_local EFFECT_BILLBOARD
            #pragma shader_feature_local EFFECT_HUE_VARIATION
            #pragma shader_feature_local EFFECT_SUBSURFACE
            #pragma shader_feature_local EFFECT_BUMP
            #pragma shader_feature_local EFFECT_EXTRA_TEX

            #define ENABLE_WIND
            #define EFFECT_BACKSIDE_NORMALS
            #include "SpeedTree8Common.cginc"

            void SpeedTreeVertA(inout appdata_full v)
            {
            // handle speedtree wind and lod
            OffsetSpeedTreeVertex(v, unity_LODFade.x);

            float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

            #if defined(EFFECT_BILLBOARD)

                // crossfade faces
                bool topDown = (v.texcoord.z > 0.5);
                float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
                float3 cameraDir = normalize(mul((float3x3)unity_WorldToObject, _WorldSpaceCameraPos - treePos));
                float viewDot = max(dot(viewDir, v.normal), dot(cameraDir, v.normal));
                viewDot *= viewDot;
                viewDot *= viewDot;
                viewDot += topDown ? 0.38 : 0.18; // different scales for horz and vert billboards to fix transition zone
                v.color = float4(1, 1, 1, clamp(viewDot, 0, 1));

                // if invisible, avoid overdraw
                if (viewDot < 0.3333)
                {
                    v.vertex.xyz = float3(0,0,0);
                }

                // adjust lighting on billboards to prevent seams between the different faces
                if (topDown)
                {
                    v.normal += cameraDir;
                }
                else
                {
                    half3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
                    float3 right = cross(cameraDir, binormal);
                    v.normal = cross(binormal, right);
                }
                v.normal = normalize(v.normal);

            #endif

            // color already contains (ao, ao, ao, blend)
            // put hue variation amount in there
            #ifdef EFFECT_HUE_VARIATION
                float hueVariationAmount = frac(treePos.x + treePos.y + treePos.z);
                v.color.g = saturate(hueVariationAmount * _HueVariationColor.a);
            #endif
        }

        void SpeedTreeSurfA(Input IN, inout SurfaceOutputStandard OUT)
        {
            fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // transparency
            OUT.Alpha = color.a * IN.color.a;
            //clip(OUT.Alpha - 0.3333);
            clip(OUT.Alpha - 0.3);

            // color
            OUT.Albedo = color.rgb;

            // hue variation
            #ifdef EFFECT_HUE_VARIATION
                half3 shiftedColor = lerp(OUT.Albedo, _HueVariationColor.rgb, IN.color.g);

                // preserve vibrance
                half maxBase = max(OUT.Albedo.r, max(OUT.Albedo.g, OUT.Albedo.b));
                half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
                maxBase /= newMaxBase;
                maxBase = maxBase * 0.5f + 0.5f;
                shiftedColor.rgb *= maxBase;

                OUT.Albedo = saturate(shiftedColor);
            #endif

            // normal
            #ifdef EFFECT_BUMP
                OUT.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            #elif defined(EFFECT_BACKSIDE_NORMALS) || defined(EFFECT_BILLBOARD)
                OUT.Normal = float3(0, 0, 1);
            #endif

            // flip normal on backsides
            #ifdef EFFECT_BACKSIDE_NORMALS
                if (IN.facing < 0.5)
                {
                    OUT.Normal.z = -OUT.Normal.z;
                }
            #endif

            // adjust billboard normals to improve GI and matching
            #ifdef EFFECT_BILLBOARD
                OUT.Normal.z *= 0.5;
                OUT.Normal = normalize(OUT.Normal);
            #endif

            // extra
            #ifdef EFFECT_EXTRA_TEX
                fixed4 extra = tex2D(_ExtraTex, IN.uv_MainTex);
                OUT.Smoothness = extra.r;
                OUT.Metallic = extra.g;
                OUT.Occlusion = extra.b * IN.color.r;
            #else
                OUT.Smoothness = _Glossiness;
                OUT.Metallic = _Metallic;
                OUT.Occlusion = IN.color.r;
            #endif

            OUT.Albedo = OUT.Albedo * IN.color.r;

            // subsurface (hijack emissive)
            #ifdef EFFECT_SUBSURFACE
                OUT.Emission = tex2D(_SubsurfaceTex, IN.uv_MainTex) * _SubsurfaceColor;
            #endif
        }

        ENDCG
    }

    // targeting SM2.0: Many effects are disabled for fewer instructions
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "DisableBatching"="LODFading"
        }
        LOD 400
        Cull [_TwoSided]

        CGPROGRAM
            #pragma surface SpeedTreeSurf Standard vertex:SpeedTreeVert addshadow noinstancing
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma shader_feature_local EFFECT_BILLBOARD
            #pragma shader_feature_local EFFECT_EXTRA_TEX

            #include "SpeedTree8Common.cginc"

        ENDCG
    }

    FallBack "Transparent/Cutout/VertexLit"
    CustomEditor "SpeedTree8ShaderGUI"
}