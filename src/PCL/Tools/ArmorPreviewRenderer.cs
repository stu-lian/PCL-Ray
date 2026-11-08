using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PCL.Tools;

internal sealed class ArmorTexture
{
    public int Width;
    public int Height;
    public uint[] Pixels;

    public uint Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return 0u;
        return Pixels[y * Width + x];
    }
}

internal sealed class ArmorPieceOptions
{
    public bool Enabled;
    public string Pattern = "sentry";
    public string Material = "gold";
}

internal static class ArmorPreviewRenderer
{
    // One Minecraft pixel = Scale screen pixels.
    private const int Scale = 6;

    // Front view of the player model: 8 wide head, 8 wide body, 12 tall legs.
    private const int ModelWidth = 16;
    private const int ModelHeight = 32;
    private const int CanvasWidth = ModelWidth * Scale;
    private const int CanvasHeight = ModelHeight * Scale;

    private const int HeadX = 4, HeadY = 0;
    private const int BodyX = 4, BodyY = 8;
    private const int ArmLeftX = 0, ArmRightX = 12;
    private const int LegLeftX = 4, LegRightX = 8;
    private const int LegY = 20;

    private const uint Background = 0xFF202326;
    private const uint Mannequin = 0xFF9EA3A8;

    // Leather sheets are grey; the game multiplies them by the undyed leather colour.
    private const int LeatherR = 160, LeatherG = 101, LeatherB = 64;

    private static readonly Dictionary<string, ArmorTexture?> Cache = new();

    private static readonly Dictionary<string, string> ArmorFiles = new()
    {
        { "leather", "leather" }, { "golden", "gold" }, { "chainmail", "chainmail" },
        { "iron", "iron" }, { "diamond", "diamond" }, { "netherite", "netherite" },
        { "copper", "copper" }
    };

    // Front faces of the vanilla humanoid armour sheet. Each slot only paints the
    // blocks that belong to it, so one piece never drags another piece's pixels along.
    private static readonly Block[] HeadBlocks =
    {
        new(8, 8, 8, 8, HeadX, HeadY, false)
    };

    private static readonly Block[] BodyBlocks =
    {
        new(20, 20, 8, 12, BodyX, BodyY, false),
        new(44, 20, 4, 12, ArmLeftX, BodyY, false),
        new(44, 20, 4, 12, ArmRightX, BodyY, true)
    };

    private static readonly Block[] WaistBlocks =
    {
        new(20, 20, 8, 12, BodyX, BodyY, false)
    };

    private static readonly Block[] LegBlocks =
    {
        new(4, 20, 4, 12, LegLeftX, LegY, false),
        new(4, 20, 4, 12, LegRightX, LegY, true)
    };

    private readonly record struct Block(int U, int V, int W, int H, int X, int Y, bool Mirror);

