Shader "Hidden/BroccoUnlitBlit" {
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _RectOrigin ("Rect Origin", Vector) = (0, 0, 0, 0)
        _RectSize ("Rect Size", Vector) = (1, 1, 1, 1)
        _ApplyGammaCorrection ("Apply Gamma Correction", Float) = 0
    
        // ADDED: Parameter to signal desired filtering (controlled externally via C#)
        // 0 = Use texture's default (likely Bilinear), 1 = Force Point filtering via C#
        _UsePointFilter ("Use Point Filtering (Set Externally)", Float) = 1
    }
    
    SubShader {
        //Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Overlay"}
        LOD 100
    
        ZWrite Off
        // Blend One OneMinusSrcAlpha // Original blend mode - standard alpha blend
        // Corrected Blend syntax: Separate Alpha? Or just standard?
        Blend SrcAlpha OneMinusSrcAlpha // Standard Alpha Blending
    
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                // Note: removed 'Lambert' - not relevant for Unlit. 'keepalpha' is implicit with blending.
                #pragma target 2.0
                #pragma multi_compile_fog // Fog might still be relevant depending on use case
    
                #include "UnityCG.cginc"
    
                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
    
                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;  // UV mapped by Rect params
                    float2 uv1 : TEXCOORD1; // Original UV (used for rect check)
                    UNITY_FOG_COORDS(1) // Renumbered FOG texcoord due to uv/uv1
                    UNITY_VERTEX_OUTPUT_STEREO
                };
    
                sampler2D _MainTex;
                float4 _MainTex_ST; // Needed if using TRANSFORM_TEX macro
                float _ApplyGammaCorrection; // Note: This is unused in the provided frag shader
                uniform float4 _RectOrigin;
                uniform float4 _RectSize;
                // Declare the added property so the shader knows about it (even if not used directly in HLSL)
                uniform float _UsePointFilter;
    
                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.pos = UnityObjectToClipPos(v.vertex);
    
                    // Store original UVs before modification for rect check
                    o.uv1 = TRANSFORM_TEX(v.uv, _MainTex);
    
                    // Calculate the UV coordinates based on the rect parameters
                    // This remaps the texture UVs to fit within the specified rect
                    o.uv = (v.uv - _RectOrigin.xy) / _RectSize.xy;
    
                    // Clamp the UV coordinates (optional, depends if you want repeating outside rect)
                    // o.uv = saturate(o.uv);
    
                    UNITY_TRANSFER_FOG(o,o.pos); // Use o.pos for fog calculation
                    return o;
                }
    
                fixed4 frag (v2f i) : SV_Target
                {
                    // Extract rect parameters (used for alpha calculation)
                    float xmin = _RectOrigin.x;
                    float ymin = _RectOrigin.y;
                    float xmax = xmin + _RectSize.x;
                    float ymax = ymin + _RectSize.y;
    
                    // Calculate alpha based on the *original* UV coordinates being inside the rect bounds
                    float alpha = (i.uv1.x >= xmin && i.uv1.x <= xmax && i.uv1.y >= ymin && i.uv1.y <= ymax) ? 1.0 : 0.0;
    
                    // Sample the texture using the *adjusted* UV coordinates (i.uv)
                    // tex2D respects the filter mode set on the _MainTex texture object externally
                    float4 color = tex2D(_MainTex, i.uv);
    
                    // Apply the calculated rect alpha mask
                    color.a *= alpha;
    
                    // Optional: Set color outside the rect to black (as in original)
                    // This prevents unexpected color bleeding if alpha isn't exactly 0 or 1 due to filtering
                    // if (alpha == 0.0) {
                    //     color.rgb = 0; // Set RGB to black
                    // }
                    // A potentially better way to handle the edge case with filtering:
                    // Only return color if alpha is definitively 1, otherwise return transparent black
                    if (alpha < 0.999) { // Use a threshold robust to potential float inaccuracy
                        return fixed4(0,0,0,0); // Return transparent black outside rect
                    }
    
    
                    // Apply fog
                    UNITY_APPLY_FOG(i.fogCoord, color);
    
                     // Apply Gamma Correction (if enabled externally) - UNUSED in original logic?
                    // if(_ApplyGammaCorrection > 0.5) {
                    //     color.rgb = pow(color.rgb, 2.2); // Assuming Linear -> Gamma
                    // }
    
                    // Output the final color
                    return color;
                }
            ENDCG
    
            // Removed SetTexture block - generally not needed/used with programmable shaders
            /*
            SetTexture[_MainTex] {
                combine primary
            }
            */
        }
    }
}