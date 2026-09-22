using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Draws the app icon in code and assigns it to every platform slot.
    ///
    /// The mark is three dots with one odd one out, which says what the app is without needing a
    /// font or an imported illustration - so it renders identically at 48px and 1024px, and the
    /// whole icon set can be regenerated from a colour change.
    /// </summary>
    public static class AppIconGenerator
    {
        private const string IconFolder = "Assets/PartyGame/Art/Icons";
        private const string OpaquePath = IconFolder + "/app-icon.png";
        private const string ForegroundPath = IconFolder + "/app-icon-foreground.png";
        private const string BackgroundPath = IconFolder + "/app-icon-background.png";

        private const int Size = 1024;

        // Deliberately the brand accents rather than the app's dark surface: an icon has to hold
        // its own in a launcher full of other icons.
        private static readonly Color GradientTop = Hex("#8B6BFF");
        private static readonly Color GradientBottom = Hex("#FF4D8D");
        private static readonly Color DotLight = Hex("#F8F6FF");
        private static readonly Color DotOdd = Hex("#17122A");

        [MenuItem("Party Game/Release/Generate App Icons", false, 60)]
        public static void GenerateAndAssign()
        {
            Directory.CreateDirectory(IconFolder);

            WritePng(OpaquePath, RenderIcon(withBackground: true, markScale: 0.62f));
            WritePng(ForegroundPath, RenderIcon(withBackground: false, markScale: 0.40f));
            WritePng(BackgroundPath, RenderBackgroundOnly());

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureImporter(OpaquePath, hasAlpha: false);
            ConfigureImporter(ForegroundPath, hasAlpha: true);
            ConfigureImporter(BackgroundPath, hasAlpha: false);

            var opaque = AssetDatabase.LoadAssetAtPath<Texture2D>(OpaquePath);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);

            AssignTo(NamedBuildTarget.Android, opaque, foreground, background);
            AssignTo(NamedBuildTarget.iOS, opaque, foreground, background);
            AssignTo(NamedBuildTarget.Standalone, opaque, foreground, background);

            AssetDatabase.SaveAssets();
            Debug.Log("[Party Game] App icons generated and assigned for Android, iOS and Standalone.");
        }

        /// <summary>
        /// Fills every icon slot the platform declares. Layered kinds (Android adaptive icons)
        /// get the background and foreground separately; everything else gets the flat icon.
        /// </summary>
        private static void AssignTo(NamedBuildTarget target, Texture2D opaque, Texture2D foreground, Texture2D background)
        {
            PlatformIconKind[] kinds;
            try
            {
                kinds = PlayerSettings.GetSupportedIconKinds(target);
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[Party Game] Skipping icons for " + target.TargetName + ": " + error.Message);
                return;
            }

            foreach (var kind in kinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(target, kind);
                if (icons == null || icons.Length == 0) continue;

                foreach (var icon in icons)
                {
                    if (icon.maxLayerCount >= 2 && background != null && foreground != null)
                        icon.SetTextures(background, foreground);
                    else
                        icon.SetTextures(opaque);
                }

                PlayerSettings.SetPlatformIcons(target, kind, icons);
            }
        }

        // ------------------------------------------------------------------ drawing

        private static Texture2D RenderIcon(bool withBackground, float markScale)
        {
            var pixels = new Color[Size * Size];

            for (var y = 0; y < Size; y++)
            {
                var vertical = y / (float)(Size - 1);
                var gradient = Color.Lerp(GradientBottom, GradientTop, vertical);

                for (var x = 0; x < Size; x++)
                    pixels[y * Size + x] = withBackground ? gradient : Color.clear;
            }

            DrawMark(pixels, markScale);
            return BuildTexture(pixels);
        }

        private static Texture2D RenderBackgroundOnly()
        {
            var pixels = new Color[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                var gradient = Color.Lerp(GradientBottom, GradientTop, y / (float)(Size - 1));
                for (var x = 0; x < Size; x++) pixels[y * Size + x] = gradient;
            }
            return BuildTexture(pixels);
        }

        /// <summary>Three dots on a row. The last one is the odd one out.</summary>
        private static void DrawMark(Color[] pixels, float markScale)
        {
            var markWidth = Size * markScale;
            var radius = markWidth / 7.2f;
            var spacing = (markWidth - radius * 2f) / 2f;
            var startX = (Size - markWidth) * 0.5f + radius;
            var centreY = Size * 0.5f;

            for (var i = 0; i < 3; i++)
            {
                var centreX = startX + spacing * i;
                var colour = i == 2 ? DotOdd : DotLight;
                DrawCircle(pixels, centreX, centreY, radius, colour);
            }
        }

        private static void DrawCircle(Color[] pixels, float centreX, float centreY, float radius, Color colour)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(centreX - radius - 2f));
            var maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(centreX + radius + 2f));
            var minY = Mathf.Max(0, Mathf.FloorToInt(centreY - radius - 2f));
            var maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(centreY + radius + 2f));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x + 0.5f - centreX;
                    var dy = y + 0.5f - centreY;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                    // One pixel of coverage either side of the edge keeps the dots smooth.
                    var coverage = Mathf.Clamp01(0.5f - distance);
                    if (coverage <= 0f) continue;

                    var index = y * Size + x;
                    var source = pixels[index];
                    var alpha = colour.a * coverage;
                    var outAlpha = alpha + source.a * (1f - alpha);
                    if (outAlpha <= 0.0001f)
                    {
                        pixels[index] = Color.clear;
                        continue;
                    }

                    var blended = (new Vector3(colour.r, colour.g, colour.b) * alpha +
                                   new Vector3(source.r, source.g, source.b) * source.a * (1f - alpha)) / outAlpha;
                    pixels[index] = new Color(blended.x, blended.y, blended.z, outAlpha);
                }
            }
        }

        private static Texture2D BuildTexture(Color[] pixels)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void WritePng(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void ConfigureImporter(string path, bool hasAlpha)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = hasAlpha;
            importer.alphaSource = hasAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var colour) ? colour : Color.magenta;
        }
    }
}
