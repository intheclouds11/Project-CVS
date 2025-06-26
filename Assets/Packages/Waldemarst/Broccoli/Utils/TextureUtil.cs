using System.Collections;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Required for AssetDatabase and TextureImporter
using System.IO;   // Required for File and Path operations
#endif

namespace Broccoli.Utils
{
	/// <summary>
	/// Texture utility methods.
	/// </summary>
	public class TextureUtil {
		#region Scaling
		/// <summary>
		/// Thread data for scaling.
		/// </summary>
		public class ThreadScaleData
		{
			public int start;
			public int end;
			public ThreadScaleData (int s, int e) {
				start = s;
				end = e;
			}
		}
		/// <summary>
		/// The pixels on the original texture.
		/// </summary>
		private static Color[] texColors;
		/// <summary>
		/// The pixels on the scaled texture.
		/// </summary>
		private static Color[] newColors;
		/// <summary>
		/// Width of the original texture.
		/// </summary>
		private static int w;
		/// <summary>
		/// Width of the scaled texture.
		/// </summary>
		private static int w2;
		/// <summary>
		/// Ratio of the scaling on X axis.
		/// </summary>
		private static float ratioX;
		/// <summary>
		/// Ratio of the scaling on Y axis.
		/// </summary>
		private static float ratioY;
		/// <summary>
		/// The number of cores that finished the scaling.
		/// </summary>
		private static int finishCoresCount;
		/// <summary>
		/// Mutual exclusion control for multithreading.
		/// </summary>
		private static Mutex mutex;
		/// <summary>
		/// Simple scaling using scale to point.
		/// </summary>
		/// <param name="tex">Texture to scale.</param>
		/// <param name="newWidth">New width.</param>
		/// <param name="newHeight">New height.</param>
		public static void PointScale (Texture2D tex, int newWidth, int newHeight)
		{
			ThreadedScale (tex, newWidth, newHeight, false);
		}
		/// <summary>
		/// Bilinear scaling.
		/// </summary>
		/// <param name="tex">Texture to scale.</param>
		/// <param name="newWidth">New width.</param>
		/// <param name="newHeight">New height.</param>
		public static void BilinearScale (Texture2D tex, int newWidth, int newHeight)
		{
			ThreadedScale (tex, newWidth, newHeight, true);
		}
		/// <summary>
		/// Threaded scaling main method.
		/// </summary>
		/// <param name="tex">Texture to scale.</param>
		/// <param name="newWidth">New width.</param>
		/// <param name="newHeight">New height.</param>
		/// <param name="useBilinear">If set to <c>true</c> use bilinear scaling method.</param>
		private static void ThreadedScale (Texture2D tex, int newWidth, int newHeight, bool useBilinear)
		{
			texColors = tex.GetPixels ();
			newColors = new Color[newWidth * newHeight];
			if (useBilinear)
			{
				ratioX = 1.0f / ((float)newWidth / (tex.width-1));
				ratioY = 1.0f / ((float)newHeight / (tex.height-1));
			}
			else {
				ratioX = ((float)tex.width) / newWidth;
				ratioY = ((float)tex.height) / newHeight;
			}
			w = tex.width;
			w2 = newWidth;
			var cores = Mathf.Min (SystemInfo.processorCount, newHeight);
			var slice = newHeight/cores;

			finishCoresCount = 0;
			if (mutex == null) {
				mutex = new Mutex (false);
			}
			if (cores > 1)
			{
				int i = 0;
				ThreadScaleData threadData;
				for (i = 0; i < cores-1; i++) {
					threadData = new ThreadScaleData (slice * i, slice * (i + 1));
					ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart (BilinearScale) : new ParameterizedThreadStart (PointScale);
					Thread thread = new Thread (ts);
					thread.Start (threadData);
				}
				threadData = new ThreadScaleData (slice*i, newHeight);
				if (useBilinear)
				{
					BilinearScale (threadData);
				}
				else
				{
					PointScale (threadData);
				}
				while (finishCoresCount < cores)
				{
					Thread.Sleep(1);
				}
			}
			else
			{
				ThreadScaleData threadData = new ThreadScaleData (0, newHeight);
				if (useBilinear)
				{
					BilinearScale (threadData);
				}
				else
				{
					PointScale (threadData);
				}
			}

			#if UNITY_2021_2_OR_NEWER
			tex.Reinitialize (newWidth, newHeight);
			#else
			tex.Resize (newWidth, newHeight);
			#endif
			tex.SetPixels (newColors);
			tex.Apply ();

			texColors = null;
			newColors = null;
		}
		/// <summary>
		/// Bilinear scaling method.
		/// </summary>
		/// <param name="obj">ThreadScaleData object.</param>
		public static void BilinearScale (System.Object obj)
		{
			ThreadScaleData threadData = (ThreadScaleData) obj;
			for (var y = threadData.start; y < threadData.end; y++)
			{
				int yFloor = (int)Mathf.Floor(y * ratioY);
				var y1 = yFloor * w;
				var y2 = (yFloor+1) * w;
				var yw = y * w2;

				for (var x = 0; x < w2; x++) {
					int xFloor = (int) Mathf.Floor (x * ratioX);
					var xLerp = x * ratioX-xFloor;
					newColors[yw + x] = ColorLerpUnclamped (ColorLerpUnclamped (texColors[y1 + xFloor], texColors[y1 + xFloor+1], xLerp),
						ColorLerpUnclamped (texColors[y2 + xFloor], texColors[y2 + xFloor+1], xLerp),
						y*ratioY-yFloor);
				}
			}

			mutex.WaitOne();
			finishCoresCount++;
			mutex.ReleaseMutex();
		}
		/// <summary>
		/// Point scaling method.
		/// </summary>
		/// <param name="obj">ThreadScaleData object.</param>
		public static void PointScale (System.Object obj)
		{
			ThreadScaleData threadData = (ThreadScaleData) obj;
			for (var y = threadData.start; y < threadData.end; y++)
			{
				var thisY = (int)(ratioY * y) * w;
				var yw = y * w2;
				for (var x = 0; x < w2; x++) {
					newColors[yw + x] = texColors[(int)(thisY + ratioX*x)];
				}
			}

			mutex.WaitOne();
			finishCoresCount++;
			mutex.ReleaseMutex();
		}
		/// <summary>
		/// Lerp unclamped for colors.
		/// </summary>
		/// <returns>The lerp unclamped.</returns>
		/// <param name="c1">Color one.</param>
		/// <param name="c2">Color two.</param>
		/// <param name="value">Value of clamping.</param>
		private static Color ColorLerpUnclamped (Color c1, Color c2, float value)
		{
			return new Color (c1.r + (c2.r - c1.r) * value, 
				c1.g + (c2.g - c1.g) * value, 
				c1.b + (c2.b - c1.b) * value, 
				c1.a + (c2.a - c1.a) * value);
		}
		#endregion

