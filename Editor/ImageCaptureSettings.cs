using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TY.ImageAssistant
{
    internal enum ImageImportMode { Icon, Unchanged, Thumbnail }
    internal enum ImageShape { Rectangle, Circle, Rounded, Custom }
    internal enum CompositionGrid { None, Grid3x3, Grid4x4, GoldenRatio, Diagonal, CenterCross }
    internal enum ImageBackground { Solid, Skybox }

    [Serializable]
    internal sealed class ImageCaptureSettings
    {
        public int Width = 256;
        public int Height = 256;
        public int Supersampling = 2;
        public ImageImportMode ImportMode;
        public bool Transparent = true;
        public bool HideOthers = true;
        public bool DropShadow;

        public ImageCaptureSettings Copy() => JsonUtility.FromJson<ImageCaptureSettings>(JsonUtility.ToJson(this));

        public string Validate(int hardwareLimit)
        {
            if (Width < 1 || Height < 1 || Width > 16384 || Height > 16384)
                return "出力解像度は1〜16384pxで指定してください。";
            if (Supersampling < 1 || Supersampling > 8) return "撮影倍率は1〜8xで指定してください。";
            if ((long)Width * Supersampling > hardwareLimit || (long)Height * Supersampling > hardwareLimit)
                return ImageLocalization.Format("内部撮影サイズ {0}×{1} が、このPCの上限 {2}px を超えています。", (long)Width * Supersampling, (long)Height * Supersampling, hardwareLimit);
            return null;
        }
    }

    [Serializable]
    internal sealed class ImagePreset
    {
        public string Name;
        public ImageCaptureSettings Settings;

        public ImagePreset(string name, ImageCaptureSettings settings) { Name = name; Settings = settings; }
    }

    internal static class ImagePresets
    {
        public static readonly string[] BuiltInNames = { "アイコン (既定)", "素材", "サムネイル", "カスタム" };

        public static ImageCaptureSettings Create(int index)
        {
            switch (index)
            {
                case 0: return new ImageCaptureSettings { DropShadow = true };
                case 1: return new ImageCaptureSettings { Width = 4096, Height = 4096, ImportMode = ImageImportMode.Unchanged };
                case 2: return new ImageCaptureSettings { Width = 1200, Height = 900, ImportMode = ImageImportMode.Thumbnail, Transparent = false, HideOthers = false };
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        [Serializable]
        private sealed class PresetList { public List<ImagePreset> Items = new List<ImagePreset>(); }

        private static string Key => "TY.ImageAssistant.Presets.v1." + Application.dataPath;

        public static List<ImagePreset> Load()
        {
            try { return JsonUtility.FromJson<PresetList>(EditorPrefs.GetString(Key, "{}"))?.Items ?? new List<ImagePreset>(); }
            catch (ArgumentException) { return new List<ImagePreset>(); }
        }

        public static void Save(List<ImagePreset> presets) => EditorPrefs.SetString(Key, JsonUtility.ToJson(new PresetList { Items = presets }));
    }

    internal sealed class ImageToolOptions
    {
        public bool PrefixEnabled = true;
        public string Prefix = "IA_";
        public bool Numbering;
        public bool ForceFolder;
        public string ForcedFolder = "";
    }
}
