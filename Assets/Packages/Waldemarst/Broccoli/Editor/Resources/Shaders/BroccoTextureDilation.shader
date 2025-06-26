// Shader: Hidden/TextureDilation
// Performs one pass of texture dilation by checking neighbors.
Shader "Hidden/Broccoli/TextureDilation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // Alpha threshold below which a pixel is considered 'empty' for dilation source
        _AlphaThreshold("Alpha Threshold", Range(0.01, 1.0)) = 0.01
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img // Use vert_img for Graphics.Blit
            #pragma fragment frag
            #include "UnityCG.cginc" // Includes vert_img

            // Define the struct that passes data from vertex to fragment shader
            // This matches the output of vert_img
            struct v2f {
                float4 vertex : SV_POSITION; // Clip space vertex position
                float2 uv : TEXCOORD0;       // Texture coordinates
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity provides this: {1/W, 1/H, W, H}
            float _AlphaThreshold;

            // Fragment shader now correctly receives the 'v2f' struct named 'i'
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 centerCol = tex2D(_MainTex, i.uv); // Access uv via the input 'i'

                // If the center pixel is already opaque enough, keep it
                if (centerCol.a >= _AlphaThreshold)
                {
                    centerCol.a = 1;
                    return centerCol;
                }

                // Center pixel is transparent, check neighbors
                float2 texelSize = _MainTex_TexelSize.xy;
                float maxAlpha = 0;
                fixed4 colorToCopy = centerCol; // Start with original transparent color

                // Define neighbor offsets (4 direct + 4 diagonal = 8 neighbors)
                const int NUM_NEIGHBORS = 8;
                float2 neighborOffsets[NUM_NEIGHBORS] = {
                    float2(0, texelSize.y),           // N
                    float2(0, -texelSize.y),          // S
                    float2(-texelSize.x, 0),          // W
                    float2(texelSize.x, 0),           // E
                    float2(-texelSize.x, texelSize.y),  // NW
                    float2(texelSize.x, texelSize.y),   // NE
                    float2(-texelSize.x, -texelSize.y), // SW
                    float2(texelSize.x, -texelSize.y)   // SE
                };

                // Find neighbor with highest alpha (or just the first opaque one)
                for (int j = 0; j < NUM_NEIGHBORS; j++)
                {
                    fixed4 neighborCol = tex2D(_MainTex, i.uv + neighborOffsets[j]); // Access uv via 'i'
                    if (neighborCol.a >= _AlphaThreshold)
                    {
                        // Option 1: Take the first opaque neighbor found
                        // return neighborCol;

                        // Option 2: Take the neighbor with the highest alpha (might look slightly better)
                        if (neighborCol.a > maxAlpha) {
                             maxAlpha = neighborCol.a;
                             colorToCopy = neighborCol;
                        }
                    }
                }
                 // If an opaque neighbor was found (maxAlpha > 0), return its color
                 if (maxAlpha > 0) {
                    colorToCopy.a = 1;
                    return colorToCopy;
                 }

                // Otherwise, return the original transparent color
                return centerCol;
            }
            ENDCG
        }
    }
}