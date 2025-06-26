Shader "Hidden/Broccoli/BillboardExtra"
{
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _ExtraTex ("Smoothness R, Metallic G, AO A", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.4
        _Smoothness ("Smoothness", Float) = 0
        _Metallic ("Metallic", Float) = 0
        _UseTex ("Use Texture", Float) = 0

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
                sampler2D _ExtraTex;
                float4 _MainTex_ST;
                fixed _Cutoff;
                float _Smoothness;
                float _Metallic;
                float _UseTex;

                float4 col;

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
                    // Get the extra texture.
                    fixed4 extra = tex2D(_ExtraTex, i.texcoord);
                    
                    float alpha = alb.a;
                    clip(alpha - _Cutoff);

                    if (_UseTex == 1) {
                        col = extra;
                    } else {
                        col = float4(_Smoothness, _Metallic, 1, 1);
                    }
                    col.a = alpha;

                    return  col;
                }
                
            ENDHLSL
        }
    }
}