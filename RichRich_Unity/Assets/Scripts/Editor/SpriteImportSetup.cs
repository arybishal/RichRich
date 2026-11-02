using UnityEditor;
using UnityEngine;

namespace RichRich.Editor
{
    /// <summary>
    /// Configures automatic TextureImporter settings for RichRich UI sprites and icons.
    /// Sets Sprite mode and 16px 9-slice borders on 9slice PNGs.
    /// </summary>
    public class SpriteImportSetup : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Contains("Assets/Sprites/"))
            {
                var importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                if (assetPath.Contains("9slice"))
                {
                    importer.spriteBorder = new Vector4(16, 16, 16, 16);
                }
            }
        }

        [MenuItem("RichRich/Reimport UI Sprites")]
        public static void ReimportAllSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log($"[SpriteImportSetup] Reimported {guids.Length} UI sprites.");
        }
    }
}
