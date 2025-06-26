using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using UnityEngine;

using Broccoli.RectpackSharp;

namespace Broccoli.Utils
{
    public class TexturePacker
    {
        #region Vars
        /// <summary>
        /// Texture sizes available for the Destination Texture.
        /// </summary>
        public enum PixelSize {
            Px256,
            Px512,
            Px1024,
            Px2048,
            Px4096,
            Px8192
        }
        /// <summary>
        /// Mode to scale the textures when creating the packing.
        /// </summary>
        public enum PackMode {
            /// <summary>
            /// Texture scaling is applied evenly on all the source textured to be packed.
            /// </summary>
            ScaleEven,
            /// <summary>
            /// Texture scaling is applied taking the smallest texture as reference (bigger textures are scaled down).
            /// </summary>
            ScaleDown,
            /// <summary>
            /// Texture scaling is applied taking the biggest texture as reference (smaller textures are scaled up).
            /// </summary>
            ScaleUp,
            /// <summary>
            /// Texture scaling is applied to a median factor between all the source textures.
            /// </summary>
            ScaleMedian,
        }
        public static PackingRectangle packingRectangle;
        public static PackingRectangle[] packingRectangles = new PackingRectangle[0];
        private static int shaderRectOriginProp = Shader.PropertyToID ("_RectOrigin");
        private static int shaderRectSizeProp = Shader.PropertyToID ("_RectSize");
        #endregion

        #region Params Struct
        public struct TexturePackParams
        {
            public int padding;
            public PixelSize pixelSize;
            public Color bgColor;
            public bool applyGammaCorrection;
            /// <summary>
            /// Creates a <see cref="TexturePackParams"/> with the specified values.
            /// </summary>
            public TexturePackParams(int padding, PixelSize pixelSize, Color bgColor, bool applyGammaCorrection)
            {
                this.padding = padding;
                this.pixelSize = pixelSize;
                this.bgColor = bgColor;
                this.applyGammaCorrection = applyGammaCorrection;
            }
        }
        #endregion

