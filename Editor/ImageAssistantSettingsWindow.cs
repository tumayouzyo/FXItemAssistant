using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TY.ImageAssistant
{
    internal sealed class ImageAssistantSettingsWindow : EditorWindow
    {
        private VisualElement content;

        internal void Initialize(VisualElement panel)
        {
            content = panel;
            titleContent = new GUIContent("FXItemAssistant 設定");
            minSize = new Vector2(580, 420);
            Build();
        }

        public void CreateGUI() => Build();

        private void Build()
        {
            rootVisualElement.Clear();
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ImageAssistantWindow.AssetRoot + "/Editor/ImageAssistantWindow.uss");
            if (sheet && !rootVisualElement.styleSheets.Contains(sheet)) rootVisualElement.styleSheets.Add(sheet);
            var container = new VisualElement();
            container.AddToClassList("assistant-root");
            container.AddToClassList("settings-window");
            rootVisualElement.Add(container);
            var scroll = new ScrollView();
            scroll.AddToClassList("grow");
            container.Add(scroll);
            scroll.Add(new Label("設定・追加プリセット"));
            if (content != null) scroll.Add(content);
            var about = new VisualElement();
            about.AddToClassList("section");
            about.Add(new Label("FX Item Assistant"));
            about.Add(new Label("責任者: tumayouzyo / 作成者: Codex"));
            about.Add(new Button(() => Application.OpenURL("https://github.com/tumayouzyo/FXItemAssistant")) { text = "GitHub" });
            about.Add(new Button(() => Application.OpenURL("https://github.com/tumayouzyo/FXItemAssistant/issues")) { text = "連絡先・不具合報告: GitHub Issues" });
            about.Add(new Button(() => Application.OpenURL("https://github.com/tumayouzyo/FXItemAssistant/blob/main/LICENSE")) { text = "MITライセンス全文" });
            var terms = new Label("MITライセンスです。個人・商用利用、改変、再配布が可能です。再配布時は著作権表示とライセンス文を保持してください。無保証のため、ご自身の責任でご利用ください。第三者アセットは各権利者の規約に従ってください。生成した画像へのクレジットは不要です。個別サポートや更新の継続は保証していません。");
            terms.AddToClassList("hint");
            about.Add(terms);
            about.Add(new Label("設定の変更は即時反映します。追加プリセットは登録時に保存します。") { name = "settingsNotice" });
            container.Add(about);
            container.Add(new Button(Close) { text = "閉じる" });
        }

        private void OnDisable() => content?.RemoveFromHierarchy();
    }
}
