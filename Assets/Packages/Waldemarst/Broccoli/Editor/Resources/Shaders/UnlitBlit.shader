// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Hidden/BroccoUnlitBlit" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _RectOrigin ("Rect Origin", Vector) = (0, 0, 0, 0)
    _RectSize ("Rect Size", Vector) = (1, 1, 1, 1)
    _ApplyGammaCorrection ("Apply Gamma Correction", Float) = 0
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    //Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Overlay"}
    LOD 100

    ZWrite Off
    Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha

    Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag Lambert keepalpha
            #pragma target 2.0
            #pragma multi_compile_fog
            //#pragma surface surf Lambert keepalpha

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ApplyGammaCorrection;
            uniform float4 _RectOrigin;
            uniform float4 _RectSize;

            v2f vert (appdata_t v)
            {
                /*
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
                */
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv1 = TRANSFORM_TEX(v.uv, _MainTex);

                // Calculate the UV coordinates based on the rect parameters
                o.uv = (v.uv - _RectOrigin.xy) / _RectSize.xy;

                // Clamp the UV coordinates to make sure they are within the rect
                //o.uv = saturate(o.uv);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture using the adjusted UV coordinates
                //fixed4 col = tex2D(_MainTex, i.uv);

                // Extract rect parameters
                float xmin = _RectOrigin.x;
                float ymin = _RectOrigin.y;
                float xmax = xmin + _RectSize.x;
                float ymax = ymin + _RectSize.y;

                // Calculate alpha based on being inside the rect
                float alpha = (i.uv1.x >= xmin && i.uv1.x <= xmax && i.uv1.y >= ymin && i.uv1.y <= ymax) ? 1.0 : 0.0;

                // Sample texture and apply alpha
                float4 color = tex2D(_MainTex, i.uv);

                // Draw only the rect.
                color.a *= alpha;

                // Set color outside the rect to black to prevent alpha blending to lighten the rest of the texture.
                if (alpha == 0.0) {
                    color.r = 0;
                    color.g = 0;
                    color.b = 0;
                }
                
                // Output the sampled color
                return color;
            }
        ENDCG

        SetTexture[_MainTex] {
            combine primary
        }
    }
}

}