        #region Pack Methods
        /// <summary>
        /// Gets the number of pixels to use for a PixelSize enum value.
        /// </summary>
        /// <param name="pixelSize">PixelSize enum value.</param>
        /// <returns>Number of pixels.</returns>
        public static int GetPixelSize (PixelSize pixelSize) {
            switch (pixelSize) {
                case PixelSize.Px256: return 256;
                case PixelSize.Px512: return 512;
                case PixelSize.Px1024: return 1024;
                case PixelSize.Px2048: return 2048;
                case PixelSize.Px4096: return 4096;
                case PixelSize.Px8192: return 8192;
                default: return 1024;
            }
        }
        public static PixelSize GetPixelSize (int pixelSize) {
            if (pixelSize <= 256) return PixelSize.Px256;
            if (pixelSize <= 512) return PixelSize.Px512;
            if (pixelSize <= 1024) return PixelSize.Px1024;
            if (pixelSize <= 2048) return PixelSize.Px2048;
            if (pixelSize <= 4096) return PixelSize.Px4096;
            if (pixelSize <= 8192) return PixelSize.Px8192;
            else return PixelSize.Px1024;
        }
        public static Rect[] PackTextures (
            out Texture2D dstTexture, 
            Texture2D[] srcTextures,  
            PackMode packMode,
            TexturePackParams packParams)
        {
            Rect[] srcSizes = new Rect[srcTextures.Length];
            int dstPixels = GetPixelSize (packParams.pixelSize);

            if (packMode == PackMode.ScaleEven) {
                // Create Rect for each texture.
                for (int i = 0; i < srcSizes.Length; i++) {
                    if (srcTextures[i] != null) {
                        srcSizes[i] = new Rect (0, 0, srcTextures[i].width, srcTextures[i].height);
                    } else {
                        srcSizes[i] = new Rect ();
                    }
                }
            } else {
                // Calculate target texture size.
                int targetPixelSize = 0;
                float totalSize = 0f;
                for (int i = 0; i < srcTextures.Length; i++) {
                    if (packMode == PackMode.ScaleDown) {
                        if (srcTextures[i].width > srcTextures[i].height) {
                            if (targetPixelSize == 0 || srcTextures[i].width < targetPixelSize) targetPixelSize = srcTextures[i].width;
                        } else {
                            if (targetPixelSize == 0 || srcTextures[i].height < targetPixelSize) targetPixelSize = srcTextures[i].height;
                        }
                    } else if (packMode == PackMode.ScaleUp) {
                        if (srcTextures[i].width > srcTextures[i].height) {
                            if (srcTextures[i].width > targetPixelSize) targetPixelSize = srcTextures[i].width;
                        } else {
                            if (srcTextures[i].height > targetPixelSize) targetPixelSize = srcTextures[i].height;
                        }
                    } else {
                        totalSize += Mathf.Sqrt (srcTextures[i].width^2 + srcTextures[i].height^2);
                    }
                }
                if (packMode == PackMode.ScaleMedian) targetPixelSize = (int)(totalSize / (float)srcTextures.Length);

                // Create Rect for each texture.
                float scaleFactor = 1f;
                for (int i = 0; i < srcSizes.Length; i++) {
                    if (srcTextures[i] != null) {
                        if (srcTextures[i].width > srcTextures[i].height) {
                            scaleFactor = targetPixelSize / (float) srcTextures[i].width;
                        } else {
                            scaleFactor = targetPixelSize / (float) srcTextures[i].height;
                        }
                        srcSizes[i] = new Rect (0, 0, srcTextures[i].width * scaleFactor, srcTextures[i].height * scaleFactor);
                    } else {
                        srcSizes[i] = new Rect ();
                    }
                }
            }

            return PackTextures (out dstTexture, srcTextures, srcSizes, packParams);
        }
        public static Rect[] PackTextures (out Texture2D dstTexture, Texture2D[] srcTextures, Rect[] srcSizes, TexturePackParams packParams) {
            if (srcTextures != null && srcTextures.Length > 0) {
                // Create PackingRects from srcSizes.
                PackingRectangle[] packRects = new PackingRectangle[srcSizes.Length];
                PackingRectangle tmpPackRect;
                for (int i = 0; i < packRects.Length; i++) {
                    tmpPackRect = new PackingRectangle (0, 0, (uint)srcSizes[i].width, (uint)srcSizes[i].height);
                    tmpPackRect.Id = i;
                    packRects[i] = tmpPackRect;
                }

                // Pack packRects.
                PackingRectangle outPackRect;
                RectanglePacker.Pack (packRects, out outPackRect);

                packRects = packRects.OrderBy(rectangle => rectangle.Id).ToArray();

                packingRectangle = outPackRect;
                packingRectangles = packRects;

                // Create scales output rects.
                Rect[] outRects = new Rect[srcSizes.Length];
                Rect tmpRect;
                uint paddingLimit = (uint)GetPixelSize(packParams.pixelSize);
                float maxSize = (outPackRect.Width > outPackRect.Height?outPackRect.Width:outPackRect.Height);
                uint padding = (uint)Mathf.RoundToInt ((float)maxSize*(float)packParams.padding/(float)GetPixelSize(packParams.pixelSize));
                for (int i = 0; i < outRects.Length; i++) {
                    tmpRect = srcSizes[i];
                    tmpPackRect = packRects[i];
                    tmpPackRect.Padding = padding;
                    tmpRect.x = tmpPackRect.PX / maxSize;
                    tmpRect.y = tmpPackRect.PY / maxSize;
                    tmpRect.width = tmpPackRect.GetPWidth((uint)maxSize) / maxSize;
                    tmpRect.height = tmpPackRect.GetPHeight((uint)maxSize) / maxSize;
                    outRects[i] = tmpRect;
                }

                // Create RenderTexture.
                int dstPxWidth = GetPixelSize (packParams.pixelSize);
                int dstPxHeight = dstPxWidth;
                RenderTextureFormat dstFormat = RenderTextureFormat.ARGB32;
                RenderTexture renderTexture = new RenderTexture(dstPxWidth, dstPxHeight, 0, dstFormat, RenderTextureReadWrite.sRGB);
                renderTexture.useMipMap = false;
                renderTexture.anisoLevel = 0;
                renderTexture.Create ();

                // Create blit material.
                Material blitMat = new Material (Shader.Find("Hidden/BroccoUnlitBlit"));

                // Draw bgColor if set.
                if (packParams.bgColor != Color.clear) {
                    RenderTexture.active = renderTexture;
                    GL.Clear(true, true, (packParams.applyGammaCorrection?packParams.bgColor.gamma:packParams.bgColor.linear));
                    RenderTexture.active = null;
                }

                if (packParams.applyGammaCorrection) {
                    blitMat.SetFloat ("_ApplyGammaCorrection", 1f);
                }
                
                // Blit draw source textures to destination texture.
                for (int i = 0; i < srcTextures.Length; i++) {
                    blitMat.SetVector (shaderRectOriginProp, outRects[i].position);
                    blitMat.SetVector (shaderRectSizeProp, outRects[i].size);
                    srcTextures[i].filterMode = FilterMode.Point;
                    srcTextures[i].anisoLevel = 0;
                    Graphics.Blit (srcTextures[i], renderTexture, blitMat);
                }

                dstTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, true, false);
                RenderTexture.active = renderTexture;
                dstTexture.ReadPixels(new Rect(0, 0, dstTexture.width, dstTexture.height), 0, 0);
                RenderTexture.active = null;

                renderTexture.Release ();

                // Return output rects.
                return outRects;
            }
            dstTexture = null;
            return null;
        }
        public static void SaveRenderTextureToPNG(RenderTexture renderTexture, string filePath) {
            // Create a temporary Texture2D to store the pixel data
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

            // Read the pixels from the RenderTexture
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);

            // Encode the Texture2D data to PNG format with transparency
            byte[] bytes = texture2D.EncodeToPNG();

            // Save the bytes to a file
            File.WriteAllBytes(filePath, bytes);

            // Destroy the temporary Texture2D
            MonoBehaviour.DestroyImmediate (texture2D);

            // Set the active RenderTexture back to previous one (optional)
            RenderTexture.active = null;
        }
        public static void SaveTextureToPNG(Texture2D texture, string filePath) {
            // Encode the Texture2D data to PNG format with transparency
            byte[] bytes = texture.EncodeToPNG();
            // Save the bytes to a file
            File.WriteAllBytes(filePath, bytes);
        }
        #endregion
    }
}