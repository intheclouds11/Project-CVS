using UnityEngine;
using UnityEngine.Experimental.Rendering; // For GraphicsFormat/Utility
using UnityEngine.Rendering; // Might be needed for GraphicsFormat itself in some versions

namespace Broccoli.Utils
{
    public static class TextureDilationUtility
    {
        private static Material _dilationMaterial;
        private static Material _fallbackMaterial;

        // Shader names must match the ones created above
        private const string DILATION_SHADER_NAME = "Hidden/Broccoli/TextureDilation";
        private const string FILL_SHADER_NAME = "Hidden/Broccoli/TextureFill";
        private const float ALPHA_THRESHOLD = 0.15f; // Sensitivity for transparency detection

        private static void EnsureMaterials()
        {
            // --- Dilation Material ---
            if (_dilationMaterial == null)
            {
                Shader dilationShader = Shader.Find(DILATION_SHADER_NAME);
                if (dilationShader == null)
                {
                    // Fallback if not found (e.g., build issue) - shaders MUST be included!
                    Debug.LogError($"[TextureDilationUtility] Cannot find shader: {DILATION_SHADER_NAME}. Make sure it's included in 'Always Included Shaders' (Project Settings > Graphics) or in a Resources folder.");
                    // Optionally, try loading from Resources as a fallback path:
                    // dilationShader = Resources.Load<Shader>(DILATION_SHADER_NAME); // Path would need to be relative to a Resources folder
                }

                if (dilationShader != null && dilationShader.isSupported) {
                    _dilationMaterial = new Material(dilationShader);
                    _dilationMaterial.SetFloat("_AlphaThreshold", ALPHA_THRESHOLD);
                } else {
                     Debug.LogError($"[TextureDilationUtility] Shader {DILATION_SHADER_NAME} not found or not supported on this platform.");
                }
            }

            // --- Fallback Material ---
            if (_fallbackMaterial == null)
            {
                Shader fallbackShader = Shader.Find(FILL_SHADER_NAME);
                 if (fallbackShader == null)
                {
                     Debug.LogError($"[TextureDilationUtility] Cannot find shader: {FILL_SHADER_NAME}. Make sure it's included in 'Always Included Shaders' (Project Settings > Graphics) or in a Resources folder.");
                    // Optionally, try loading from Resources:
                    // fallbackShader = Resources.Load<Shader>(FILL_SHADER_NAME);
                }

                if (fallbackShader != null && fallbackShader.isSupported)
                {
                    _fallbackMaterial = new Material(fallbackShader);
                    _fallbackMaterial.SetFloat("_AlphaThreshold", ALPHA_THRESHOLD);
                } else {
                    Debug.LogError($"[TextureDilationUtility] Shader {FILL_SHADER_NAME} not found or not supported on this platform.");
                }
            }
        }

        /// <summary>
        /// Dilates texture colors outwards into transparent areas using GPU shaders.
        /// </summary>
        /// <param name="sourceTexture">The source Texture2D with transparency.</param>
        /// <param name="iterations">How many pixels outwards to dilate (number of passes).</param>
        /// <param name="fallbackColor">The color to fill any remaining transparent pixels after dilation.</param>
        /// <param name="createMipMaps">Should the resulting Texture2D have mipmaps?</param>
        /// <param name="linear">Should the resulting Texture2D use linear color space?</param> /// <!- Note: Texture2D constructor takes linear bool, not directly color space ->
        /// <returns>A new Texture2D with dilated colors, or null on failure.</returns>
        public static Texture2D DilateTexture(Texture sourceTexture, int iterations, Color fallbackColor, bool createMipMaps = false, bool linear = false)
        {
            if (sourceTexture == null)
            {
                Debug.LogError("[TextureDilationUtility] Source Texture is null.");
                return null;
            }

            // Make sure shaders are loaded and ready
            EnsureMaterials();
            if (_dilationMaterial == null || _fallbackMaterial == null)
            {
                Debug.LogError("[TextureDilationUtility] Failed to create necessary materials. Check previous log errors about shaders.");
                return null;
            }

            // Determine if source is HDR to choose appropriate intermediate formats
            GraphicsFormat sourceGraphicsFormat = sourceTexture.graphicsFormat;
            bool isHDRSource = GraphicsFormatUtility.IsFloatFormat(sourceGraphicsFormat) ||
                               sourceGraphicsFormat == GraphicsFormat.RGB_BC6H_SFloat || // Explicit check for compressed HDR
                               sourceGraphicsFormat == GraphicsFormat.RGB_BC6H_UFloat;

            // Choose RenderTextureFormat - use HDR if source is HDR, otherwise default LDR
            RenderTextureFormat rtFormat = isHDRSource ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            // Ensure the chosen format is supported, fallback if necessary
            if (!SystemInfo.SupportsRenderTextureFormat(rtFormat)) {
                Debug.LogWarning($"[TextureDilationUtility] RenderTextureFormat {rtFormat} not supported. Falling back.");
                rtFormat = isHDRSource ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32; // Common fallbacks
                if (!SystemInfo.SupportsRenderTextureFormat(rtFormat)) {
                     Debug.LogError($"[TextureDilationUtility] Fallback RenderTextureFormat {rtFormat} also not supported. Cannot proceed.");
                     return null;
                }
            }

            RenderTexture rt1 = null;
            RenderTexture rt2 = null;
            RenderTexture finalRT = null; // Will point to the RT holding the final result before readback
            Texture2D resultTexture = null;
            RenderTexture previousActive = RenderTexture.active; // Store initially active RT

            try // Use try-finally to ensure temporary RTs are released
            {
                // Get temporary RenderTextures
                rt1 = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, rtFormat);
                rt2 = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, rtFormat);

