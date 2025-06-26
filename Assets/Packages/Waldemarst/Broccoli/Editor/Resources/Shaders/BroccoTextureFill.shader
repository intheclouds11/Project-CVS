// Shader: Hidden/FillTransparentFallback
// Fills pixels below an alpha threshold with a fallback color.
Shader "Hidden/Broccoli/TextureFill"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FallbackColor ("Fallback Color", Color) = (0,0,0,0)
        // Alpha threshold below which a pixel is considered 'empty'
         _AlphaThreshold("Alpha Threshold", Range(0.01, 1.0)) = 0.01
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img // Uses Unity's built-in image effect vertex shader
            #pragma fragment frag
            #include "UnityCG.cginc" // Includes vert_img and other utilities

            // Define the struct that passes data from vertex to fragment shader
            // This matches the output of vert_img
            struct v2f {
                float4 vertex : SV_POSITION; // Clip space vertex position
                float2 uv : TEXCOORD0;       // Texture coordinates
            };

            sampler2D _MainTex;
            fixed4 _FallbackColor;
             float _AlphaThreshold;

            // Fragment shader now correctly receives the 'v2f' struct named 'i'
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 inputColor = tex2D(_MainTex, i.uv); // Access uv via the input 'i'

                // If the input pixel is below the alpha threshold, output the fallback color
                if (inputColor.a < _AlphaThreshold)
                {
                    return _FallbackColor;
                }

                // Otherwise, output the original input color
                return inputColor;
            }
            ENDCG
        }
    }
}