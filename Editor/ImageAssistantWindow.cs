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
                if (!string.IsNullOrEmpty(assetRoot) && AssetDatabase.IsValidFolder(assetRoot)) return assetRoot;
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
        private string presetName = "アイコン (既定)";
        private bool fixedLighting;
        private ImageBackground background;
        private CompositionGrid grid;
        private ImageShape shape;
        private Texture2D shapeMask;
        private bool dropShadow;

        private static readonly string[] GridNames = { "なし", "三分割法", "黄金比" };
        private static readonly string[] ShapeNames = { "全て", "円形切り抜き", "角の丸め", "カスタム" };
        private Color backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private float focalLength = 85;
        private bool dirty = true;
        private bool faulted;
        private double nextUpdate;
        private double nextSourceUpdate;
        private int lastCameraHash;
        private Image previewImage;
        private HelpBox status;
        private string statusKey = "Hierarchyから撮影するアイテムを指定してください。";
        private object[] statusArguments = Array.Empty<object>();
        private DropdownField presetField;
        private Button saveButton;
        private bool uiReady;
        private bool refreshingControls;
        private VisualElement settingsPanel;
        private ImageAssistantSettingsWindow settingsWindow;

        [MenuItem("Tools/FXItemAssistant")]
        public static void Open() => GetWindow<ImageAssistantWindow>("FXItemAssistant");

        private void OnEnable()
        {
            minSize = new Vector2(420, 640);
            presets = ImagePresets.Load();
            EditorApplication.update += UpdatePreview;
            ImageLocalization.Changed += RefreshLanguage;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            Undo.undoRedoPerformed += Invalidate;
            AssemblyReloadEvents.beforeAssemblyReload += DisposePreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
            ImageLocalization.Changed -= RefreshLanguage;
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
            OnValue(targetField, evt => Run(() => SetItem(evt.newValue as GameObject)));
            var basisField = Q<ObjectField>("basisField");
            basisField.objectType = typeof(Renderer);
            basisField.allowSceneObjects = true;
            OnValue(basisField, evt => Run(() =>
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
            OnValue(presetField, evt =>
            {
                if (!refreshingControls) Run(() => ApplyPreset(presetField.index >= 0 && presetField.index < 4 ? ImagePresets.BuiltInNames[presetField.index] : evt.newValue));
            });
            OnValue(Q<IntegerField>("widthField"), evt => ChangeSettings(() => settings.Width = evt.newValue));
            OnValue(Q<IntegerField>("heightField"), evt => ChangeSettings(() => settings.Height = evt.newValue));
            var sampling = Q<DropdownField>("samplingField");
            sampling.choices = Enumerable.Range(1, 8).Select(value => value + "x").ToList();
            OnValue(sampling, evt => ChangeSettings(() => settings.Supersampling = sampling.index + 1));
            var imports = Q<DropdownField>("importField");
            imports.choices = new List<string> { ImageLocalization.Text("アイコン"), ImageLocalization.Text("通常（デカール素材）"), ImageLocalization.Text("サムネイル") };
            OnValue(imports, evt =>
            {
                if (!refreshingControls) ChangeSettings(() => settings.ImportMode = (ImageImportMode)imports.index);
            });
            OnValue(Q<Toggle>("transparentField"), evt => ChangeSettings(() => settings.Transparent = evt.newValue));
            OnValue(Q<Toggle>("hideOthersField"), evt => ChangeSettings(() =>
            {
                settings.HideOthers = evt.newValue;
                RebuildPreview();
            }));
            OnValue(Q<Slider>("focalField"), evt => Run(() =>
            {
                focalLength = Mathf.Clamp(evt.newValue, 85, 200);
                RefreshFields();
                Invalidate();
            }));
            OnValue(Q<Toggle>("orthoField"), evt => Run(() =>
            {
                if (GetSceneView()) { sceneView.orthographic = evt.newValue; sceneView.Repaint(); }
                Invalidate();
            }));
            var gridField = Q<DropdownField>("gridField");
            OnValue(gridField, _ => { grid = (CompositionGrid)gridField.index; Invalidate(); });
            var shapeField = Q<DropdownField>("shapeField");
            OnValue(shapeField, _ => { shape = (ImageShape)shapeField.index; RefreshFields(); Invalidate(); });
            var maskField = Q<ObjectField>("maskField");
            maskField.objectType = typeof(Texture2D);
            maskField.allowSceneObjects = false;
            OnValue(maskField, evt => { shapeMask = evt.newValue as Texture2D; RefreshOutput(); Invalidate(); });
            OnValue(Q<Toggle>("shadowField"), evt => { dropShadow = evt.newValue; Invalidate(); });
            var backgrounds = Q<DropdownField>("backgroundField");
            backgrounds.choices = new List<string> { "Solid", "Skybox" };
            OnValue(backgrounds, evt => { background = (ImageBackground)backgrounds.index; RefreshFields(); Invalidate(); });
            OnValue(Q<ColorField>("colorField"), evt => { backgroundColor = evt.newValue; Invalidate(); });
            Q<ColorField>("colorField").showAlpha = false;
            OnValue(Q<Toggle>("prefixEnabled"), evt => { options.PrefixEnabled = evt.newValue; RefreshOutput(); });
            OnValue(Q<TextField>("prefixText"), evt => { options.Prefix = evt.newValue; RefreshOutput(); });
            OnValue(Q<Toggle>("numberingField"), evt => { options.Numbering = evt.newValue; RefreshOutput(); });
            OnValue(Q<Toggle>("forceFolderField"), evt => { options.ForceFolder = evt.newValue; RefreshFields(); });
            OnValue(Q<TextField>("forcedFolderField"), evt => { options.ForcedFolder = evt.newValue; RefreshOutput(); });
            Button("forcedBrowseButton", () => ChooseFolder(true));
            Button("addPresetButton", AddPreset);
            Button("deletePresetButton", DeletePreset);
            OnValue(Q<TextField>("saveName"), evt => { saveName = evt.newValue; RefreshOutput(); });
            OnValue(Q<TextField>("saveFolder"), evt => { saveFolder = evt.newValue; RefreshOutput(); });
            Button("browseButton", () => ChooseFolder(false));
            Button("defaultFolderButton", ResetFolder);
            Button("saveButton", SaveImage);
            Button("highlightButton", HighlightFolder);
            settingsPanel = Q<VisualElement>("settingsPanel");
            settingsPanel.RemoveFromHierarchy();
            Button("settingsButton", OpenSettings);
            uiReady = true;
            RefreshLanguage();
        }

        private T Q<T>(string name) where T : VisualElement => rootVisualElement.Q<T>(name) ?? settingsPanel?.Q<T>(name);
        private void OpenSettings()
        {
            if (!settingsWindow) settingsWindow = CreateInstance<ImageAssistantSettingsWindow>();
            settingsWindow.Initialize(settingsPanel);
            settingsWindow.ShowUtility();
            settingsWindow.Focus();
        }

        private void OnValue<T>(BaseField<T> field, EventCallback<ChangeEvent<T>> callback)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == field && !refreshingControls
                    && EqualityComparer<T>.Default.Equals(evt.newValue, field.value)) callback(evt);
            });
        }

        private void Button(string name, Action action) => Q<Button>(name).clicked += () => Run(action);
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { Message(exception.Message, HelpBoxMessageType.Error); }
        }

        private void Message(string text, HelpBoxMessageType type = HelpBoxMessageType.Info, params object[] arguments)
        {
            if (status == null) return;
            statusKey = text;
            statusArguments = arguments;
            status.messageType = type;
            RefreshMessage();
        }

        private void RefreshMessage()
        {
            if (status != null) status.text = statusArguments.Length == 0
                ? ImageLocalization.Text(statusKey) : ImageLocalization.Format(statusKey, statusArguments);
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
            var previous = refreshingControls;
            refreshingControls = true;
            try
            {
                presetField.choices = ImagePresets.BuiltInNames.Select(ImageLocalization.Text).Concat(presets.Select(p => p.Name)).ToList();
                presetField.SetValueWithoutNotify(ImageLocalization.PresetName(presetName));
            }
            finally { refreshingControls = previous; }
        }

        private void AddPreset()
        {
            var name = Q<TextField>("presetName").value.Trim();
            if (string.IsNullOrWhiteSpace(name) || ImageLocalization.IsBuiltInPresetName(name) || presets.Any(p => p.Name == name))
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

        private void RefreshLanguage()
        {
            if (!uiReady) return;
            refreshingControls = true;
            try
            {
                RefreshPresetChoices();
                Q<DropdownField>("gridField").choices = GridNames.Select(ImageLocalization.Text).ToList();
                Q<DropdownField>("shapeField").choices = ShapeNames.Select(ImageLocalization.Text).ToList();
                Q<DropdownField>("importField").choices = new List<string>
                {
                    ImageLocalization.Text("アイコン"), ImageLocalization.Text("通常（デカール素材）"), ImageLocalization.Text("サムネイル")
                };
                ImageLocalization.Translate(rootVisualElement);
                ImageLocalization.Translate(settingsPanel);
                RefreshFields();
                RefreshMessage();
                Invalidate();
            }
            finally { refreshingControls = false; }
        }

        private void RefreshFields()
        {
            if (!uiReady) return;
            Q<ObjectField>("targetField").SetValueWithoutNotify(item);
            Q<ObjectField>("basisField").SetValueWithoutNotify(basis);
            presetField.SetValueWithoutNotify(ImageLocalization.PresetName(presetName));
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
            Q<Button>("lightingButton").text = ImageLocalization.Text(fixedLighting ? "照明：専用固定" : "照明：シーン");
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
            Q<DropdownField>("gridField").SetValueWithoutNotify(ImageLocalization.Text(GridNames[(int)grid]));
            Q<DropdownField>("shapeField").SetValueWithoutNotify(ImageLocalization.Text(ShapeNames[(int)shape]));
            Q<ObjectField>("maskField").SetValueWithoutNotify(shapeMask);
            Q<ObjectField>("maskField").style.display = shape == ImageShape.Custom ? DisplayStyle.Flex : DisplayStyle.None;
            Q<Toggle>("shadowField").SetValueWithoutNotify(dropShadow);
            Q<Toggle>("shadowField").SetEnabled(settings.Transparent);
            RefreshOutput();
        }

        private string EffectiveFolder => options.ForceFolder ? options.ForcedFolder : saveFolder;

        private void RefreshOutput()
        {
            if (!uiReady) return;
            var issue = settings.Validate(SystemInfo.maxTextureSize);
            if (issue == null && shape == ImageShape.Custom && !shapeMask) issue = "マスクテクスチャを指定してください。";
            try
            {
                var path = ImageAssetWriter.ResolvePath(EffectiveFolder, saveName, options);
                Q<Label>("outputSummary").text = ImageLocalization.Text(issue) ?? path + (options.Numbering ? ImageLocalization.Text("（連番）") : string.Empty);
                saveButton.SetEnabled(item && preview != null && !faulted && issue == null);
            }
            catch (ArgumentException exception)
            {
                Q<Label>("outputSummary").text = ImageLocalization.Text(issue ?? exception.Message);
                saveButton.SetEnabled(false);
            }
        }

        private void ResetFolder() { saveFolder = ImageAssetWriter.FindDefaultFolder(basis); RefreshFields(); }

        private void ChooseFolder(bool forced)
        {
            var path = EditorUtility.OpenFolderPanel(ImageLocalization.Text("PNGの保存先"), Application.dataPath, "");
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
                preview.Grid = grid;
                preview.Shape = shape;
                preview.ShapeMask = shapeMask;
                preview.DropShadow = dropShadow;
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
                Q<Label>("sceneSummary").text = ImageLocalization.Format("{0} ｜ メッシュ {1} ｜ {2}", item.scene.name, preview.SurfaceCount, ImageLocalization.Text(EditorApplication.isPlaying ? "Play中" : "Edit中"));
            }
            catch (Exception exception)
            {
                faulted = true;
                Message(ImageLocalization.Format("プレビューを更新できません。「再読み込み」を試してください。\n{0}", ImageLocalization.Text(exception.Message)), HelpBoxMessageType.Error);
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
                EditorUtility.DisplayProgressBar("FXItemAssistant", ImageLocalization.Text("画像を撮影しています…"), 0.25f);
                preview.FixedLighting = fixedLighting;
                preview.Background = background;
                preview.BackgroundColor = backgroundColor;
                preview.FocalLength = focalLength;
                preview.Grid = grid;
                preview.Shape = shape;
                preview.ShapeMask = shapeMask;
                preview.DropShadow = dropShadow;
                image = preview.Capture(sceneView, settings);
                EditorUtility.DisplayProgressBar("FXItemAssistant", ImageLocalization.Text("PNGを保存しています…"), 0.7f);
                var outputSettings = settings.Copy();
                outputSettings.Transparent |= shape != ImageShape.Rectangle;
                ImageAssetWriter.Save(path, image, outputSettings);
                Message("保存しました：{0}", HelpBoxMessageType.Info, path);
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