    public static void Render(WriteableBitmap target, string armorKey, ArmorPieceOptions helmet,
        ArmorPieceOptions chest, ArmorPieceOptions leggings, ArmorPieceOptions boots)
    {
        uint[] frame = new uint[CanvasWidth * CanvasHeight];
        for (int i = 0; i < frame.Length; i++) frame[i] = Background;

        string file = ArmorFiles.TryGetValue(armorKey, out string? name) ? name : "iron";
        bool isLeather = file == "leather";
        ArmorTexture? bodySheet = LoadArmor(file, false);
        ArmorTexture? legsSheet = LoadArmor(file, true);
        ArmorTexture? bodyOverlay = isLeather ? LoadEmbedded("armor_body_leather_overlay") : null;
        uint fallback = FallbackArmorColor(armorKey);

        // Grey mannequin: no face, no skin tint.
        DrawRect(frame, HeadX, HeadY, 8, 8, Mannequin);
        DrawRect(frame, BodyX, BodyY, 8, 12, Mannequin);
        DrawRect(frame, ArmLeftX, BodyY, 4, 12, Mannequin);
        DrawRect(frame, ArmRightX, BodyY, 4, 12, Mannequin);
        DrawRect(frame, LegLeftX, LegY, 4, 12, Mannequin);
        DrawRect(frame, LegRightX, LegY, 4, 12, Mannequin);

        // Vanilla draw order: leggings, boots, chestplate, helmet.
        if (leggings.Enabled)
        {
            DrawSlot(frame, legsSheet, WaistBlocks, leggings, true, isLeather, fallback, armorKey);
            DrawSlot(frame, legsSheet, LegBlocks, leggings, true, isLeather, fallback, armorKey);
        }
        if (boots.Enabled)
        {
            DrawSlot(frame, bodySheet, LegBlocks, boots, false, isLeather, fallback, armorKey);
        }
        if (chest.Enabled)
        {
            // Vanilla order: chest sheet, leather dye overlay, then the trim on top.
            DrawArmor(frame, bodySheet, BodyBlocks, fallback, isLeather);
            if (bodyOverlay != null)
            {
                foreach (Block block in BodyBlocks) Blit(frame, bodyOverlay, block, false);
            }
            DrawTrim(frame, chest, false, BodyBlocks, armorKey);
        }
        if (helmet.Enabled)
        {
            DrawSlot(frame, bodySheet, HeadBlocks, helmet, false, isLeather, fallback, armorKey);
        }

        target.WritePixels(new Int32Rect(0, 0, CanvasWidth, CanvasHeight), frame, CanvasWidth * 4, 0);
    }

    private static void DrawSlot(uint[] frame, ArmorTexture? sheet, Block[] blocks, ArmorPieceOptions options,
        bool legsTrim, bool tint, uint fallback, string armorKey)
    {
        DrawArmor(frame, sheet, blocks, fallback, tint);
        DrawTrim(frame, options, legsTrim, blocks, armorKey);
    }

    private static void DrawArmor(uint[] frame, ArmorTexture? sheet, Block[] blocks, uint fallback, bool tint)
    {
        foreach (Block block in blocks)
        {
            if (sheet == null)
            {
                DrawRect(frame, block.X, block.Y, block.W, block.H, fallback);
            }
            else
            {
                Blit(frame, sheet, block, tint);
            }
        }
    }

    private static void DrawTrim(uint[] frame, ArmorPieceOptions options, bool legsTrim, Block[] blocks, string armorKey)
    {
        if (string.IsNullOrEmpty(options.Pattern) || string.IsNullOrEmpty(options.Material)) return;
        ArmorTexture? trim = LoadTrim(options.Pattern, legsTrim);
        if (trim == null) return;
        uint[] palette = LoadPalette(PaletteName(options.Material, armorKey));
        if (palette.Length == 0) return;
        foreach (Block block in blocks) BlitTrim(frame, trim, palette, block);
    }

    // Vanilla swaps to the *_darker palette when the trim material matches the armour.
    private static string PaletteName(string material, string armorKey)
    {
        string file = ArmorFiles.TryGetValue(armorKey, out string? name) ? name : "iron";
        return string.Equals(material, file, StringComparison.OrdinalIgnoreCase) ? material + "_darker" : material;
    }

    private static void Blit(uint[] frame, ArmorTexture texture, Block block, bool tint)
    {
        for (int row = 0; row < block.H; row++)
        {
            for (int col = 0; col < block.W; col++)
            {
                int sourceU = block.Mirror ? block.U + block.W - 1 - col : block.U + col;
                uint pixel = texture.Get(sourceU, block.V + row);
                if ((pixel >> 24) < 8) continue;
                if (tint) pixel = TintLeather(pixel);
                FillPixel(frame, block.X + col, block.Y + row, pixel);
            }
        }
    }