		#region Colors
		/// <summary>
		/// Creates a plain color texture.
		/// </summary>
		/// <returns>Texture.</returns>
		/// <param name="pxSize">Pixels per side.</param>
		/// <param name="col">Color of the texture.</param>
		public static Texture2D ColorToTex (int pxSize, Color col) 
		{
			Color[] texCols = new Color[pxSize*pxSize];
			for (int px = 0; px < pxSize*pxSize; px++) 
				texCols[px] = col;
			Texture2D tex = new Texture2D (pxSize, pxSize);
			tex.SetPixels (texCols);
			tex.Apply ();
			return tex;
		}
		/// <summary>
		/// Applies a tinture to a texture.
		/// </summary>
		/// <param name="tex">Texture.</param>
		/// <param name="color">Color of the tinture.</param>
		public static Texture2D Tint (Texture2D tex, Color color) 
		{
			Texture2D tintedTex = UnityEngine.Object.Instantiate (tex);
			for (int x = 0; x < tex.width; x++) 
				for (int y = 0; y < tex.height; y++) 
					tintedTex.SetPixel (x, y, tex.GetPixel (x, y) * color);
			tintedTex.Apply ();
			return tintedTex;
		}
		#endregion

		#region Crop
		/// <summary>
		/// Crops a texture using 0 to 1 coordinates.
		/// </summary>
		/// <returns>The resulting texture.</returns>
		/// <param name="tex">Texture to crop.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public static Texture2D CropTextureRelative (Texture2D tex, float x, float y, float width, float height) {
			x = Mathf.Clamp01 (x);
			y = Mathf.Clamp01 (y);
			if (x + width > 1)
				width = 1f - x;
			if (y + height > 1)
				height = 1f - y;
			int xMin = Mathf.FloorToInt (x * tex.width);
			int xMax = xMin + Mathf.CeilToInt (width * tex.width);
			int yMin = Mathf.FloorToInt (y * tex.height);
			int yMax = yMin + Mathf.CeilToInt (height * tex.height);
			return CropTexture (tex, xMin, yMin, xMax - xMin, yMax - yMin);
		}
		/// <summary>
		/// Crops a texture using pixel coordinates.
		/// </summary>
		/// <returns>The resulting texture.</returns>
		/// <param name="tex">Texture to crop.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public static Texture2D CropTexture (Texture2D tex, int x, int y, int width, int height) {
			if (tex == null)
				return null;
			x = Mathf.Clamp (x, 0, tex.width);
			y = Mathf.Clamp (y, 0, tex.height);
			if (x + width > tex.width)
				width = tex.width - x;
			else if (x + width < 1)
				width = 1;
			if (y + height > tex.height)
				height = tex.height - y;
			else if (y + height < 1)
				height = 1;
			Texture2D cropTex = new Texture2D (width, height);
			Color[] origCol = tex.GetPixels ();
			Color[] cropCol = new Color[width * height];
			int origPos = y * tex.width;
			int cropPos = 0;
			for (int j = 0; j < height; j++) {
				origPos += x;
				for (int i = 0; i < width; i++) {
					cropCol [cropPos] = origCol [origPos];
					origPos++;
					cropPos++;
				}
				origPos += tex.width - x - width;
			}
			cropTex.SetPixels (cropCol);
			cropTex.Apply (true, false);
			return cropTex;
		}
		#endregion

		#region Rotation
		/// <summary>
		/// Rotates a texture clockwise.
		/// </summary>
		/// <returns>The resulting texture.</returns>
		/// <param name="tex">Texture to rotate.</param>
		/// <param name="quarterSteps">Steps (90) clockwise.</param>
		public static Texture2D RotateTextureCW (Texture2D tex, int quarterSteps) {
			if (tex == null)
				return null;
			quarterSteps = quarterSteps % 4;
			if (quarterSteps == 0) {
				return tex;
			} else if (quarterSteps > 0) { 
				Color[] origCol = tex.GetPixels ();
				Color[] rotCol = new Color[tex.height * tex.width];
				int pos = 0;
				if (quarterSteps == 1) {
					Texture2D rotatedTex = new Texture2D (tex.height, tex.width);
					for (int j = tex.width - 1; j >= 0; j--) {
						for (int i = 0; i < tex.height; i++) {
							rotCol [pos] = origCol [(i * tex.width) + j];
							pos++;
						}
					}
					rotatedTex.SetPixels (rotCol);
					tex = rotatedTex;
				} else if (quarterSteps == 2) {
					for (int j = tex.height - 1; j >= 0; j--) {
						for (int i = tex.width - 1; i >= 0; i--) {
							rotCol [pos] = origCol [(j * tex.width) + i];
							pos++;
						}
					}
					tex.SetPixels (rotCol);
				} else {
					Texture2D rotatedTex = new Texture2D (tex.height, tex.width);
					for (int j = 0; j < tex.width; j++) {
						for (int i = tex.height - 1; i >= 0; i--) {
							rotCol [pos] = origCol [(i * tex.width) + j];
							pos++;
						}
					}
					rotatedTex.SetPixels (rotCol);
					tex = rotatedTex;
				}
				tex.Apply ();
			}
			return tex;
		}
		/// <summary>
		/// Rotates a texture counter clockwise.
		/// </summary>
		/// <returns>The resulting texture.</returns>
		/// <param name="tex">Texture to rotate.</param>
		/// <param name="quarterSteps">Steps (90) counter clockwise.</param>
		public static Texture2D RotateTextureCCW (Texture2D tex, int quarterSteps) 
		{
			quarterSteps = quarterSteps % 4;
			return RotateTextureCW (tex, 4 - quarterSteps);
		}
		#endregion

		#region File
		/// <summary>
        /// Saves a Texture2D to a PNG file and optionally imports/configures it as an asset
        /// with settings appropriate for sharp, non-aliased pixels (Point filter, no compression/mipmaps).
        /// </summary>
        /// <param name="texture">The Texture2D to save.</param>
        /// <param name="filename">The full path (including Assets/...) and filename (e.g., "Assets/Textures/output.png").</param>
        /// <param name="configureAsset">If true, configures the imported asset for Point filtering.</param>
        /// <returns>The Texture2D asset reference if configureAsset is true, otherwise null (or consider returning the input texture).</returns>
        public static Texture2D SaveTextureToFile (Texture2D texture, string filename, bool configureAsset = false)
        {
            #if UNITY_EDITOR
            if (texture == null)
            {
                Debug.LogError("SaveTextureToFile: Input texture is null.");
                return null;
            }
            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogError("SaveTextureToFile: Filename is null or empty.");
                return null;
            }

            // Ensure filename has .png extension
            if (!filename.ToLower().EndsWith(".png"))
            {
                filename += ".png";
                Debug.LogWarning("Filename did not end with .png, appending it.");
            }

            // Ensure the directory exists
            try
            {
                string directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveTextureToFile: Error creating directory for {filename}: {ex.Message}");
                return null;
            }

            // Encode texture to PNG (lossless, preserves sharp pixels)
            byte[] bytes = texture.EncodeToPNG();

            try
            {
                // Write the file to disk
                File.WriteAllBytes(filename, bytes);
                Debug.Log($"Texture saved to file: {filename}");

                if (configureAsset)
                {
                    // Tell Unity to notice the new/changed file
                    AssetDatabase.ImportAsset(filename);

                    // Get the TextureImporter for the asset path
                    TextureImporter textureImporter = AssetImporter.GetAtPath(filename) as TextureImporter;

                    if (textureImporter != null)
                    {
                        bool settingsChanged = false;

                        // --- Configure Importer Settings for Sharp Pixels ---

                        // 1. Set Filter Mode to Point (No Bilinear/Trilinear Smoothing)
                        if (textureImporter.filterMode != FilterMode.Point)
                        {
                            textureImporter.filterMode = FilterMode.Point;
                            settingsChanged = true;
                        }

                        // 2. Disable MipMaps (Often not needed for this type of generated texture)
                        if (textureImporter.mipmapEnabled)
                        {
                            textureImporter.mipmapEnabled = false;
                            settingsChanged = true;
                        }

                        // 3. Ensure No Compression (Compression can cause artifacts)
                        if (textureImporter.textureCompression != TextureImporterCompression.Uncompressed)
                        {
                            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                            settingsChanged = true;
                        }

                        // Optional: Keep it readable from script if needed later? (Costs memory)
                        // if (!textureImporter.isReadable) {
                        //     textureImporter.isReadable = true;
                        //     settingsChanged = true;
                        // }

                        // --- Apply Changes ---
                        if (settingsChanged)
                        {
                            // Save the changes to the importer settings and force reimport
                            textureImporter.SaveAndReimport();
                            Debug.Log($"Applied Point filter (No AA) and reimported: {filename}");
                        }

                        // Load and return the asset reference with the updated settings
                        return AssetDatabase.LoadAssetAtPath<Texture2D>(filename);
                    }
                    else
                    {
                        Debug.LogError($"SaveTextureToFile: Could not get TextureImporter for {filename}. Asset might not be in 'Assets' folder or import failed.");
                        return null; // Failed to configure
                    }
                }
                else
                {
                    // If not configuring the asset, maybe just signal success? Or return input?
                    // Returning null if not configured, as the function implies configuration.
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveTextureToFile: Error writing file or processing asset {filename}: {ex.Message}");
                return null;
            }
            #else
            return null;
            #endif
        }
		/// <summary>
		/// Sets a texture Asset as a normal map.
		/// </summary>
		/// <param name="texture">Texture2D asset instance.</param>
		/// <param name="texturePath">Path to texture asset.</param>
        public static void SetTextureAsNormalMap (Texture2D texture, string texturePath) {
            #if UNITY_EDITOR
            UnityEditor.TextureImporter importer = (UnityEditor.TextureImporter)UnityEditor.TextureImporter.GetAtPath (texturePath);
            importer.textureType = UnityEditor.TextureImporterType.NormalMap;
            importer.SaveAndReimport ();
            #endif
        }
		/// <summary>
		/// Removes textures from memory.
		/// </summary>
		/// <param name="texturesToClean">List of texture assets.</param>
        public static void CleanTextures (List<Texture2D> texturesToClean) {
            for (int i = 0; i < texturesToClean.Count; i++) {
                UnityEngine.Object.DestroyImmediate (texturesToClean [i]);
            }
            texturesToClean.Clear ();
        }
		#endregion
	}
}