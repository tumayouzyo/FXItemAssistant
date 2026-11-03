using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TY.ImageAssistant
{
    internal static class ImageAssetWriter
    {
        public static string FindDefaultFolder(Renderer renderer)
        {
            if (!renderer || renderer.sharedMaterials.Length == 0) return "";
            var material = renderer.sharedMaterials[0];
            if (!material) return "";
            Texture texture = null;
            foreach (var property in new[] { "_BaseMap", "_BaseColorMap", "_MainTex" })
            {
                if (!material.HasProperty(property)) continue;
                texture = material.GetTexture(property);
                if (texture) break;
            }
            if (!texture) texture = material.mainTexture;
            var path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)) return "";
            return Path.GetDirectoryName(path)?.Replace('\\', '/') + "/_icon";
        }

        public static string FromAbsoluteFolder(string path)
        {
            var assets = Path.GetFullPath(Application.dataPath).TrimEnd('\\', '/');
            var absolute = Path.GetFullPath(path).TrimEnd('\\', '/');
            if (string.Equals(assets, absolute, StringComparison.OrdinalIgnoreCase)) return "Assets";
            if (!absolute.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("保存先は、このプロジェクトのAssets内を指定してください。");
            var result = "Assets/" + absolute.Substring(assets.Length + 1).Replace('\\', '/');
            ValidateFolder(result);
            return result;
        }

        public static void ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)))
                throw new ArgumentException("保存先を指定してください。このプロジェクトのAssets内へ保存できます。");
            foreach (var part in folder.Split('/')) ValidateName(part);
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.EndsWith(".") || name.EndsWith(" ")
                || name.IndexOfAny("<>:\"/\\|?*".ToCharArray()) >= 0 || name.Any(char.IsControl))
                throw new ArgumentException("保存名・フォルダ名に使用できない文字が含まれています。");
            var stem = name.Split('.')[0].ToUpperInvariant();
            if (new[] { "CON", "PRN", "AUX", "NUL" }.Contains(stem)
                || (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] >= '1' && stem[3] <= '9'))
                throw new ArgumentException("Windowsの予約名は保存名・フォルダ名に使用できません。");
        }

        public static string ResolvePath(string folder, string name, ImageToolOptions options)
        {
            ValidateFolder(folder);
            name = (name ?? "").Trim();
            if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
            ValidateName(name);
            name = (options.PrefixEnabled ? options.Prefix : "") + name;
            ValidateName(name);
            if (name.Length > 160) throw new ArgumentException("保存名は接頭辞を含めて160文字以内で指定してください。");
            var path = folder + "/" + name + ".png";
            if (!options.Numbering) return path;
            for (var i = 1; File.Exists(path); i++) path = $"{folder}/{name}_{i:000}.png";
            return path;
        }

        public static void Save(string path, Texture2D image, ImageCaptureSettings settings)
        {
            if (!image) throw new ArgumentNullException(nameof(image));
            var directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            ValidateFolder(directory);
            var png = image.EncodeToPNG();
            if (png == null || png.Length == 0) throw new IOException("PNGを生成できませんでした。");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new IOException("保存画像のインポーターを取得できませんでした。");
            ApplyImportSettings(importer, settings);
            importer.SaveAndReimport();
        }

        internal static void ApplyImportSettings(TextureImporter importer, ImageCaptureSettings settings)
        {
            var maxSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(settings.Width, settings.Height)), 32, 16384);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = settings.Transparent;
            importer.mipmapEnabled = settings.ImportMode == ImageImportMode.Standard;
            importer.streamingMipmaps = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = settings.ImportMode == ImageImportMode.Standard ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.anisoLevel = settings.ImportMode == ImageImportMode.Standard ? 2 : 0;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.crunchedCompression = false;
            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.maxTextureSize = maxSize;
            defaults.format = TextureImporterFormat.Automatic;
            defaults.textureCompression = TextureImporterCompression.CompressedHQ;
            defaults.compressionQuality = 100;
            defaults.crunchedCompression = false;
            importer.SetPlatformTextureSettings(defaults);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone", overridden = true, maxTextureSize = maxSize,
                format = TextureImporterFormat.BC7, compressionQuality = 100, crunchedCompression = false
            });
            foreach (var platform in new[] { "Android", "iPhone" })
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform, overridden = true, maxTextureSize = maxSize,
                    format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100, crunchedCompression = false
                });
        }
    }
}