    private static void BlitTrim(uint[] frame, ArmorTexture trim, uint[] palette, Block block)
    {
        for (int row = 0; row < block.H; row++)
        {
            for (int col = 0; col < block.W; col++)
            {
                int sourceU = block.Mirror ? block.U + block.W - 1 - col : block.U + col;
                uint pixel = trim.Get(sourceU, block.V + row);
                if ((pixel >> 24) < 8) continue;
                FillPixel(frame, block.X + col, block.Y + row, TrimColor(pixel, palette));
            }
        }
    }

    private static uint TintLeather(uint pixel)
    {
        uint a = pixel & 0xFF000000;
        uint r = ((pixel >> 16) & 0xFF) * (uint)LeatherR / 255;
        uint g = ((pixel >> 8) & 0xFF) * (uint)LeatherG / 255;
        uint b = (pixel & 0xFF) * (uint)LeatherB / 255;
        return a | r << 16 | g << 8 | b;
    }

    private static void DrawRect(uint[] frame, int x, int y, int w, int h, uint color)
    {
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++) FillPixel(frame, x + col, y + row, color);
        }
    }

    private static void FillPixel(uint[] frame, int x, int y, uint color)
    {
        if (x < 0 || y < 0 || x >= ModelWidth || y >= ModelHeight) return;
        int left = x * Scale;
        int top = y * Scale;
        for (int dy = 0; dy < Scale; dy++)
        {
            int row = (top + dy) * CanvasWidth;
            for (int dx = 0; dx < Scale; dx++) frame[row + left + dx] = color;
        }
    }

    private static uint TrimColor(uint trimPixel, uint[] palette)
    {
        uint[] ramp = TrimRamp();
        uint rgb = trimPixel & 0xFFFFFF;
        for (int i = 0; i < ramp.Length && i < palette.Length; i++)
        {
            if (ramp[i] == rgb) return (trimPixel & 0xFF000000) | (palette[i] & 0xFFFFFF);
        }
        return trimPixel;
    }

    private static uint[] TrimRamp()
    {
        if (_trimRamp != null) return _trimRamp;
        ArmorTexture? texture = LoadEmbedded("palette_trim_palette")
            ?? LoadFromJar("assets/minecraft/textures/trims/color_palettes/trim_palette.png");
        if (texture == null || texture.Width == 0)
        {
            _trimRamp = Array.Empty<uint>();
            return _trimRamp;
        }
        uint[] ramp = new uint[texture.Width];
        for (int i = 0; i < ramp.Length; i++) ramp[i] = texture.Get(i, 0) & 0xFFFFFF;
        _trimRamp = ramp;
        return ramp;
    }

    private static uint[]? _trimRamp;

    private static ArmorTexture? LoadArmor(string name, bool leggings)
    {
        string key = (leggings ? "armor_legs_" : "armor_body_") + name;
        if (Cache.TryGetValue(key, out ArmorTexture? cached)) return cached;
        ArmorTexture? result = LoadEmbedded(key)
            ?? LoadFromJar("assets/minecraft/textures/entity/equipment/" + (leggings ? "humanoid_leggings/" : "humanoid/") + name + ".png");
        Cache[key] = result;
        return result;
    }

    private static ArmorTexture? LoadTrim(string pattern, bool leggings)
    {
        string key = (leggings ? "trim_legs_" : "trim_body_") + pattern;
        if (Cache.TryGetValue(key, out ArmorTexture? cached)) return cached;
        ArmorTexture? result = LoadEmbedded(key)
            ?? LoadFromJar("assets/minecraft/textures/trims/entity/" + (leggings ? "humanoid_leggings/" : "humanoid/") + pattern + ".png");
        Cache[key] = result;
        return result;
    }

    private static uint[] LoadPalette(string material)
    {
        string key = "palette_" + material;
        if (Cache.TryGetValue(key, out ArmorTexture? cachedTexture) && cachedTexture != null)
        {
            uint[] colors = new uint[cachedTexture.Width];
            for (int i = 0; i < colors.Length; i++) colors[i] = cachedTexture.Get(i, 0);
            return colors;
        }
        ArmorTexture? texture = LoadEmbedded(key)
            ?? LoadFromJar("assets/minecraft/textures/trims/color_palettes/" + material + ".png");
        Cache[key] = texture;
        if (texture == null) return Array.Empty<uint>();
        uint[] result = new uint[texture.Width];
        for (int i = 0; i < result.Length; i++) result[i] = texture.Get(i, 0);
        return result;
    }

    private static ArmorTexture? LoadEmbedded(string name)
    {
        try
        {
            System.Reflection.Assembly assembly = typeof(ArmorPreviewRenderer).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream("PCL.Tools.ArmorAssets." + name + ".png");
            if (stream == null) return null;
            return DecodePng(stream);
        }
        catch
        {
            return null;
        }
    }

    private static ArmorTexture? LoadFromJar(string entryName)
    {
        string? jar = FindJar();
        if (jar == null) return null;
        try
        {
            using System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(jar);
            System.IO.Compression.ZipArchiveEntry? entry = archive.GetEntry(entryName);
            if (entry == null) return null;
            using Stream stream = entry.Open();
            using MemoryStream memory = new();
            stream.CopyTo(memory);
            memory.Position = 0;
            return DecodePng(memory);
        }
        catch
        {
            return null;
        }
    }

    private static ArmorTexture? DecodePng(Stream stream)
    {
        try
        {
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return ToTexture(image);
        }
        catch
        {
            return null;
        }
    }

    private static ArmorTexture ToTexture(BitmapSource image)
    {
        BitmapSource source = image.Format == PixelFormats.Bgra32
            ? image
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int stride = source.PixelWidth * 4;
        byte[] data = new byte[stride * source.PixelHeight];
        source.CopyPixels(data, stride, 0);
        uint[] pixels = new uint[source.PixelWidth * source.PixelHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte b = data[i * 4];
            byte g = data[i * 4 + 1];
            byte r = data[i * 4 + 2];
            byte a = data[i * 4 + 3];
            pixels[i] = (uint)(a << 24 | r << 16 | g << 8 | b);
        }
        return new ArmorTexture { Width = source.PixelWidth, Height = source.PixelHeight, Pixels = pixels };
    }

    private static string? FindJar()
    {
        if (_jarSearched) return _jarPath;
        _jarSearched = true;

        List<string> folders = new();
        try
        {
            if (!string.IsNullOrEmpty(ModFolder.mcFolderSelected)) folders.Add(ModFolder.mcFolderSelected);
        }
        catch
        {
        }
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        if (!folders.Contains(appData)) folders.Add(appData);

        foreach (string folder in folders)
        {
            string versions = Path.Combine(folder, "versions");
            if (!Directory.Exists(versions)) continue;
            List<string> jars = new();
            foreach (string directory in Directory.GetDirectories(versions))
            {
                string name = Path.GetFileName(directory);
                foreach (string jar in Directory.GetFiles(directory, "*.jar"))
                {
                    if (Path.GetFileNameWithoutExtension(jar) == name) jars.Add(jar);
                }
            }
            jars.Sort((a, b) => string.CompareOrdinal(Path.GetFileName(b), Path.GetFileName(a)));
            foreach (string jar in jars)
            {
                try
                {
                    using System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(jar);
                    if (archive.GetEntry("assets/minecraft/textures/entity/equipment/humanoid/iron.png") != null)
                    {
                        _jarPath = jar;
                        return _jarPath;
                    }
                }
                catch
                {
                }
            }
        }
        return null;
    }

    private static string? _jarPath;
    private static bool _jarSearched;

    private static uint FallbackArmorColor(string armorKey)
    {
        return armorKey switch
        {
            "leather" => 0xFFA06540,
            "golden" => 0xFFF6D84B,
            "chainmail" => 0xFF9A9A9A,
            "diamond" => 0xFF4AEDD9,
            "netherite" => 0xFF4A4247,
            "copper" => 0xFFE07A5F,
            _ => 0xFFD8D8D8
        };
    }
}