using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TY.ImageAssistant
{
    internal static class ImageLocalization
    {
        private static readonly Dictionary<string, string> EnglishText = new Dictionary<string, string>
        {
            { "構図グリッド", "Composition grid" },
            { "画像の形状", "Image shape" },
            { "マスクテクスチャ", "Mask texture" },
            { "ドロップシャドウ", "Drop shadow" },
            { "なし", "None" },
            { "三分割法", "Rule of thirds" },
            { "黄金比", "Golden ratio" },
            { "全て", "Full image" },
            { "円形切り抜き", "Circle crop" },
            { "角の丸め", "Rounded corners" },
            { "マスクテクスチャを指定してください。", "Choose a mask texture." },
            { "プレビューに構図ガイドを表示します。保存画像には入りません。", "Show composition guides in the preview. Guides are excluded from the saved image." },
            { "保存画像の形状を選択できます。切り抜きの外側は透過します。円は画像の短辺に合わせます。", "Choose the output shape. The area outside the shape is transparent. The circle fits the shorter edge." },
            { "白を残し、黒を透過します。中間色は半透明になります。画像全体に合わせてマスクを適用します。", "White keeps the image, black makes it transparent, and gray gives partial opacity. The mask fits the full canvas." },
            { "背景透過がオンのときに、被写体の右下へ柔らかい影を付けます。影は画像の範囲内に収まります。", "Add a soft shadow below and to the right of the item when background transparency is enabled. The shadow is clipped to the output shape." },
            { "ライセンス・注意事項", "License and notices" },
            { "利用条件と免責事項を確認できます。", "View the terms of use and warranty disclaimer." },
            { "撮影アイテム", "Capture item" },
            { "選択中を指定", "Use selection" },
            { "再読み込み", "Reload" },
            { "設定", "Settings" },
            { "全体を収める", "Frame item" },
            { "照明：シーン", "Lighting: Scene" },
            { "照明：専用固定", "Lighting: Fixed" },
            { "撮影設定", "Capture settings" },
            { "プリセット", "Preset" },
            { "幅 (px)", "Width (px)" },
            { "高さ (px)", "Height (px)" },
            { "インポート設定", "Import settings" },
            { "撮影倍率", "Supersampling" },
            { "背景透過", "Transparent background" },
            { "他Object非表示", "Hide other objects" },
            { "カメラ設定", "Camera settings" },
            { "焦点距離 (mm)", "Focal length (mm)" },
            { "平行投影", "Orthographic" },
            { "背景", "Background" },
            { "背景色", "Background color" },
            { "接頭辞を付ける", "Use prefix" },
            { "接頭辞", "Prefix" },
            { "ナンバリング保存", "Numbered saves" },
            { "保存先強制設定", "Override output folder" },
            { "強制保存先", "Override folder" },
            { "フォルダ選択", "Browse" },
            { "追加プリセット名", "New preset name" },
            { "現在の設定を登録", "Add current settings" },
            { "選択プリセットを削除", "Delete selected preset" },
            { "保存先の基準メッシュ", "Folder source mesh" },
            { "保存名", "File name" },
            { "保存先", "Output folder" },
            { "既定", "Default" },
            { "PNGを保存", "Save PNG" },
            { "保存先をハイライト", "Highlight folder" },
            { "アイコン (既定)", "Icon (Default)" },
            { "素材", "Material" },
            { "サムネイル", "Thumbnail" },
            { "カスタム", "Custom" },
            { "アイコン", "Icon" },
            { "アイコン用", "For icons" },
            { "サムネイル用", "For thumbnails" },
            { "変更なし", "Unchanged" },
            { "保存ルール", "Save rules" },
            { "追加プリセット", "Custom presets" },
            { "FXItemAssistant 設定", "FXItemAssistant Settings" },
            { "表示言語", "Display language" },
            { "閉じる", "Close" },
            { "責任者: tumayouzyo / 作成者: Codex", "Maintainer: tumayouzyo / Created by: Codex" },
            { "連絡先・不具合報告: GitHub Issues", "Contact / bug reports: GitHub Issues" },
            { "MITライセンス全文", "Full MIT license" },
            { "左・中ドラッグ：平行移動 ｜ 右：周回 ｜ ホイール：距離", "Left / middle drag: pan | Right: orbit | Wheel: zoom" },
            { "撮影設定は次回起動へ持ち越しません。登録した追加プリセットだけ、このPC・プロジェクト用に保存します。", "Capture settings reset next time. Only explicitly registered presets are saved for this PC and project." },
            { "設定の変更は即時反映します。追加プリセットは登録時に保存します。", "Changes apply immediately. Custom presets are saved when registered." },
            { "Hierarchyから撮影するアイテムを指定してください。", "Choose an item from the Hierarchy." },
            { "ツールのインストール先を確認できませんでした。", "Could not locate the tool installation." },
            { "レイアウトを読み込めませんでした。", "Could not load the window layout." },
            { "撮影アイテム配下のSMRまたはMRを指定してください。", "Choose an SMR or MR within the capture item." },
            { "Project内のアセットではなく、Hierarchyのオブジェクトを指定してください。", "Choose a Hierarchy object, not an asset in the Project window." },
            { "保存先を自動判定できませんでした。保存先を指定してください。", "Could not determine an output folder. Choose one manually." },
            { "Sceneビューと同期中です。Inspectorでの変更もプレビューへ反映します。", "Synced with Scene view. Inspector changes also update the preview." },
            { "重複しないプリセット名を入力してください。", "Enter a unique preset name." },
            { "追加プリセットを登録しました。", "Custom preset registered." },
            { "標準プリセットは削除できません。", "Built-in presets cannot be deleted." },
            { "追加プリセットを削除しました。現在の設定値は維持しています。", "Custom preset deleted. Current settings are preserved." },
            { "（連番）", " (numbered)" },
            { "PNGの保存先", "Choose PNG output folder" },
            { "保存先はまだありません。PNG保存時に作成します。", "The output folder does not exist yet. Saving a PNG creates it." },
            { "撮影アイテムとSceneビューを確認してください。", "Check the capture item and Scene view." },
            { "画像を撮影しています…", "Capturing image..." },
            { "PNGを保存しています…", "Saving PNG..." },
            { "保存しました：{0}", "Saved: {0}" },
            { "プレビューを更新できません。「再読み込み」を試してください。\n{0}", "Could not update the preview. Try Reload.\n{0}" },
            { "{0} ｜ メッシュ {1} ｜ {2}", "{0} | Meshes: {1} | {2}" },
            { "Play中", "Play mode" },
            { "Edit中", "Edit mode" },
            { "モード切替後、Hierarchyから撮影対象を指定してください。NDMFビルド後のオブジェクトも指定できます。", "After switching modes, choose the capture item from the Hierarchy. Objects processed by NDMF are supported." },
            { "保存先は、このプロジェクトのAssets内を指定してください。", "Choose an output folder inside this project's Assets folder." },
            { "保存先を指定してください。このプロジェクトのAssets内へ保存できます。", "Choose an output folder inside this project's Assets folder." },
            { "保存名・フォルダ名に使用できない文字が含まれています。", "The file or folder name contains invalid characters." },
            { "Windowsの予約名は保存名・フォルダ名に使用できません。", "Windows reserved names cannot be used for files or folders." },
            { "保存名は接頭辞を含めて160文字以内で指定してください。", "The file name, including its prefix, must be at most 160 characters." },
            { "PNGを生成できませんでした。", "Could not encode the PNG." },
            { "保存画像のインポーターを取得できませんでした。", "Could not get the saved image importer." },
            { "出力解像度は1〜16384pxで指定してください。", "Output dimensions must be between 1 and 16384 pixels." },
            { "撮影倍率は1〜8xで指定してください。", "Supersampling must be between 1x and 8x." },
            { "内部撮影サイズ {0}×{1} が、このPCの上限 {2}px を超えています。", "The internal capture size {0}x{1} exceeds this PC's limit of {2} pixels." },
            { "撮影用シェーダーが利用できません。", "The capture shader is unavailable." },
            { "MITライセンスです。個人・商用利用、改変、再配布が可能です。再配布時は著作権表示とライセンス文を保持してください。無保証のため、ご自身の責任でご利用ください。第三者アセットは各権利者の規約に従ってください。生成した画像へのクレジットは不要です。個別サポートや更新の継続は保証していません。", "MIT License. Personal and commercial use, modification and redistribution are allowed. Retain copyright and license notices when redistributing. Provided without warranty; use at your own risk. Follow third-party asset terms. No credit is required for generated images. Individual support and continued updates are not guaranteed." },
            { "Hierarchyのオブジェクトを指定してください。自身と子孫のSMR・MRを撮影できます。元オブジェクトの設定は変更しません。", "Choose a Hierarchy object. Capture its SMRs and MRs, including descendants, without editing the source." },
            { "Hierarchyの現在の選択を撮影対象にします。指定後に選択を変えても、撮影対象は維持します。", "Use the current Hierarchy selection. Later selection changes do not change the capture item." },
            { "現在の対象からプレビューを作り直します。元オブジェクトは変更しません。", "Rebuild the preview from the current item without editing the source." },
            { "表示言語、接頭辞、連番、強制保存先、追加プリセットを設定できます。", "Set the display language, prefix, numbering, folder override and custom presets." },
            { "左・中ドラッグで平行移動、右ドラッグで対象中心の周回、ホイールで距離を調整できます。Sceneビューの位置・向きも同期します。", "Left or middle drag to pan, right drag to orbit the item center, and scroll to zoom. Scene view position and rotation stay synced." },
            { "見る方向を維持し、撮影対象全体が収まる位置へ移動します。", "Keep the viewing direction and move to fit the entire item." },
            { "シーンのDirectional Lightと、カメラ基準の専用固定照明を切り替えます。元のライトは変更しません。", "Switch between scene Directional Lights and camera-relative fixed lighting. Source lights are unchanged." },
            { "標準または登録済みプリセットを選択できます。撮影設定を変更するとカスタムへ切り替わります。カスタムを手動選択した場合は値を維持します。", "Choose a built-in or saved preset. Editing capture settings selects Custom. Selecting Custom manually keeps all values." },
            { "保存するPNGの最終的な幅を指定してください。撮影時は倍率を掛け、保存時にこの幅へ縮小します。", "Final PNG width. Render at the selected multiplier, then downsample to this width." },
            { "保存するPNGの最終的な高さを指定してください。撮影時は倍率を掛け、保存時にこの高さへ縮小します。", "Final PNG height. Render at the selected multiplier, then downsample to this height." },
            { "保存画像のUnityインポート設定を選択できます。アイコン用・サムネイル用はMip Mapなしで保存します。変更なしでは既存の設定を維持し、新規画像にはUnityの初期設定を使用します。", "Icon and Thumbnail disable mipmaps. Unchanged preserves existing import settings and uses Unity defaults for new images." },
            { "各辺を1〜8倍で撮影し、指定解像度へ縮小します。倍率が大きいほどGPUメモリと処理時間が必要になります。プレビューの解像度は変わりません。", "Render each dimension at 1x-8x and downsample. Higher values need more GPU memory and time. Preview resolution is unchanged." },
            { "オンにすると透過PNGを保存します。市松模様は保存画像に含みません。オフの場合はSolidまたはSkyboxを使用します。", "Save a transparent PNG. The checkerboard is not included. When disabled, use Solid or Skybox." },
            { "オンにすると指定対象だけを表示します。オフの場合は同じSceneのメッシュも表示します。Directional Lightは別Sceneも参照します。", "Show only the selected item when enabled; otherwise include meshes from its Scene. Directional Lights may come from other Scenes." },
            { "35mm判の縦24mmを基準に、85〜200mmで指定できます。プレビュー専用の設定で、SceneビューのFOVは変更しません。", "Set 85-200 mm using a 24 mm vertical sensor reference. Applies only to the preview; Scene view FOV is unchanged." },
            { "平行投影へ切り替えます。オンの場合は焦点距離が無効になり、ホイールで表示範囲を調整できます。", "Use orthographic projection. Focal length is disabled; scroll to adjust the visible area." },
            { "透過オフ時の背景を選択できます。Solidは単色、SkyboxはアクティブSceneのSkyboxを使用します。", "Background for opaque captures. Solid uses a color; Skybox uses the active Scene's skybox." },
            { "Solid背景の色を指定できます。透過オン時とSkybox選択時は使用しません。", "Solid background color. Unused when transparency or Skybox is selected." },
            { "保存名の先頭に接頭辞を付けます。初期状態はオフです。", "Add a prefix to the file name. Disabled by default." },
            { "接頭辞の文面を入力してください。初期値はIA_です。接頭辞がオンの場合だけ保存名に追加します。", "Prefix text, initially IA_. Added only when Use prefix is enabled." },
            { "同名ファイルがある場合、_001、_002の順で空き番号へ保存します。オフの場合は確認なしで上書きします。", "If a file exists, use the next free _001, _002, etc. Disabled means overwrite without confirmation." },
            { "オンにすると、対象や基準メッシュを変更しても、この設定の保存先を優先します。", "Always use the override folder, even when the item or source mesh changes." },
            { "共通の保存先を、このプロジェクトのAssets内で指定してください。存在しないフォルダは保存時に作成します。", "Shared output folder inside this project's Assets. Missing folders are created when saving." },
            { "強制保存先を、このプロジェクトのAssets内から選択できます。", "Choose the override folder inside Assets." },
            { "登録する追加プリセットの名前を入力してください。既存の名前は使用できません。", "Name for a custom preset. Existing names cannot be reused." },
            { "解像度・インポート設定・倍率・背景透過・他Object非表示を登録します。構図・保存先・照明は含みません。", "Save dimensions, import mode, supersampling, transparency and isolation. Camera pose, folders and lighting are excluded." },
            { "選択中の追加プリセットだけを削除します。現在の撮影値は維持します。標準プリセットは削除できません。", "Delete only the selected custom preset. Current capture values are kept. Built-in presets cannot be deleted." },
            { "Material 0のBase Color／Albedoテクスチャから、既定の_icon保存先を探すためのメッシュを指定できます。", "Mesh used to locate the default _icon folder from Material 0's Base Color or Albedo texture." },
            { "PNGの保存名を入力してください。接頭辞がオンの場合は先頭に追加します。同名保存は、連番がオフの場合に確認なしで上書きします。", "PNG file name. The prefix is added when enabled. Existing files are overwritten without confirmation unless numbering is enabled." },
            { "PNGを保存するAssets内のフォルダを指定してください。存在しない場合は保存時に作成します。元素材と同じパス・名前を指定すると元画像も上書きされます。", "Output folder inside Assets; created on save. Choosing the same path and name as a source image overwrites that image." },
            { "PNGの保存先を、このプロジェクトのAssets内から選択できます。", "Choose the PNG output folder inside Assets." },
            { "基準メッシュのテクスチャと同じ場所にある_iconへ、保存先を戻します。", "Reset the folder to _icon beside the source mesh's texture." },
            { "現在のプレビューの構図でPNGを保存し、インポート設定を適用します。同名ファイルは、連番がオフの場合に確認なしで上書きします。", "Save a PNG using the current preview framing and apply import settings. Existing files are overwritten without confirmation when numbering is off." },
            { "保存先をProjectウィンドウでハイライトします。この操作ではフォルダを作成しません。", "Highlight the output folder in the Project window. Does not create the folder." },
            { "日本語表示へ切り替えます。撮影対象・撮影設定・保存先は維持します。", "Switch to Japanese while preserving the item, capture settings and output folder." },
            { "英語表示へ切り替えます。撮影対象・撮影設定・保存先は維持します。", "Switch to English while preserving the item, capture settings and output folder." },
            { "このツールのGitHubリポジトリを開きます。", "Open this tool's GitHub repository." },
            { "不具合報告・連絡用のGitHub Issuesを開きます。", "Open GitHub Issues for bug reports and contact." },
            { "このツールのMITライセンス全文を開きます。", "Open the full MIT license for this tool." },
            { "設定ウィンドウを閉じます。変更済みの設定は維持します。", "Close Settings. Keep changes already applied." },
        };
        private static readonly Dictionary<string, string> JapaneseText = EnglishText.GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.First().Key);
        private static readonly Dictionary<string, string> Tips = new Dictionary<string, string>
        {
            { "gridField", "プレビューに構図ガイドを表示します。保存画像には入りません。" },
            { "shapeField", "保存画像の形状を選択できます。切り抜きの外側は透過します。円は画像の短辺に合わせます。" },
            { "maskField", "白を残し、黒を透過します。中間色は半透明になります。画像全体に合わせてマスクを適用します。" },
            { "shadowField", "背景透過がオンのときに、被写体の右下へ柔らかい影を付けます。影は画像の範囲内に収まります。" },
            { "licenseDetails", "利用条件と免責事項を確認できます。" },
            { "targetField", "Hierarchyのオブジェクトを指定してください。自身と子孫のSMR・MRを撮影できます。元オブジェクトの設定は変更しません。" },
            { "selectedButton", "Hierarchyの現在の選択を撮影対象にします。指定後に選択を変えても、撮影対象は維持します。" },
            { "reloadButton", "現在の対象からプレビューを作り直します。元オブジェクトは変更しません。" },
            { "settingsButton", "表示言語、接頭辞、連番、強制保存先、追加プリセットを設定できます。" },
            { "previewImage", "左・中ドラッグで平行移動、右ドラッグで対象中心の周回、ホイールで距離を調整できます。Sceneビューの位置・向きも同期します。" },
            { "frameButton", "見る方向を維持し、撮影対象全体が収まる位置へ移動します。" },
            { "lightingButton", "シーンのDirectional Lightと、カメラ基準の専用固定照明を切り替えます。元のライトは変更しません。" },
            { "presetField", "標準または登録済みプリセットを選択できます。撮影設定を変更するとカスタムへ切り替わります。カスタムを手動選択した場合は値を維持します。" },
            { "widthField", "保存するPNGの最終的な幅を指定してください。撮影時は倍率を掛け、保存時にこの幅へ縮小します。" },
            { "heightField", "保存するPNGの最終的な高さを指定してください。撮影時は倍率を掛け、保存時にこの高さへ縮小します。" },
            { "importField", "保存画像のUnityインポート設定を選択できます。アイコン用・サムネイル用はMip Mapなしで保存します。変更なしでは既存の設定を維持し、新規画像にはUnityの初期設定を使用します。" },
            { "samplingField", "各辺を1〜8倍で撮影し、指定解像度へ縮小します。倍率が大きいほどGPUメモリと処理時間が必要になります。プレビューの解像度は変わりません。" },
            { "transparentField", "オンにすると透過PNGを保存します。市松模様は保存画像に含みません。オフの場合はSolidまたはSkyboxを使用します。" },
            { "hideOthersField", "オンにすると指定対象だけを表示します。オフの場合は同じSceneのメッシュも表示します。Directional Lightは別Sceneも参照します。" },
            { "focalField", "35mm判の縦24mmを基準に、85〜200mmで指定できます。プレビュー専用の設定で、SceneビューのFOVは変更しません。" },
            { "orthoField", "平行投影へ切り替えます。オンの場合は焦点距離が無効になり、ホイールで表示範囲を調整できます。" },
            { "backgroundField", "透過オフ時の背景を選択できます。Solidは単色、SkyboxはアクティブSceneのSkyboxを使用します。" },
            { "colorField", "Solid背景の色を指定できます。透過オン時とSkybox選択時は使用しません。" },
            { "prefixEnabled", "保存名の先頭に接頭辞を付けます。初期状態はオフです。" },
            { "prefixText", "接頭辞の文面を入力してください。初期値はIA_です。接頭辞がオンの場合だけ保存名に追加します。" },
            { "numberingField", "同名ファイルがある場合、_001、_002の順で空き番号へ保存します。オフの場合は確認なしで上書きします。" },
            { "forceFolderField", "オンにすると、対象や基準メッシュを変更しても、この設定の保存先を優先します。" },
            { "forcedFolderField", "共通の保存先を、このプロジェクトのAssets内で指定してください。存在しないフォルダは保存時に作成します。" },
            { "forcedBrowseButton", "強制保存先を、このプロジェクトのAssets内から選択できます。" },
            { "presetName", "登録する追加プリセットの名前を入力してください。既存の名前は使用できません。" },
            { "addPresetButton", "解像度・インポート設定・倍率・背景透過・他Object非表示を登録します。構図・保存先・照明は含みません。" },
            { "deletePresetButton", "選択中の追加プリセットだけを削除します。現在の撮影値は維持します。標準プリセットは削除できません。" },
            { "basisField", "Material 0のBase Color／Albedoテクスチャから、既定の_icon保存先を探すためのメッシュを指定できます。" },
            { "saveName", "PNGの保存名を入力してください。接頭辞がオンの場合は先頭に追加します。同名保存は、連番がオフの場合に確認なしで上書きします。" },
            { "saveFolder", "PNGを保存するAssets内のフォルダを指定してください。存在しない場合は保存時に作成します。元素材と同じパス・名前を指定すると元画像も上書きされます。" },
            { "browseButton", "PNGの保存先を、このプロジェクトのAssets内から選択できます。" },
            { "defaultFolderButton", "基準メッシュのテクスチャと同じ場所にある_iconへ、保存先を戻します。" },
            { "saveButton", "現在のプレビューの構図でPNGを保存し、インポート設定を適用します。同名ファイルは、連番がオフの場合に確認なしで上書きします。" },
            { "highlightButton", "保存先をProjectウィンドウでハイライトします。この操作ではフォルダを作成しません。" },
            { "japaneseButton", "日本語表示へ切り替えます。撮影対象・撮影設定・保存先は維持します。" },
            { "englishButton", "英語表示へ切り替えます。撮影対象・撮影設定・保存先は維持します。" },
            { "githubButton", "このツールのGitHubリポジトリを開きます。" },
            { "issuesButton", "不具合報告・連絡用のGitHub Issuesを開きます。" },
            { "licenseButton", "このツールのMITライセンス全文を開きます。" },
            { "closeSettingsButton", "設定ウィンドウを閉じます。変更済みの設定は維持します。" },
        };
        private static string PreferenceKey => "TY.FXItemAssistant.English." + Application.dataPath;
        public static event Action Changed;
        public static bool English
        {
            get => EditorPrefs.GetBool(PreferenceKey, false);
            set
            {
                if (English == value) return;
                EditorPrefs.SetBool(PreferenceKey, value);
                Changed?.Invoke();
            }
        }

        public static string Text(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (JapaneseText.TryGetValue(value, out var original)) value = original;
            return English && EnglishText.TryGetValue(value, out var translated) ? translated : value;
        }

        public static string Format(string key, params object[] arguments) => string.Format(CultureInfo.CurrentCulture, Text(key), arguments);

        public static string PresetName(string name) => ImagePresets.BuiltInNames.Contains(name) ? Text(name) : name;

        public static bool IsBuiltInPresetName(string name) => ImagePresets.BuiltInNames
            .Any(key => key == name || EnglishText.TryGetValue(key, out var translated) && translated == name);

        public static void Translate(VisualElement root)
        {
            if (root == null) return;
            if (Tips.TryGetValue(root.name ?? "", out var tip)) root.tooltip = Text(tip);
            if (root.ClassListContains("unity-base-field"))
            {
                var labelProperty = root.GetType().GetProperty("label");
                if (labelProperty != null && labelProperty.CanWrite && labelProperty.PropertyType == typeof(string))
                    labelProperty.SetValue(root, Text((string)labelProperty.GetValue(root)));
                return;
            }
            if (root is Button button) button.text = Text(button.text);
            else if (root is Label label) label.text = Text(label.text);
            else if (root is Foldout foldout) foldout.text = Text(foldout.text);
            else if (root is HelpBox help) { help.text = Text(help.text); return; }
            foreach (var child in root.Children().ToArray()) Translate(child);
        }
    }
}
