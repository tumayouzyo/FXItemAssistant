using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TY.ImageAssistant
{
    public sealed class ImageAssistantWindow : EditorWindow
    {
        private static string assetRoot;
        internal static string AssetRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(assetRoot)) return assetRoot;
                var script = AssetDatabase.FindAssets("ImageAssistantWindow t:MonoScript")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(path => Path.GetFileName(path) == "ImageAssistantWindow.cs");
                if (string.IsNullOrEmpty(script)) throw new InvalidOperationException("ツールのインストール先を確認できませんでした。");
                assetRoot = Path.GetDirectoryName(Path.GetDirectoryName(script))?.Replace('\\', '/');
                return assetRoot;
            }
        }
        private ImageCaptureSettings settings = new ImageCaptureSettings();
        private readonly ImageToolOptions options = new ImageToolOptions();
        private List<ImagePreset> presets;
        private GameObject item;
        private Renderer basis;
        private ItemPreviewScene preview;
        private SceneView sceneView;
        private string saveName = "";
        private string saveFolder = "";
        private string presetName = "規定（ExpMenu）";
        private bool fixedLighting;
        private ImageBackground background;
        private Color backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private float focalLength = 85;
        private bool dirty = true;
        private bool faulted;
        private double nextUpdate;
        private double nextSourceUpdate;
        private int lastCameraHash;
        private Image previewImage;
        private HelpBox status;
        private DropdownField presetField;
        private Button saveButton;
        private bool uiReady;
        private VisualElement settingsPanel;
        private ImageAssistantSettingsWindow settingsWindow;

        [MenuItem("Tools/FXItemAssistant")]
        public static void Open() => GetWindow<ImageAssistantWindow>("FXItemAssistant");

        private void OnEnable()
        {
            minSize = new Vector2(420, 640);
            presets = ImagePresets.Load();
            EditorApplication.update += UpdatePreview;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            Undo.undoRedoPerformed += Invalidate;
            AssemblyReloadEvents.beforeAssemblyReload += DisposePreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            Undo.undoRedoPerformed -= Invalidate;
            AssemblyReloadEvents.beforeAssemblyReload -= DisposePreview;
            DisposePreview();
            if (settingsWindow) settingsWindow.Close();
            uiReady = false;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetRoot + "/Editor/ImageAssistantWindow.uxml");
            if (!layout) { rootVisualElement.Add(new Label("レイアウトを読み込めませんでした。")); return; }
            layout.CloneTree(rootVisualElement);
            previewImage = Q<Image>("previewImage");
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            previewImage.AddManipulator(new PreviewCameraManipulator(DragCamera, ZoomCamera));
            previewImage.RegisterCallback<GeometryChangedEvent>(_ => Invalidate());
            status = Q<HelpBox>("status");
            saveButton = Q<Button>("saveButton");
            var targetField = Q<ObjectField>("targetField");
            targetField.objectType = typeof(GameObject);
            targetField.allowSceneObjects = true;
            targetField.RegisterValueChangedCallback(evt => Run(() => SetItem(evt.newValue as GameObject)));
            var basisField = Q<ObjectField>("basisField");
            basisField.objectType = typeof(Renderer);
            basisField.allowSceneObjects = true;
            basisField.RegisterValueChangedCallback(evt => Run(() =>
            {
                var candidate = evt.newValue as Renderer;
                if (candidate && (!item || !candidate.transform.IsChildOf(item.transform) || !(candidate is MeshRenderer || candidate is SkinnedMeshRenderer)))
                {
                    basisField.SetValueWithoutNotify(basis);
                    throw new ArgumentException("撮影アイテム配下のSMRまたはMRを指定してください。");
                }
                basis = candidate;
                ResetFolder();
            }));
            Button("selectedButton", () => SetItem(Selection.activeGameObject));
            Button("reloadButton", RebuildPreview);
            Button("frameButton", FrameItem);
            Button("lightingButton", () => { fixedLighting = !fixedLighting; RefreshFields(); Invalidate(); });
            presetField = Q<DropdownField>("presetField");
            RefreshPresetChoices();
            presetField.RegisterValueChangedCallback(evt => Run(() => ApplyPreset(evt.newValue)));
            Q<IntegerField>("widthField").RegisterValueChangedCallback(evt => ChangeSettings(() => settings.Width = evt.newValue));
            Q<IntegerField>("heightField").RegisterValueChangedCallback(evt => ChangeSettings(() => settings.Height = evt.newValue));
            var sampling = Q<DropdownField>("samplingField");
            sampling.choices = Enumerable.Range(1, 8).Select(value => value + "x").ToList();
            sampling.RegisterValueChangedCallback(evt => ChangeSettings(() => settings.Supersampling = sampling.index + 1));
            var imports = Q<DropdownField>("importField");
            imports.choices = new List<string> { "アイコン", "通常（デカール素材）", "サムネイル" };
            imports.RegisterValueChangedCallback(evt => ChangeSettings(() => settings.ImportMode = (ImageImportMode)imports.index));
            Q<Toggle>("transparentField").RegisterValueChangedCallback(evt => ChangeSettings(() => settings.Transparent = evt.newValue));
            Q<Toggle>("hideOthersField").RegisterValueChangedCallback(evt => ChangeSettings(() =>
            {
                settings.HideOthers = evt.newValue;
                RebuildPreview();
            }));
            Q<Slider>("focalField").RegisterValueChangedCallback(evt => Run(() =>
            {
                focalLength = Mathf.Clamp(evt.newValue, 85, 200);
                RefreshFields();
                Invalidate();
            }));
            Q<Toggle>("orthoField").RegisterValueChangedCallback(evt => Run(() =>
            {
                if (GetSceneView()) { sceneView.orthographic = evt.newValue; sceneView.Repaint(); }
                Invalidate();
            }));
            var backgrounds = Q<DropdownField>("backgroundField");
            backgrounds.choices = new List<string> { "Solid", "Skybox" };
            backgrounds.RegisterValueChangedCallback(evt => { background = (ImageBackground)backgrounds.index; RefreshFields(); Invalidate(); });
            Q<ColorField>("colorField").RegisterValueChangedCallback(evt => { backgroundColor = evt.newValue; Invalidate(); });
            Q<ColorField>("colorField").showAlpha = false;
            Q<Toggle>("prefixEnabled").RegisterValueChangedCallback(evt => { options.PrefixEnabled = evt.newValue; RefreshOutput(); });
            Q<TextField>("prefixText").RegisterValueChangedCallback(evt => { options.Prefix = evt.newValue; RefreshOutput(); });
            Q<Toggle>("numberingField").RegisterValueChangedCallback(evt => { options.Numbering = evt.newValue; RefreshOutput(); });
            Q<Toggle>("forceFolderField").RegisterValueChangedCallback(evt => { options.ForceFolder = evt.newValue; RefreshFields(); });
            Q<TextField>("forcedFolderField").RegisterValueChangedCallback(evt => { options.ForcedFolder = evt.newValue; RefreshOutput(); });
            Button("forcedBrowseButton", () => ChooseFolder(true));
            Button("addPresetButton", AddPreset);
            Button("deletePresetButton", DeletePreset);
            Q<TextField>("saveName").RegisterValueChangedCallback(evt => { saveName = evt.newValue; RefreshOutput(); });
            Q<TextField>("saveFolder").RegisterValueChangedCallback(evt => { saveFolder = evt.newValue; RefreshOutput(); });
            Button("browseButton", () => ChooseFolder(false));
            Button("defaultFolderButton", ResetFolder);
            Button("saveButton", SaveImage);
            Button("highlightButton", HighlightFolder);
            settingsPanel = Q<VisualElement>("settingsPanel");
            settingsPanel.RemoveFromHierarchy();
            Button("settingsButton", OpenSettings);
            uiReady = true;
            RefreshFields();
        }

        private T Q<T>(string name) where T : VisualElement => rootVisualElement.Q<T>(name) ?? settingsPanel?.Q<T>(name);
        private void OpenSettings()
        {
            if (!settingsWindow) settingsWindow = CreateInstance<ImageAssistantSettingsWindow>();
            settingsWindow.Initialize(settingsPanel);
            settingsWindow.ShowUtility();
            settingsWindow.Focus();
        }

        private void Button(string name, Action action) => Q<Button>(name).clicked += () => Run(action);
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { Message(exception.Message, HelpBoxMessageType.Error); }
        }

        private void Message(string text, HelpBoxMessageType type = HelpBoxMessageType.Info)
        {
            if (status == null) return;
            status.text = text;
            status.messageType = type;
        }

        private void SetItem(GameObject next)
        {
            if (next && (EditorUtility.IsPersistent(next) || !next.scene.IsValid() || EditorSceneManager.IsPreviewScene(next.scene)))
            {
                Q<ObjectField>("targetField").SetValueWithoutNotify(item);
                throw new ArgumentException("Project内のアセットではなく、Hierarchyのオブジェクトを指定してください。");
            }
            item = next;
            basis = item ? item.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault() : null;
            if (!basis && item) basis = item.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault();
            saveName = item ? item.name : "";
            saveFolder = ImageAssetWriter.FindDefaultFolder(basis);
            RebuildPreview();
            if (item) FrameItem();
            RefreshFields();
            Message(!item ? "Hierarchyから撮影するアイテムを指定してください。"
                : string.IsNullOrEmpty(saveFolder) ? "保存先を自動判定できませんでした。保存先を指定してください。"
                : "Sceneビューと同期中です。Inspectorでの変更もプレビューへ反映します。");
        }

        private void ChangeSettings(Action change)
        {
            Run(() =>
            {
                change();
                presetName = "カスタム";
                RefreshFields();
                Invalidate();
            });
        }

        private void ApplyPreset(string name)
        {
            var index = Array.IndexOf(ImagePresets.BuiltInNames, name);
            var previousHideOthers = settings.HideOthers;
            if (index >= 0 && index < 3) settings = ImagePresets.Create(index);
            else if (index == -1)
            {
                var preset = presets.Find(p => p.Name == name);
                if (preset == null) return;
                settings = preset.Settings.Copy();
            }
            presetName = name;
            if (previousHideOthers != settings.HideOthers) RebuildPreview();
            RefreshFields();
            Invalidate();
        }

        private void RefreshPresetChoices()
        {
            presetField.choices = ImagePresets.BuiltInNames.Concat(presets.Select(p => p.Name)).ToList();
            presetField.SetValueWithoutNotify(presetName);
        }

        private void AddPreset()
        {
            var name = Q<TextField>("presetName").value.Trim();
            if (string.IsNullOrWhiteSpace(name) || presetField.choices.Contains(name))
                throw new ArgumentException("重複しないプリセット名を入力してください。");
            var issue = settings.Validate(int.MaxValue);
            if (issue != null) throw new ArgumentException(issue);
            presets.Add(new ImagePreset(name, settings.Copy()));
            ImagePresets.Save(presets);
            presetName = name;
            RefreshPresetChoices();
            Message("追加プリセットを登録しました。");
        }

        private void DeletePreset()
        {
            if (ImagePresets.BuiltInNames.Contains(presetName)) throw new InvalidOperationException("標準プリセットは削除できません。");
            presets.RemoveAll(p => p.Name == presetName);
            ImagePresets.Save(presets);
            presetName = "カスタム";
            RefreshPresetChoices();
            Message("追加プリセットを削除しました。現在の設定値は維持しています。");
        }

        private void RefreshFields()
        {
            if (!uiReady) return;
            Q<ObjectField>("targetField").SetValueWithoutNotify(item);
            Q<ObjectField>("basisField").SetValueWithoutNotify(basis);
            presetField.SetValueWithoutNotify(presetName);
            Q<IntegerField>("widthField").SetValueWithoutNotify(settings.Width);
            Q<IntegerField>("heightField").SetValueWithoutNotify(settings.Height);
            Q<DropdownField>("samplingField").SetValueWithoutNotify(settings.Supersampling + "x");
            Q<DropdownField>("importField").SetValueWithoutNotify(Q<DropdownField>("importField").choices[(int)settings.ImportMode]);
            Q<Toggle>("transparentField").SetValueWithoutNotify(settings.Transparent);
            Q<Toggle>("hideOthersField").SetValueWithoutNotify(settings.HideOthers);
            Q<Slider>("focalField").SetValueWithoutNotify(focalLength);
            Q<Slider>("focalField").SetEnabled(!sceneView || !sceneView.orthographic);
            Q<Toggle>("orthoField").SetValueWithoutNotify(sceneView && sceneView.orthographic);
            Q<DropdownField>("backgroundField").SetValueWithoutNotify(background.ToString());
            Q<DropdownField>("backgroundField").SetEnabled(!settings.Transparent);
            Q<ColorField>("colorField").SetValueWithoutNotify(backgroundColor);
            Q<ColorField>("colorField").SetEnabled(!settings.Transparent && background == ImageBackground.Solid);
            Q<Button>("lightingButton").text = fixedLighting ? "照明：専用固定" : "照明：シーン";
            Q<TextField>("saveName").SetValueWithoutNotify(saveName);
            Q<TextField>("saveFolder").SetValueWithoutNotify(EffectiveFolder);
            Q<TextField>("saveFolder").SetEnabled(!options.ForceFolder);
            Q<Button>("browseButton").SetEnabled(!options.ForceFolder);
            Q<Button>("defaultFolderButton").SetEnabled(!options.ForceFolder);
            Q<Toggle>("prefixEnabled").SetValueWithoutNotify(options.PrefixEnabled);
            Q<TextField>("prefixText").SetValueWithoutNotify(options.Prefix);
            Q<Toggle>("numberingField").SetValueWithoutNotify(options.Numbering);
            Q<Toggle>("forceFolderField").SetValueWithoutNotify(options.ForceFolder);
            Q<TextField>("forcedFolderField").SetValueWithoutNotify(options.ForcedFolder);
            Q<TextField>("forcedFolderField").SetEnabled(options.ForceFolder);
            Q<Button>("forcedBrowseButton").SetEnabled(options.ForceFolder);
            RefreshOutput();
        }

        private string EffectiveFolder => options.ForceFolder ? options.ForcedFolder : saveFolder;

        private void RefreshOutput()
        {
            if (!uiReady) return;
            var issue = settings.Validate(SystemInfo.maxTextureSize);
            try
            {
                var path = ImageAssetWriter.ResolvePath(EffectiveFolder, saveName, options);
                Q<Label>("outputSummary").text = issue ?? path + (options.Numbering ? "（連番）" : "（同名は上書き）");
                saveButton.SetEnabled(item && preview != null && !faulted && issue == null);
            }
            catch (ArgumentException exception)
            {
                Q<Label>("outputSummary").text = issue ?? exception.Message;
                saveButton.SetEnabled(false);
            }
        }

        private void ResetFolder() { saveFolder = ImageAssetWriter.FindDefaultFolder(basis); RefreshFields(); }

        private void ChooseFolder(bool forced)
        {
            var path = EditorUtility.OpenFolderPanel("PNGの保存先", Application.dataPath, "");
            if (string.IsNullOrEmpty(path)) return;
            var folder = ImageAssetWriter.FromAbsoluteFolder(path);
            if (forced) options.ForcedFolder = folder;
            else saveFolder = folder;
            RefreshFields();
        }

        private void HighlightFolder()
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EffectiveFolder);
            if (!folder) { Message("保存先はまだありません。PNG保存時に作成します。"); return; }
            EditorGUIUtility.PingObject(folder);
        }

        private bool GetSceneView()
        {
            if (SceneView.lastActiveSceneView) sceneView = SceneView.lastActiveSceneView;
            if (!sceneView) sceneView = SceneView.sceneViews.Count > 0 ? SceneView.sceneViews[0] as SceneView : null;
            return sceneView;
        }

        private void FrameItem()
        {
            if (!item || preview == null || !GetSceneView()) return;
            preview.Sync(true);
            var bounds = preview.Bounds();
            var radius = Mathf.Max(0.01f, bounds.extents.magnitude);
            var aspect = settings.Width / (float)Mathf.Max(1, settings.Height);
            var size = radius * 1.12f / Mathf.Min(1, aspect);
            if (!sceneView.orthographic)
                size *= Mathf.Sin(sceneView.camera.fieldOfView * Mathf.Deg2Rad * 0.5f)
                    / Mathf.Sin(Camera.FocalLengthToFieldOfView(focalLength, 24) * Mathf.Deg2Rad * 0.5f);
            sceneView.LookAtDirect(bounds.center, sceneView.rotation, size);
            sceneView.Repaint();
            Invalidate();
        }

        private void DragCamera(int button, Vector2 delta)
        {
            if (!item || preview == null || !GetSceneView()) return;
            if (button == 1)
            {
                var center = preview.Bounds().center;
                var rotation = Quaternion.AngleAxis(delta.x * 0.3f, Vector3.up)
                    * Quaternion.AngleAxis(delta.y * 0.3f, sceneView.rotation * Vector3.right);
                sceneView.LookAtDirect(center + rotation * (sceneView.pivot - center), rotation * sceneView.rotation, sceneView.size);
            }
            else
            {
                var height = Mathf.Max(1, previewImage.contentRect.height);
                var halfHeight = sceneView.orthographic ? sceneView.camera.orthographicSize
                    : Vector3.Distance(sceneView.camera.transform.position, sceneView.pivot) * Mathf.Tan(Camera.FocalLengthToFieldOfView(focalLength, 24) * Mathf.Deg2Rad * 0.5f);
                var movement = sceneView.rotation * new Vector3(-delta.x, delta.y, 0) * (2 * halfHeight / height);
                sceneView.LookAtDirect(sceneView.pivot + movement, sceneView.rotation, sceneView.size);
            }
            sceneView.Repaint();
            Invalidate();
        }

        private void ZoomCamera(float delta)
        {
            if (!GetSceneView()) return;
            sceneView.LookAtDirect(sceneView.pivot, sceneView.rotation, Mathf.Max(0.0001f, sceneView.size * Mathf.Exp(delta * 0.035f)));
            sceneView.Repaint();
            Invalidate();
        }

        private void RebuildPreview()
        {
            DisposePreview();
            faulted = false;
            if (item) preview = new ItemPreviewScene(item, settings.HideOthers);
            Invalidate();
            RefreshOutput();
        }

        private void DisposePreview()
        {
            if (previewImage != null) previewImage.image = null;
            preview?.Dispose();
            preview = null;
        }

        private void Invalidate() { dirty = true; }

        private void UpdatePreview()
        {
            if (!uiReady || previewImage?.panel == null || faulted || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.timeSinceStartup < nextUpdate) return;
            nextUpdate = EditorApplication.timeSinceStartup + 1d / 60;
            if (!item) { if (preview != null) { DisposePreview(); RefreshOutput(); } return; }
            if (preview == null || !GetSceneView()) return;
            try
            {
                var camera = sceneView.camera;
                var hash = camera.transform.localToWorldMatrix.GetHashCode() ^ camera.fieldOfView.GetHashCode()
                    ^ camera.orthographicSize.GetHashCode() ^ camera.orthographic.GetHashCode() ^ RenderSettings.ambientLight.GetHashCode()
                    ^ (RenderSettings.skybox ? EditorUtility.GetDirtyCount(RenderSettings.skybox) : 0);
                dirty |= lastCameraHash != hash;
                lastCameraHash = hash;
                preview.FixedLighting = fixedLighting;
                preview.Background = background;
                preview.BackgroundColor = backgroundColor;
                preview.FocalLength = focalLength;
                if (EditorApplication.timeSinceStartup >= nextSourceUpdate)
                {
                    nextSourceUpdate = EditorApplication.timeSinceStartup + (EditorApplication.isPlaying ? 1d / 30 : 1d / 15);
                    dirty |= preview.Sync();
                }
                if (!dirty) return;
                var aspect = settings.Width / (float)Mathf.Max(1, settings.Height);
                var width = Mathf.Clamp(Mathf.RoundToInt(previewImage.contentRect.width * EditorGUIUtility.pixelsPerPoint), 64, 1024);
                var height = Mathf.Clamp(Mathf.RoundToInt(width / Mathf.Max(0.01f, aspect)), 1, 1024);
                width = Mathf.Clamp(Mathf.RoundToInt(height * aspect), 1, 1024);
                previewImage.image = preview.Render(sceneView, width, height, settings.Transparent, 1, true);
                dirty = false;
                Q<Toggle>("orthoField").SetValueWithoutNotify(sceneView.orthographic);
                Q<Slider>("focalField").SetEnabled(!sceneView.orthographic);
                Q<Label>("sceneSummary").text = $"{item.scene.name} ｜ メッシュ {preview.SurfaceCount} ｜ {(EditorApplication.isPlaying ? "Play中" : "Edit中")}";
            }
            catch (Exception exception)
            {
                faulted = true;
                Message("プレビューを更新できません。「再読み込み」を試してください。\n" + exception.Message, HelpBoxMessageType.Error);
                RefreshOutput();
            }
        }

        private void SaveImage()
        {
            if (!item || preview == null || !GetSceneView()) throw new InvalidOperationException("撮影アイテムとSceneビューを確認してください。");
            var path = ImageAssetWriter.ResolvePath(EffectiveFolder, saveName, options);
            Texture2D image = null;
            try
            {
                EditorUtility.DisplayProgressBar("FXItemAssistant", "画像を撮影しています…", 0.25f);
                preview.FixedLighting = fixedLighting;
                preview.Background = background;
                preview.BackgroundColor = backgroundColor;
                preview.FocalLength = focalLength;
                image = preview.Capture(sceneView, settings);
                EditorUtility.DisplayProgressBar("FXItemAssistant", "PNGを保存しています…", 0.7f);
                ImageAssetWriter.Save(path, image, settings);
                Message("保存しました：" + path);
            }
            finally
            {
                if (image) Object.DestroyImmediate(image);
                EditorUtility.ClearProgressBar();
                Invalidate();
                RefreshOutput();
            }
        }

        private void PlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.ExitingPlayMode) return;
            DisposePreview();
            item = null;
            basis = null;
            RefreshFields();
            Message("モード切替後、Hierarchyから撮影対象を指定してください。NDMFビルド後のオブジェクトも指定できます。");
        }
    }
}