                // Check if RTs were successfully created
                if (rt1 == null || rt2 == null) {
                     Debug.LogError("[TextureDilationUtility] Failed to get temporary RenderTextures.");
                     return null;
                }
                
                RenderTexture currentSourceRT = rt1;
                RenderTexture currentTargetRT = rt2;

                // Blit the original texture to the first RT
                Graphics.Blit(sourceTexture, currentSourceRT);

                // --- Dilation Passes ---
                _dilationMaterial.SetFloat("_AlphaThreshold", ALPHA_THRESHOLD); // Ensure threshold is set
                for (int i = 0; i < iterations; i++)
                {
                    Graphics.Blit(currentSourceRT, currentTargetRT, _dilationMaterial);

                    // Swap RTs for next iteration (ping-pong)
                    RenderTexture temp = currentSourceRT;
                    currentSourceRT = currentTargetRT;
                    currentTargetRT = temp;
                }
                // After the loop, currentSourceRT holds the result of the last dilation pass

                // --- Fallback Color Pass ---
                _fallbackMaterial.SetColor("_FallbackColor", fallbackColor);
                _fallbackMaterial.SetFloat("_AlphaThreshold", ALPHA_THRESHOLD); // Ensure threshold is set
                finalRT = currentTargetRT; // The 'other' RT is the destination for this final pass
                Graphics.Blit(currentSourceRT, finalRT, _fallbackMaterial);
                // finalRT now holds the dilated image with the fallback color applied

                // --- Read back to Texture2D ---
                // *** KEY CHANGE HERE ***
                // Choose an appropriate UNCOMPRESSED TextureFormat for the result Texture2D
                // that ReadPixels can handle, based on whether we processed in HDR or LDR.
                TextureFormat resultFormat = isHDRSource ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;

                resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height, resultFormat, createMipMaps, linear);

                RenderTexture.active = finalRT; // Set the final RT as active
                resultTexture.ReadPixels(new Rect(0, 0, finalRT.width, finalRT.height), 0, 0);
                resultTexture.Apply(createMipMaps, false); // Apply changes, 'makeNoLongerReadable = false' is often desired for utility textures
                RenderTexture.active = previousActive; // Restore previous active RT
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TextureDilationUtility] Exception occurred: {ex.Message}\n{ex.StackTrace}");
                resultTexture = null; // Ensure null is returned on error
            }
            finally // Cleanup
            {
                RenderTexture.active = previousActive; // Ensure active RT is restored even if errors occurred

                if (rt1 != null) RenderTexture.ReleaseTemporary(rt1);
                if (rt2 != null) RenderTexture.ReleaseTemporary(rt2);
                // finalRT points to either rt1 or rt2, so no separate release needed
            }

            // Note: Materials are kept cached by default. Call CleanupMaterials() manually if needed.

            if (resultTexture == null) {
                 Debug.LogError("[TextureDilationUtility] Failed to create result texture. Check logs for errors.");
            }

            return resultTexture;
        }


        // Optional: Call this from somewhere appropriate if you want to clean up cached materials
        public static void CleanupMaterials() {
            if (_dilationMaterial != null) {
                // Use DestroyImmediate in Editor, Destroy otherwise
                #if UNITY_EDITOR
                    Object.DestroyImmediate(_dilationMaterial);
                #else
                    Object.Destroy(_dilationMaterial);
                #endif
                _dilationMaterial = null;
            }
            if (_fallbackMaterial != null) {
                #if UNITY_EDITOR
                     Object.DestroyImmediate(_fallbackMaterial);
                #else
                     Object.Destroy(_fallbackMaterial);
                #endif
                _fallbackMaterial = null;
            }
             Debug.Log("[TextureDilationUtility] Cleaned up cached materials.");
        }
    }
}