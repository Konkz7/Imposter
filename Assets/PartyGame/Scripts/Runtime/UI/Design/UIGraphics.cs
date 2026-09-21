using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.UI.Design
{
    /// <summary>
    /// Generates the sprites the UI needs (rounded rectangles, rings, circles, gradients)
    /// at runtime. The app therefore ships with a consistent look and no imported art,
    /// and every shape is a 9-sliced sprite so it scales without distortion.
    /// </summary>
    public static class UIGraphics
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Solid rounded rectangle, 9-sliced at the corner radius.</summary>
        public static Sprite RoundedRect(int radius)
        {
            return Build("rr-" + radius, radius, (distance, _) => Coverage(distance));
        }

        /// <summary>Rounded rectangle outline of the given thickness.</summary>
        public static Sprite RoundedOutline(int radius, int thickness)
        {
            return Build("ro-" + radius + "-" + thickness, radius, (distance, _) =>
            {
                var outer = Coverage(distance);
                var inner = Coverage(distance + thickness);
                return Mathf.Clamp01(outer - inner);
            });
        }

        public static Sprite Circle(int radius)
        {
            var key = "circle-" + radius;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var size = Mathf.Max(8, radius * 2);
            var texture = NewTexture(size, size);
            var centre = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) - (size * 0.5f - 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Coverage(distance)));
                }
            }
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Vertical two-stop gradient used for the app background.</summary>
        public static Sprite VerticalGradient(Color top, Color bottom, int height = 256)
        {
            var key = "grad-" + ColorUtility.ToHtmlStringRGBA(top) + "-" + ColorUtility.ToHtmlStringRGBA(bottom) + "-" + height;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var texture = NewTexture(1, height);
            for (var y = 0; y < height; y++)
                texture.SetPixel(0, y, Color.Lerp(bottom, top, y / (float)(height - 1)));
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(0, 1, 0, 1));
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Build(string key, int radius, System.Func<float, Vector2Int, float> alphaAt)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            radius = Mathf.Max(2, radius);
            var size = radius * 2 + 4;
            var texture = NewTexture(size, size);

            var inner = new Rect(radius, radius, size - radius * 2, size - radius * 2);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = RoundedDistance(x + 0.5f, y + 0.5f, inner, radius);
                    var alpha = Mathf.Clamp01(alphaAt(distance, new Vector2Int(x, y)));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            var border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Signed distance to the edge of a rounded rectangle. Negative means inside.</summary>
        private static float RoundedDistance(float x, float y, Rect inner, float radius)
        {
            var dx = Mathf.Max(inner.xMin - x, 0f, x - inner.xMax);
            var dy = Mathf.Max(inner.yMin - y, 0f, y - inner.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }

        /// <summary>One pixel of anti-aliasing either side of the edge.</summary>
        private static float Coverage(float distance)
        {
            return Mathf.Clamp01(0.5f - distance);
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            return texture;
        }
    }
}
