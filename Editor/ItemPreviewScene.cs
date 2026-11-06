using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TY.ImageAssistant
{
    internal sealed class ItemPreviewScene : IDisposable
    {
        private sealed class Surface
        {
            public Renderer Source;
            public MeshRenderer Copy;
            public MeshFilter Filter;
            public Mesh Baked;
            public int Signature;
        }

        private readonly PreviewRenderUtility utility = new PreviewRenderUtility();
        private readonly List<Surface> surfaces = new List<Surface>();
        private readonly List<KeyValuePair<Light, Light>> sceneLights = new List<KeyValuePair<Light, Light>>();
        private readonly Dictionary<Transform, Transform> poseTransforms = new Dictionary<Transform, Transform>();
        private readonly List<GameObject> poseRoots = new List<GameObject>();
        private readonly GameObject target;
        private readonly bool hideOthers;
        private double nextTopologyCheck;
        private int topologySignature;
        private Material resolveMaterial;
        private RenderTexture display;
        public bool FixedLighting { get; set; }
        public float FocalLength { get; set; } = 85;
        public CompositionGrid Grid { get; set; }
        public bool SafeArea { get; set; }
        public ImageShape Shape { get; set; }
        public Texture2D ShapeMask { get; set; }
        public bool DropShadow { get; set; }
        public ImageBackground Background { get; set; }
        public Color BackgroundColor { get; set; } = new Color(0.18f, 0.18f, 0.18f, 1f);
        public Camera Camera => utility.camera;
        public int SurfaceCount => surfaces.Count;

        public ItemPreviewScene(GameObject target, bool hideOthers)
        {
            this.target = target;
            this.hideOthers = hideOthers;
            try
            {
                Camera.allowHDR = false;
                Camera.allowMSAA = false;
                Camera.cullingMask = ~0;
                Camera.gameObject.AddComponent<Skybox>();
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(ImageAssistantWindow.AssetRoot + "/Editor/ImageResolve.shader");
                if (!shader || !shader.isSupported) throw new InvalidOperationException("撮影用シェーダーが利用できません。");
                resolveMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                Rebuild();
            }
            catch { Dispose(); throw; }
        }

        private Renderer[] Sources()
        {
            if (!target) return Array.Empty<Renderer>();
            var roots = hideOthers ? new[] { target } : target.scene.GetRootGameObjects();
            return roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(r => r is SkinnedMeshRenderer || (r is MeshRenderer && r.GetComponent<MeshFilter>())).ToArray();
        }

        private static Light[] DirectionalLights() => Resources.FindObjectsOfTypeAll<Light>()
            .Where(light => light.gameObject.scene.IsValid() && light.gameObject.scene.isLoaded
                && !EditorSceneManager.IsPreviewScene(light.gameObject.scene) && !EditorUtility.IsPersistent(light)
                && light.type == LightType.Directional).ToArray();

        private static int TopologyHash(IEnumerable<Renderer> renderers, IEnumerable<Light> lights)
        {
            unchecked
            {
                var hash = 17;
                foreach (var renderer in renderers)
                {
                    hash = hash * 31 + renderer.GetInstanceID();
                    for (var parent = renderer.transform.parent; parent; parent = parent.parent) hash = hash * 31 + parent.GetInstanceID();
                }
                foreach (var light in lights) hash = hash * 31 + light.GetInstanceID();
                return hash;
            }
        }

        private void Rebuild()
        {
            ClearCopies();
            var renderers = Sources();
            var lights = DirectionalLights();
            topologySignature = TopologyHash(renderers, lights);
            foreach (var source in renderers)
            {
                var go = new GameObject(source.name + " Preview") { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(CopyPoseTransform(source.transform), false);
                var surface = new Surface { Source = source, Filter = go.AddComponent<MeshFilter>(), Copy = go.AddComponent<MeshRenderer>() };
                surface.Copy.lightProbeUsage = LightProbeUsage.Off;
                surface.Copy.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (source is SkinnedMeshRenderer)
                {
                    surface.Baked = new Mesh { name = source.name + " Baked", hideFlags = HideFlags.HideAndDontSave };
                    surface.Baked.MarkDynamic();
                    surface.Filter.sharedMesh = surface.Baked;
                }
                surfaces.Add(surface);
            }
            foreach (var light in lights)
            {
                var go = new GameObject("Directional Preview") { hideFlags = HideFlags.HideAndDontSave };
                utility.AddSingleGO(go);
                sceneLights.Add(new KeyValuePair<Light, Light>(light, go.AddComponent<Light>()));
            }
            Sync(true);
        }

        private Transform CopyPoseTransform(Transform source)
        {
            if (!source) return null;
            if (poseTransforms.TryGetValue(source, out var existing)) return existing;
            var parent = CopyPoseTransform(source.parent);
            var go = new GameObject(source.name + " Pose") { hideFlags = HideFlags.HideAndDontSave };
            if (parent) go.transform.SetParent(parent, false);
            else { utility.AddSingleGO(go); poseRoots.Add(go); }
            go.transform.localPosition = source.localPosition;
            go.transform.localRotation = source.localRotation;
            go.transform.localScale = source.localScale;
            poseTransforms.Add(source, go.transform);
            return go.transform;
        }

        private void SyncPoseTransforms()
        {
            foreach (var pair in poseTransforms)
            {
                if (!pair.Key || !pair.Value) continue;
                pair.Value.localPosition = pair.Key.localPosition;
                pair.Value.localRotation = pair.Key.localRotation;
                pair.Value.localScale = pair.Key.localScale;
            }
        }

        private bool Visible(Renderer source)
        {
            if (!source.enabled || source.forceRenderingOff) return false;
            if (!hideOthers && !source.transform.IsChildOf(target.transform)) return source.gameObject.activeInHierarchy;
            for (var t = source.transform; t && t != target.transform; t = t.parent)
                if (!t.gameObject.activeSelf) return false;
            return true;
        }

        private static int RendererHash(Renderer source)
        {
            unchecked
            {
                var hash = source.localToWorldMatrix.GetHashCode() * 31 + EditorUtility.GetDirtyCount(source);
                hash = hash * 31 + source.gameObject.layer;
                hash = hash * 31 + source.enabled.GetHashCode();
                hash = hash * 31 + source.forceRenderingOff.GetHashCode();
                for (var t = source.transform; t; t = t.parent) hash = hash * 31 + t.gameObject.activeSelf.GetHashCode();
                foreach (var material in source.sharedMaterials)
                    hash = hash * 31 + (material ? material.GetInstanceID() ^ EditorUtility.GetDirtyCount(material) : 0);
                if (source is SkinnedMeshRenderer skin && skin.sharedMesh)
                {
                    hash = hash * 31 + skin.sharedMesh.GetInstanceID() + EditorUtility.GetDirtyCount(skin.sharedMesh);
                    for (var i = 0; i < skin.sharedMesh.blendShapeCount; i++) hash = hash * 31 + skin.GetBlendShapeWeight(i).GetHashCode();
                    foreach (var bone in skin.bones) if (bone) hash = hash * 31 + bone.GetInstanceID() + bone.localToWorldMatrix.GetHashCode();
                }
                else if (source.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh)
                    hash = hash * 31 + filter.sharedMesh.GetInstanceID() + EditorUtility.GetDirtyCount(filter.sharedMesh);
                return hash;
            }
        }

        public bool Sync(bool force = false)
        {
            if (!target) return false;
            if (!force && EditorApplication.timeSinceStartup >= nextTopologyCheck)
            {
                nextTopologyCheck = EditorApplication.timeSinceStartup + 0.5;
                if (topologySignature != TopologyHash(Sources(), DirectionalLights())) { Rebuild(); return true; }
            }
            var changed = force;
            SyncPoseTransforms();
            foreach (var surface in surfaces)
            {
                var source = surface.Source;
                if (!source) { surface.Copy.enabled = false; changed = true; continue; }
                var signature = RendererHash(source);
                if (!force && signature == surface.Signature && !EditorApplication.isPlaying) continue;
                surface.Signature = signature;
                changed = true;
                var copy = surface.Copy;
                copy.gameObject.layer = source.gameObject.layer;
                copy.sharedMaterials = source.sharedMaterials;
                copy.enabled = Visible(source);
                copy.shadowCastingMode = source.shadowCastingMode;
                copy.receiveShadows = source.receiveShadows;
                if (source is SkinnedMeshRenderer skin)
                {
                    surface.Baked.Clear();
                    if (skin.sharedMesh) skin.BakeMesh(surface.Baked);
                }
                else surface.Filter.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                // In-place BakeMesh updates can leave MeshRenderer's culling bounds cached.
                copy.localBounds = surface.Filter.sharedMesh ? surface.Filter.sharedMesh.bounds : new Bounds();
                var properties = new MaterialPropertyBlock();
                source.GetPropertyBlock(properties);
                copy.SetPropertyBlock(properties);
                for (var i = 0; i < source.sharedMaterials.Length; i++)
                {
                    properties.Clear();
                    source.GetPropertyBlock(properties, i);
                    copy.SetPropertyBlock(properties, i);
                }
            }
            foreach (var pair in sceneLights)
            {
                if (!pair.Key) { pair.Value.enabled = false; continue; }
                var source = pair.Key;
                var copy = pair.Value;
                var enabled = !FixedLighting && source.isActiveAndEnabled;
                changed |= copy.enabled != enabled || copy.intensity != source.intensity || copy.color != source.color
                    || copy.transform.rotation != source.transform.rotation || copy.cullingMask != source.cullingMask
                    || copy.useColorTemperature != source.useColorTemperature || copy.colorTemperature != source.colorTemperature;
                copy.enabled = enabled;
                copy.type = LightType.Directional;
                copy.intensity = source.intensity;
                copy.color = source.color;
                copy.useColorTemperature = source.useColorTemperature;
                copy.colorTemperature = source.colorTemperature;
                copy.cullingMask = source.cullingMask;
                copy.transform.rotation = source.transform.rotation;
                copy.shadows = source.shadows;
            }
            return changed;
        }

        public Bounds Bounds()
        {
            var found = false;
            var bounds = new Bounds(target ? target.transform.position : Vector3.zero, Vector3.one * 0.1f);
            foreach (var surface in surfaces)
            {
                if (!surface.Source || !surface.Source.transform.IsChildOf(target.transform) || !surface.Copy.enabled) continue;
                var mesh = surface.Filter.sharedMesh;
                if (!mesh || mesh.vertexCount == 0) continue;
                var local = mesh.bounds;
                var matrix = surface.Copy.localToWorldMatrix;
                var x = matrix.MultiplyVector(new Vector3(local.extents.x, 0, 0));
                var y = matrix.MultiplyVector(new Vector3(0, local.extents.y, 0));
                var z = matrix.MultiplyVector(new Vector3(0, 0, local.extents.z));
                var extents = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                    Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                var current = new Bounds(matrix.MultiplyPoint3x4(local.center), extents * 2);
                if (!found) { bounds = current; found = true; }
                else bounds.Encapsulate(current);
            }
            return bounds;
        }

        private void Configure(SceneView view)
        {
            var source = view.camera;
            Camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Camera.orthographic = source.orthographic;
            Camera.orthographicSize = source.orthographicSize;
            Camera.fieldOfView = UnityEngine.Camera.FocalLengthToFieldOfView(FocalLength, 24);
            Camera.nearClipPlane = source.nearClipPlane;
            Camera.farClipPlane = source.farClipPlane;
            Camera.clearFlags = Background == ImageBackground.Skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            Camera.GetComponent<Skybox>().material = RenderSettings.skybox;
            utility.ambientColor = FixedLighting ? new Color(0.55f, 0.55f, 0.55f) : RenderSettings.ambientLight;
            for (var i = 0; i < utility.lights.Length; i++)
            {
                utility.lights[i].intensity = FixedLighting ? (i == 0 ? 1f : 0.5f) : 0f;
                utility.lights[i].transform.rotation = source.transform.rotation * Quaternion.Euler(i == 0 ? new Vector3(30f, -30f, 0f) : new Vector3(-20f, 140f, 0f));
            }
        }

        private RenderTexture RenderFrame(int width, int height, Color background, bool transparent)
        {
            Camera.backgroundColor = background;
            if (transparent) Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.aspect = width / (float)height;
            utility.BeginPreview(new Rect(0, 0, width / EditorGUIUtility.pixelsPerPoint, height / EditorGUIUtility.pixelsPerPoint), GUIStyle.none);
            try { utility.Render(false, false); }
            catch { utility.EndPreview(); throw; }
            return (RenderTexture)utility.EndPreview();
        }

        public RenderTexture Render(SceneView view, int width, int height, bool transparent, int factor, bool checker = false)
        {
            Configure(view);
            RenderTexture black = null;
            RenderTexture resolved = null;
            var oldActive = RenderTexture.active;
            try
            {
                RenderTexture.active = null;
                var frame = RenderFrame(width * factor, height * factor, transparent ? Color.black : BackgroundColor, transparent);
                if (transparent)
                {
                    black = RenderTexture.GetTemporary(frame.width, frame.height, 0, frame.format, RenderTextureReadWrite.Linear);
                    Graphics.Blit(frame, black);
                    frame = RenderFrame(width * factor, height * factor, Color.white, true);
                }
                if (!display || display.width != width || display.height != height)
                {
                    if (display) Object.DestroyImmediate(display);
                    display = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                        { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                    display.Create();
                }
                resolveMaterial.SetTexture("_BlackTex", transparent ? black : frame);
                resolveMaterial.SetTexture("_WhiteTex", frame);
                resolveMaterial.SetInt("_Factor", factor);
                resolveMaterial.SetInt("_Transparent", transparent ? 1 : 0);
                var linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                var grid = checker ? Grid : CompositionGrid.None;
                var shape = Shape == ImageShape.Custom && !ShapeMask ? ImageShape.Rectangle : Shape;
                var shadow = transparent && DropShadow;
                var effects = checker || grid != CompositionGrid.None || shape != ImageShape.Rectangle || shadow;
                resolveMaterial.SetInt("_Linear", linear && !checker && !effects ? 1 : 0);
                resolveMaterial.SetInt("_PreviewLinear", linear && checker ? 1 : 0);
                resolveMaterial.SetInt("_Checker", checker && transparent && !effects ? 1 : 0);
                resolveMaterial.SetVector("_OutputSize", new Vector4(width, height, 0, 0));
                if (effects)
                {
                    resolved = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                    resolved.filterMode = FilterMode.Bilinear;
                    resolved.wrapMode = TextureWrapMode.Clamp;
                    Graphics.Blit(frame, resolved, resolveMaterial, 0);
                    resolveMaterial.SetInt("_Grid", (int)grid);
                    resolveMaterial.SetInt("_SafeArea", checker && SafeArea ? 1 : 0);
                    resolveMaterial.SetInt("_Shape", (int)shape);
                    resolveMaterial.SetInt("_Shadow", shadow ? 1 : 0);
                    resolveMaterial.SetInt("_Checker", checker && (transparent || shape != ImageShape.Rectangle) ? 1 : 0);
                    resolveMaterial.SetInt("_Linear", linear && !checker ? 1 : 0);
                    resolveMaterial.SetTexture("_ShapeMask", ShapeMask ? ShapeMask : Texture2D.whiteTexture);
                    Graphics.Blit(resolved, display, resolveMaterial, 1);
                }
                else Graphics.Blit(frame, display, resolveMaterial, 0);
                return display;
            }
            finally
            {
                if (black) RenderTexture.ReleaseTemporary(black);
                if (resolved) RenderTexture.ReleaseTemporary(resolved);
                RenderTexture.active = oldActive;
            }
        }

        public Texture2D Capture(SceneView view, ImageCaptureSettings settings)
        {
            var issue = settings.Validate(SystemInfo.maxTextureSize);
            if (issue != null) throw new InvalidOperationException(issue);
            if (Shape == ImageShape.Custom && !ShapeMask)
                throw new InvalidOperationException(ImageLocalization.Text("マスクテクスチャを指定してください。"));
            Sync(true);
            var frame = Render(view, settings.Width, settings.Height, settings.Transparent, settings.Supersampling);
            var previous = RenderTexture.active;
            var image = new Texture2D(settings.Width, settings.Height, TextureFormat.RGBA32, false, false)
                { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                RenderTexture.active = frame;
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0, false);
                return image;
            }
            catch { Object.DestroyImmediate(image); throw; }
            finally { RenderTexture.active = previous; }
        }

        private void ClearCopies()
        {
            foreach (var surface in surfaces)
            {
                if (surface.Copy) Object.DestroyImmediate(surface.Copy.gameObject);
                if (surface.Baked) Object.DestroyImmediate(surface.Baked);
            }
            surfaces.Clear();
            foreach (var pair in sceneLights) if (pair.Value) Object.DestroyImmediate(pair.Value.gameObject);
            sceneLights.Clear();
            foreach (var go in poseRoots) if (go) Object.DestroyImmediate(go);
            poseRoots.Clear();
            poseTransforms.Clear();
        }

        public void Dispose()
        {
            var previous = RenderTexture.active;
            var owned = previous == display || previous == utility.camera.targetTexture;
            RenderTexture.active = null;
            ClearCopies();
            utility.Cleanup();
            if (resolveMaterial) Object.DestroyImmediate(resolveMaterial);
            if (display) Object.DestroyImmediate(display);
            if (!owned && previous) RenderTexture.active = previous;
        }
    }
}
