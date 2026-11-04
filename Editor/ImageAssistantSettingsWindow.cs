using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TY.ImageAssistant
{
    internal sealed class ImageAssistantSettingsWindow : EditorWindow
    {
        private VisualElement content;

        private void OnEnable() => ImageLocalization.Changed += Build;

        internal void Initialize(VisualElement panel)
        {
            content = panel;
            titleContent = new GUIContent(ImageLocalization.Text("FXItemAssistant 設定"));
            minSize = new Vector2(580, 560);
            Build();
        }

        public void CreateGUI() => Build();

        private void Build()
        {
            titleContent = new GUIContent(ImageLocalization.Text("FXItemAssistant 設定"));
            rootVisualElement.Clear();
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ImageAssistantWindow.AssetRoot + "/Editor/ImageAssistantWindow.uss");
            if (sheet && !rootVisualElement.styleSheets.Contains(sheet)) rootVisualElement.styleSheets.Add(sheet);
            var container = new VisualElement();
            container.AddToClassList("settings-root");
            container.AddToClassList("settings-window");
            rootVisualElement.Add(container);
            var scroll = new ScrollView();
            scroll.AddToClassList("settings-content");
            container.Add(scroll);
            var languageHeading = new Label("表示言語");
            languageHeading.AddToClassList("settings-heading");
            scroll.Add(languageHeading);
            var languages = new VisualElement();
            languages.AddToClassList("settings-language-buttons");
            var japanese = new Button(() => ImageLocalization.English = false) { text = "日本語", name = "japaneseButton" };
            var english = new Button(() => ImageLocalization.English = true) { text = "English", name = "englishButton" };
            japanese.SetEnabled(ImageLocalization.English);
            english.SetEnabled(!ImageLocalization.English);
            languages.Add(japanese);
            languages.Add(english);
            scroll.Add(languages);
            if (content != null) scroll.Add(content);
            var notice = new Label("設定の変更は即時反映します。追加プリセットは登録時に保存します。") { name = "settingsNotice" };
            notice.AddToClassList("settings-description");
            scroll.Add(notice);
            var about = new VisualElement { name = "aboutSettings" };
            about.AddToClassList("settings-footer");
            var heading = new Label("FX Item Assistant");
            heading.AddToClassList("settings-heading");
            about.Add(heading);
            var credit = new Label("責任者: tumayouzyo / 作成者: Codex");
            credit.AddToClassList("footer-credit");
            about.Add(credit);
            var links = new VisualElement();
            links.AddToClassList("footer-links");
            links.Add(new Button(() => Application.OpenURL("https://github.com/tumayouzyo/FXItemAssistant")) { text = "GitHub", name = "githubButton" });
            links.Add(new Button(() => Application.OpenURL("https://github.com/tumayouzyo/FXItemAssistant/issues")) { text = "連絡先・不具合報告: GitHub Issues", name = "issuesButton" });
            links.Add(new Button(() => Application.OpenURL("https://github.com/tumayouzyo/FXItemAssistant/blob/main/LICENSE")) { text = "MITライセンス全文", name = "licenseButton" });
            about.Add(links);
            var details = new Foldout { text = "ライセンス・注意事項", name = "licenseDetails", value = false };
            details.AddToClassList("footer-details");
            var detailsScroll = new ScrollView();
            detailsScroll.AddToClassList("footer-details-scroll");
            details.Add(detailsScroll);
            about.Add(details);
            var terms = new Label("MITライセンスです。個人・商用利用、改変、再配布が可能です。再配布時は著作権表示とライセンス文を保持してください。無保証のため、ご自身の責任でご利用ください。第三者アセットは各権利者の規約に従ってください。生成した画像へのクレジットは不要です。個別サポートや更新の継続は保証していません。");
            terms.AddToClassList("settings-description");
            detailsScroll.Add(terms);
            container.Add(about);
            var actions = new VisualElement();
            actions.AddToClassList("settings-actions");
            var spacer = new VisualElement();
            spacer.AddToClassList("settings-spacer");
            actions.Add(spacer);
            actions.Add(new Button(Close) { text = "閉じる", name = "closeSettingsButton" });
            container.Add(actions);
            ImageLocalization.Translate(rootVisualElement);
        }

        private void OnDisable()
        {
            ImageLocalization.Changed -= Build;
            content?.RemoveFromHierarchy();
        }
    }
}
