using System.IO;
using UnityEditor;
using UnityEngine;

// Import rules for everything under Assets/Art: pixel-art stays crisp, pivots and scale are set by file name.
public class ArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;

        var ti = (TextureImporter)assetImporter;
        string name = Path.GetFileNameWithoutExtension(assetPath);

        float ppu = 16f;
        SpriteAlignment pivot = SpriteAlignment.Center;
        FilterMode filter = FilterMode.Point;
        bool border = false;

        if (assetPath.StartsWith("Assets/Art/UI/"))
        {
            ppu = 100f;
            // stone panels / buttons are pixel art, round touch controls are smooth
            bool pixelUi = name == "panel" || name == "button" || name == "btn_pause";
            filter = pixelUi ? FilterMode.Point : FilterMode.Bilinear;
            border = name == "panel" || name == "button";
        }
        else if (name.StartsWith("hero_"))
        {
            pivot = SpriteAlignment.BottomCenter;
        }
        else if (name.StartsWith("snake_"))
        {
            ppu = 44f;
            pivot = SpriteAlignment.BottomCenter;
        }
        else if (name == "boss")
        {
            ppu = 100f;
            pivot = SpriteAlignment.BottomCenter;
        }
        else if (name.StartsWith("treasure_"))
        {
            pivot = SpriteAlignment.BottomCenter;
        }
        else if (name == "glow")
        {
            filter = FilterMode.Bilinear;
        }
        else if (name == "bg_sky")
        {
            filter = FilterMode.Bilinear;
        }
        else if (name == "bg_far" || name == "bg_near")
        {
            filter = FilterMode.Bilinear;
            pivot = SpriteAlignment.BottomCenter;
        }
        else if (name.StartsWith("portal_"))
        {
            pivot = SpriteAlignment.BottomCenter;
        }

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.npotScale = TextureImporterNPOTScale.None;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spritePixelsPerUnit = ppu;
        s.filterMode = filter;
        s.spriteAlignment = (int)pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        if (border) s.spriteBorder = new Vector4(24, 24, 24, 24);
        ti.SetTextureSettings(s);
    }
}
