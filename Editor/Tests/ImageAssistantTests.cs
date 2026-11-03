using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TY.ImageAssistant.Tests
{
    internal sealed class ImageAssistantTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Scene fixtureScene;
        private SceneView view;
        private GameObject root;
        private Material material;
        private Vector3 oldPivot;
        private Quaternion oldRotation;
        private float oldSize;
        private float oldFov;
        private bool oldOrtho;

        [SetUp]
        public void SetUp()
        {
            fixtureScene = EditorSceneManager.NewPreviewScene();
            root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(root, fixtureScene);
            root.name = "Capture fixture";
            material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.3f, 0.6f, 0.9f, 1) };
            root.GetComponent<Renderer>().sharedMaterial = material;
            view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            oldPivot = view.pivot;
            oldRotation = view.rotation;
            oldSize = view.size;
            oldFov = view.cameraSettings.fieldOfView;
            oldOrtho = view.orthographic;
            view.camera.transform.SetPositionAndRotation(new Vector3(0, 0, -8), Quaternion.identity);
            view.camera.orthographic = false;
            view.camera.fieldOfView = 30;
            view.camera.nearClipPlane = 0.01f;
            view.camera.farClipPlane = 100;
        }

        [TearDown]
        public void TearDown()
        {
            if (material) Object.DestroyImmediate(material);
            if (fixtureScene.IsValid()) EditorSceneManager.ClosePreviewScene(fixtureScene);
            if (view)
            {
                view.cameraSettings.fieldOfView = oldFov;
                view.orthographic = oldOrtho;
                view.LookAtDirect(oldPivot, oldRotation, oldSize);
                view.Repaint();
            }
        }

        [TestCase(0, 256, 256, true, true, ImageImportMode.Icon)]
        [TestCase(1, 4096, 4096, true, true, ImageImportMode.Standard)]
        [TestCase(2, 1200, 900, false, false, ImageImportMode.Thumbnail)]
        public void BuiltInPresetContract(int preset, int width, int height, bool transparent, bool hideOthers, ImageImportMode mode)
        {
            var settings = ImagePresets.Create(preset);
            Assert.That((settings.Width, settings.Height, settings.Supersampling), Is.EqualTo((width, height, 2)));
            Assert.That((settings.Transparent, settings.HideOthers, settings.ImportMode), Is.EqualTo((transparent, hideOthers, mode)));
        }

        [Test]
        public void HardwareLimitRejectsOversizeWithoutChangingValues()
        {
            var settings = ImagePresets.Create(1);
            settings.Supersampling = 8;
            Assert.That(settings.Validate(16384), Is.Not.Null);
            Assert.That(settings.Supersampling, Is.EqualTo(8));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(8)]
        public void TransparentCaptureHasExactOutputSizeCoverageAndColor(int factor)
        {
            root.transform.rotation = Quaternion.Euler(0, 0, 13);
            using (var preview = new ItemPreviewScene(root, true))
            {
                var image = preview.Capture(view, new ImageCaptureSettings { Width = 96, Height = 64, Supersampling = factor });
                try
                {
                    Assert.That((image.width, image.height), Is.EqualTo((96, 64)));
                    Assert.That(image.GetPixel(0, 0).a, Is.LessThan(0.01f));
                    var center = image.GetPixel(48, 32);
                    Assert.That(center.a, Is.GreaterThan(0.99f));
                    Assert.That(center.r, Is.EqualTo(material.color.r).Within(0.03));
                    Assert.That(center.g, Is.EqualTo(material.color.g).Within(0.03));
                    Assert.That(center.b, Is.EqualTo(material.color.b).Within(0.03));
                    if (factor > 1) Assert.That(image.GetPixels().Any(p => p.a > 0 && p.a < 1), Is.True);
                }
                finally { Object.DestroyImmediate(image); }
            }
        }

        [Test]
        public void SolidCaptureIsOpaqueAndUsesBackgroundColor()
        {
            using (var preview = new ItemPreviewScene(root, true) { BackgroundColor = Color.red })
            {
                var image = preview.Capture(view, new ImageCaptureSettings { Width = 64, Height = 64, Transparent = false });
                try
                {
                    var corner = image.GetPixel(0, 0);
                    Assert.That(corner.a, Is.EqualTo(1));
                    Assert.That(corner.r, Is.GreaterThan(0.99));
                    Assert.That(corner.g, Is.LessThan(0.01));
                }
                finally { Object.DestroyImmediate(image); }
            }
        }

        [Test]
        public void InactiveRootIsPreviewedWithoutChangingSource()
        {
            root.SetActive(false);
            using (var preview = new ItemPreviewScene(root, true))
            {
                Assert.That(preview.Bounds().size.x, Is.EqualTo(1).Within(0.01));
                Assert.That(root.activeSelf, Is.False);
            }
            Assert.That(root.activeSelf, Is.False);
        }

        [Test]
        public void HideOthersIncludesOnlySelectedSceneAndPreservesSourceVisibility()
        {
            var sibling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(sibling, fixtureScene);
            using (var isolated = new ItemPreviewScene(root, true)) Assert.That(isolated.SurfaceCount, Is.EqualTo(1));
            using (var wholeScene = new ItemPreviewScene(root, false)) Assert.That(wholeScene.SurfaceCount, Is.EqualTo(2));
            Assert.That(sibling.activeSelf, Is.True);
            Assert.That(root.GetComponent<Renderer>().enabled, Is.True);
        }

        [Test]
        public void InspectorTransformChangeIsReflectedInBounds()
        {
            using (var preview = new ItemPreviewScene(root, true))
            {
                root.transform.position = new Vector3(2, 3, 4);
                Assert.That(preview.Sync(), Is.True);
                Assert.That(preview.Bounds().center, Is.EqualTo(root.transform.position));
                Assert.That(preview.Sync(), Is.False);
            }
        }

        [Test]
        public void ExternalBonePoseIsBakedWithoutCopyingAvatarBody()
        {
            Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(root.GetComponent<MeshFilter>());
            var bone = new GameObject("External bone");
            SceneManager.MoveGameObjectToScene(bone, fixtureScene);
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0) },
                triangles = new[] { 0, 1, 2 },
                bindposes = new[] { Matrix4x4.identity },
                boneWeights = Enumerable.Repeat(new BoneWeight { boneIndex0 = 0, weight0 = 1 }, 3).ToArray()
            };
            var skin = root.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = mesh;
            skin.bones = new[] { bone.transform };
            skin.rootBone = bone.transform;
            skin.sharedMaterial = material;
            try
            {
                using (var preview = new ItemPreviewScene(root, true))
                {
                    var before = preview.Bounds().center;
                    bone.transform.position = Vector3.right * 2;
                    Assert.That(preview.Sync(), Is.True);
                    Assert.That(preview.Bounds().center.x - before.x, Is.EqualTo(2).Within(0.01));
                    Assert.That(preview.SurfaceCount, Is.EqualTo(1));
                    var rendered = preview.Camera.scene.GetRootGameObjects()
                        .SelectMany(go => go.GetComponentsInChildren<MeshRenderer>(true)).Single();
                    Assert.That(rendered.localBounds, Is.EqualTo(rendered.GetComponent<MeshFilter>().sharedMesh.bounds),
                        "Frustum culling must use the latest baked mesh bounds.");
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void PngImportPreservesNpotDimensionsAndHighQualityCompression()
        {
            var folder = "Assets/__TYImageAssistantTest_" + Guid.NewGuid().ToString("N");
            var path = folder + "/sample.png";
            var image = new Texture2D(120, 90, TextureFormat.RGBA32, false);
            try
            {
                var settings = new ImageCaptureSettings { Width = 120, Height = 90, ImportMode = ImageImportMode.Standard };
                ImageAssetWriter.Save(path, image, settings);
                var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That((imported.width, imported.height), Is.EqualTo((120, 90)));
                Assert.That(importer.mipmapEnabled, Is.True);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
                Assert.That(importer.isReadable, Is.False);
                Assert.That(importer.GetPlatformTextureSettings("Standalone").format, Is.EqualTo(TextureImporterFormat.BC7));
                Assert.That(importer.GetPlatformTextureSettings("Android").format, Is.EqualTo(TextureImporterFormat.ASTC_4x4));
                var numbered = ImageAssetWriter.ResolvePath(folder, "sample", new ImageToolOptions { Numbering = true });
                Assert.That(numbered, Does.EndWith("sample_001.png"));
                var guid = AssetDatabase.AssetPathToGUID(path);
                ImageAssetWriter.Save(path, image, settings);
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            }
            finally
            {
                Object.DestroyImmediate(image);
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [TestCase("Assets/../outside")]
        [TestCase("Packages/test")]
        [TestCase("Assets/CON")]
        public void InvalidOutputFolderIsRejected(string folder) => Assert.Throws<ArgumentException>(() => ImageAssetWriter.ValidateFolder(folder));

        [Test]
        public void ManualCustomSelectionPreservesAllSettingValues()
        {
            var window = ScriptableObject.CreateInstance<ImageAssistantWindow>();
            try
            {
                window.CreateGUI();
                var settingsField = typeof(ImageAssistantWindow).GetField("settings", Private);
                var settings = (ImageCaptureSettings)settingsField.GetValue(window);
                settings.Width = 777;
                typeof(ImageAssistantWindow).GetMethod("ApplyPreset", Private).Invoke(window, new object[] { "カスタム" });
                Assert.That(settingsField.GetValue(window), Is.SameAs(settings));
                Assert.That(settings.Width, Is.EqualTo(777));
                window.rootVisualElement.Q<IntegerField>("heightField").value = 321;
                Assert.That(window.rootVisualElement.Q<DropdownField>("presetField").value, Is.EqualTo("カスタム"));
            }
            finally { Object.DestroyImmediate(window); }
        }
    }
}
