using System.IO;
using UnityEditor;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Writes the listing artwork Google Play asks for at its exact required sizes.
    ///
    /// The app icon assigned to the player is 1024 square, which Play rejects: the listing icon
    /// has to be exactly 512. Rather than resample and soften the edges, the mark is redrawn at
    /// the target size from the same geometry the launcher icon uses.
    ///
    /// Output lands next to Screenshots/ at the project root, because these are deliverables for
    /// the store rather than assets the app builds against.
    /// </summary>
    public static class StoreAssetGenerator
    {
        private static readonly Color GradientTop = Hex("#8B6BFF");
        private static readonly Color GradientBottom = Hex("#FF4D8D");
        private static readonly Color DotLight = Hex("#F8F6FF");
        private static readonly Color DotOdd = Hex("#17122A");

        public static string OutputFolder =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "StoreAssets");

        [MenuItem("Party Game/Release/Generate Store Artwork", false, 61)]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);

            var icon = RenderIcon(512);
            Write("store-icon-512.png", icon);

            Debug.Log("[Party Game] Store artwork written to " + OutputFolder +
                      ". The feature graphic comes from the CaptureFeatureGraphic play mode test.");
        }

        /// <summary>Square gradient tile with three dots on it, the last one the odd one out.</summary>
        private static Texture2D RenderIcon(int size)
        {
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                var gradient = Color.Lerp(GradientBottom, GradientTop, y / (float)(size - 1));
                for (var x = 0; x < size; x++) pixels[y * size + x] = gradient;
            }

            const float markScale = 0.62f;
            var markWidth = size * markScale;
            var radius = markWidth / 7.2f;
            var spacing = (markWidth - radius * 2f) / 2f;
            var startX = (size - markWidth) * 0.5f + radius;

            for (var i = 0; i < 3; i++)
                DrawCircle(pixels, size, startX + spacing * i, size * 0.5f, radius, i == 2 ? DotOdd : DotLight);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void DrawCircle(Color[] pixels, int size, float centreX, float centreY, float radius, Color colour)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(centreX - radius - 2f));
            var maxX = Mathf.Min(size - 1, Mathf.CeilToInt(centreX + radius + 2f));
            var minY = Mathf.Max(0, Mathf.FloorToInt(centreY - radius - 2f));
            var maxY = Mathf.Min(size - 1, Mathf.CeilToInt(centreY + radius + 2f));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x + 0.5f - centreX;
                    var dy = y + 0.5f - centreY;
                    var coverage = Mathf.Clamp01(0.5f - (Mathf.Sqrt(dx * dx + dy * dy) - radius));
                    if (coverage <= 0f) continue;

                    var index = y * size + x;
                    pixels[index] = Color.Lerp(pixels[index], colour, coverage);
                }
            }
        }

        private static void Write(string fileName, Texture2D texture)
        {
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            return colour;
        }
    }
}
