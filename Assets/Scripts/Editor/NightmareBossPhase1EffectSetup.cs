#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NightmareBossPhase1EffectSetup
{
    private const string Source = "Assets/Sprites/phase1.png";
    private const string Output = "Assets/Resources/NightmareBoss/Phase1Effects";

    private readonly struct EffectFrame
    {
        public readonly string name;
        public readonly RectInt rect;
        public EffectFrame(string name, int x, int y, int width, int height)
        { this.name = name; rect = new RectInt(x, y, width, height); }
    }

    private static readonly EffectFrame[] Frames =
    {
        new("Orb_01", 544, 803, 94, 91),
        new("Orb_02", 636, 808, 55, 65),
        new("Orb_03", 686, 808, 55, 63),
        new("Orb_04", 738, 808, 55, 60),
        new("Orb_05", 790, 808, 55, 58),
        new("CrimsonSlash", 837, 790, 180, 94),
        new("SoulSkull_01", 532, 896, 54, 58),
        new("SoulSkull_02", 675, 866, 55, 62),
        new("SoulSkull_03", 790, 878, 58, 62),
        new("SoulSkull_04", 898, 882, 58, 62),
        new("ShadowBurstLarge", 1068, 795, 165, 185),
        new("ShadowPortalLarge", 1225, 800, 170, 105),
        new("ShadowBurstSmall", 1255, 895, 125, 115),
        new("ShadowPortalSmall", 1385, 910, 145, 90)
    };

    [MenuItem("Tools/2D Dungeon/Extract Nightmare Phase 1 Effects")]
    public static void Extract()
    {
        TextureImporter importer = AssetImporter.GetAtPath(Source) as TextureImporter;
        if (importer == null) { Debug.LogError("Nightmare phase 1 source missing."); return; }
        bool wasReadable = importer.isReadable;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        EnsureFolders();
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        foreach (EffectFrame frame in Frames) Write(source, frame);
        AssetDatabase.Refresh();
        foreach (EffectFrame frame in Frames) Configure($"{Output}/{frame.name}.png");
        importer = AssetImporter.GetAtPath(Source) as TextureImporter;
        importer.isReadable = wasReadable;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log($"NIGHTMARE_PHASE1_EFFECTS_PASS: {Frames.Length} effects extracted.");
    }

    private static void Write(Texture2D source, EffectFrame frame)
    {
        int width = frame.rect.width, height = frame.rect.height;
        Color32[] crop = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int sourceX = frame.rect.x + x;
            int sourceY = source.height - frame.rect.y - frame.rect.height + y;
            crop[y * width + x] = source.GetPixel(sourceX, sourceY);
        }

        Color32[] keyed = new Color32[crop.Length];
        int minX = width, maxX = -1, minY = height, maxY = -1;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color actual = crop[y * width + x];
            float brightest = actual.maxColorComponent;
            float darkest = Mathf.Min(actual.r, Mathf.Min(actual.g, actual.b));
            float chroma = brightest - darkest;
            float alpha;
            if (frame.name.StartsWith("Shadow"))
            {
                float purpleSignal = Mathf.Max(actual.r, actual.b) - actual.g;
                alpha = Mathf.InverseLerp(0.2f, 0.62f, brightest) *
                        Mathf.InverseLerp(0.08f, 0.32f, purpleSignal);
            }
            else
            {
                float redSignal = actual.r - Mathf.Max(actual.g, actual.b);
                float redAlpha = Mathf.InverseLerp(0.25f, 0.82f, actual.r) *
                                 Mathf.InverseLerp(0.08f, 0.38f, redSignal);
                float neutralAlpha = frame.name.StartsWith("SoulSkull")
                    ? Mathf.InverseLerp(0.32f, 0.72f, brightest) * (1f - Mathf.InverseLerp(0.13f, 0.34f, chroma))
                    : 0f;
                alpha = Mathf.Max(redAlpha, neutralAlpha);
            }
            alpha = Mathf.SmoothStep(0f, 1f, alpha);
            Color result = new(actual.r, actual.g, actual.b, alpha);
            keyed[y * width + x] = result;
            if (alpha > 0.08f) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        }
        if (maxX < minX || maxY < minY) return;

        const int size = 256;
        int contentWidth = maxX - minX + 1, contentHeight = maxY - minY + 1;
        float scale = Mathf.Min(236f / contentWidth, 236f / contentHeight);
        int drawWidth = Mathf.RoundToInt(contentWidth * scale), drawHeight = Mathf.RoundToInt(contentHeight * scale);
        int offsetX = (size - drawWidth) / 2, offsetY = (size - drawHeight) / 2;
        Color32[] outputPixels = new Color32[size * size];
        for (int y = 0; y < drawHeight; y++)
        for (int x = 0; x < drawWidth; x++)
        {
            int sourceX = minX + Mathf.Clamp(Mathf.FloorToInt(x / scale), 0, contentWidth - 1);
            int sourceY = minY + Mathf.Clamp(Mathf.FloorToInt(y / scale), 0, contentHeight - 1);
            outputPixels[(offsetY + y) * size + offsetX + x] = keyed[sourceY * width + sourceX];
        }
        Texture2D output = new(size, size, TextureFormat.RGBA32, false);
        output.SetPixels32(outputPixels); output.Apply();
        File.WriteAllBytes($"{Output}/{frame.name}.png", output.EncodeToPNG());
        Object.DestroyImmediate(output);
    }

    private static void Configure(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/NightmareBoss/Phase1Effects"))
            AssetDatabase.CreateFolder("Assets/Resources/NightmareBoss", "Phase1Effects");
    }
}
#endif
