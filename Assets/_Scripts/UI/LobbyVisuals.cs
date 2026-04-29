using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural sprite/texture factory for the lobby's UI polish pass.
///
/// Generates rounded-rect 9-slice sprites, vertical gradients, radial glows,
/// vignettes and subtle patterns at runtime so we don't depend on imported art.
/// All sprites are cached (keyed by their parameters) and shared across instances.
///
/// This is intentionally engine-only — no editor APIs — so it's safe in builds.
/// </summary>
public static class LobbyVisuals
{
    private static readonly Dictionary<string, Sprite> _spriteCache = new();
    private static readonly Dictionary<string, Texture2D> _textureCache = new();

    // ─── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// 9-slice rounded rectangle. Corner radius and border width are in pixels of the source texture.
    /// The returned sprite has its border field set so UnityEngine.UI.Image.type=Sliced will tile correctly.
    /// </summary>
    public static Sprite GetRoundedRect(int cornerRadius, int borderThickness, Color fill, Color borderColor)
    {
        cornerRadius = Mathf.Max(2, cornerRadius);
        borderThickness = Mathf.Max(0, borderThickness);
        string key = $"rr_{cornerRadius}_{borderThickness}_{ColorKey(fill)}_{ColorKey(borderColor)}";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        // Texture must be at least 2*cornerRadius + 2 wide so the middle slice has at least 2 px.
        int size = cornerRadius * 2 + 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = SampleRoundedRect(x, y, size, size, cornerRadius, borderThickness, fill, borderColor, clear);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        // Sprite border = the size of the corner regions (in pixels). Image.type=Sliced will preserve them.
        Vector4 border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.name = key;

        _spriteCache[key] = sprite;
        _textureCache[key] = tex;
        return sprite;
    }

    /// <summary>
    /// Vertical linear gradient sprite (top → bottom). Stretches via Image.type=Simple.
    /// </summary>
    public static Sprite GetVerticalGradient(Color top, Color bottom, int height = 256)
    {
        height = Mathf.Max(8, height);
        string key = $"vg_{height}_{ColorKey(top)}_{ColorKey(bottom)}";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        const int width = 4;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float t = 1f - (y / (float)(height - 1)); // top has y high → t=0 means top
            Color c = Color.Lerp(top, bottom, t);
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        _spriteCache[key] = sprite;
        _textureCache[key] = tex;
        return sprite;
    }

    /// <summary>
    /// Radial glow with soft falloff. Center color → transparent edge.
    /// </summary>
    public static Sprite GetRadialGlow(Color center, int size = 256)
    {
        size = Mathf.Max(16, size);
        string key = $"rg_{size}_{ColorKey(center)}";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;
        Color edge = new Color(center.r, center.g, center.b, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxDist;
                d = Mathf.Clamp01(d);
                // Smoother falloff than linear
                float t = Mathf.Pow(d, 1.4f);
                pixels[y * size + x] = Color.Lerp(center, edge, t);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        _spriteCache[key] = sprite;
        _textureCache[key] = tex;
        return sprite;
    }

    /// <summary>
    /// Full-screen vignette: transparent center, dark edges.
    /// </summary>
    public static Sprite GetVignette(Color edge, int size = 512)
    {
        size = Mathf.Max(64, size);
        string key = $"vig_{size}_{ColorKey(edge)}";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;
        Color clear = new Color(edge.r, edge.g, edge.b, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxDist;
                d = Mathf.Clamp01(d);
                // Strong falloff so the vignette sits mostly at the corners
                float t = Mathf.SmoothStep(0.55f, 1.05f, d);
                pixels[y * size + x] = Color.Lerp(clear, edge, t);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        _spriteCache[key] = sprite;
        _textureCache[key] = tex;
        return sprite;
    }

    /// <summary>
    /// Subtle diagonal scan-line pattern. Tiles. Use with Image.type=Tiled and low alpha overlay.
    /// </summary>
    public static Sprite GetSubtlePattern(Color line, int size = 32)
    {
        size = Mathf.Max(8, size);
        string key = $"pat_{size}_{ColorKey(line)}";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[size * size];
        Color clear = new Color(line.r, line.g, line.b, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Diagonal lines every ~6 px
                int diag = (x + y) % 6;
                float a = (diag == 0) ? 1f : 0f;
                pixels[y * size + x] = Color.Lerp(clear, line, a * 0.35f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        _spriteCache[key] = sprite;
        _textureCache[key] = tex;
        return sprite;
    }

    /// <summary>
    /// 1x1 white sprite for solid color images. Cached.
    /// </summary>
    public static Sprite GetWhitePixel()
    {
        const string key = "white1x1";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        _spriteCache[key] = sprite;
        _textureCache[key] = tex;
        return sprite;
    }

    // ─── Internals ─────────────────────────────────────────────────────

    private static Color SampleRoundedRect(int x, int y, int w, int h, int radius, int borderThickness, Color fill, Color borderColor, Color clear)
    {
        // Distance to nearest corner (for the rounded clipping)
        // Pick the relevant corner based on (x,y) quadrant.
        int cx = (x < w * 0.5f) ? radius : (w - 1 - radius);
        int cy = (y < h * 0.5f) ? radius : (h - 1 - radius);

        // If we're inside the inner rectangle (not in corner regions), skip the circle test
        bool inCornerRegion = (x < radius || x > w - 1 - radius) && (y < radius || y > h - 1 - radius);

        float distFromCornerCenter = inCornerRegion
            ? Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy))
            : 0f;

        // Outside the rounded rect → transparent
        if (inCornerRegion && distFromCornerCenter > radius)
            return clear;

        if (borderThickness <= 0)
            return fill;

        // Border test:
        if (inCornerRegion)
        {
            // Border ring at the corners
            return distFromCornerCenter > radius - borderThickness ? borderColor : fill;
        }
        // Straight edges
        bool nearEdge =
            x < borderThickness || x > w - 1 - borderThickness ||
            y < borderThickness || y > h - 1 - borderThickness;
        return nearEdge ? borderColor : fill;
    }

    private static string ColorKey(Color c)
    {
        // Quantize to 1/255 to keep cache keys stable
        return $"{(int)(c.r * 255)},{(int)(c.g * 255)},{(int)(c.b * 255)},{(int)(c.a * 255)}";
    }
}
