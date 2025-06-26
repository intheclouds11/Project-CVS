Shader "Hidden/Broccoli/BillboardSubsurface"
{
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _SubTex ("Base (RGB) Trans (A)", 2D) = "black" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.4
        _UseAlbedoTex ("Use Albedo", Float) = 0
        
        _TintColor ("Tint Color", Color) = (1,1,1,1)

        _IsLinearColorSpace ("Is Linear Color Space", Float) = 0
    }
    SubShader {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        LOD 100

        Cull Off
        Lighting Off

        Pass {
            Name "Albedo"
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_fog
                
                #include "UnityCG.cginc"
                #include "BlendModes.hlsl"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                sampler2D _SubTex;
                float4 _MainTex_ST;
                fixed _Cutoff;
                float _UseAlbedoTex;
                float4 col;

                float4 _TintColor;
                float _MinSproutTint;
                float _MaxSproutTint;
                uniform int _SproutTintMode;
                uniform int _InvertSproutTintMode;
                float _SproutTintVariance;
                float _RandSproutTint;
                float _PosSproutTint;
                float _SproutTint;

                float _BranchShade;
                float _BranchSat;

                float _MinSproutSat;
                float _MaxSproutSat;
                uniform int _SproutSatMode;
                uniform int _InvertSproutSatMode;
                float _SproutSatVariance;
                float _RandSproutSat;
                float _PosSproutSat;
                float _SproutSat;
                float _ApplyExtraSat;

                float _IsLinearColorSpace;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    o.color = v.color;
                    o.uv5 = v.uv5;
                    o.uv6 = v.uv6;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }
                
                fixed4 frag (v2f i) : Color
                {
                    // Get the albedo texture.
                    fixed4 alb = tex2D(_MainTex, i.texcoord);
                    // Get the subsurface texture.
                    fixed4 sub = tex2D(_SubTex, i.texcoord);
                    
                    if (_IsLinearColorSpace) {
                        //sub.rgb = pow (col.rgb, 0.4545);
                        //_TintColor.rgb = pow (_TintColor.rgb, 0.4545);
                    }
                    
                    float alpha = alb.a;
                    clip(alpha - _Cutoff);

                    if (_UseAlbedoTex == 1) {
                        col = alb;
                    } else {
                        col = sub;
                    }

                    if (abs(i.uv5.w - 1.0) < 0.01) { // Geometry is sprout, uv5.w == 1.
                        col.rgb = BlendMultiply (col, _TintColor);
                    } else {
                        col.rgb = ContrastSaturationBrightness (col.rgb, 0.5, 1, 1);
                        if (_IsLinearColorSpace)
                            col.rgb = pow (col.rgb, 2.2);
                    }

                    col.a = alpha;
                    return  col;
                }
                
            ENDHLSL
        }
    }
}