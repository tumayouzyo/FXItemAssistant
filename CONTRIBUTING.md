# 開発について

Unity 2022.3の検証用プロジェクトで作業してください。Assets版とPackage Manager版を同時に導入しないでください。

## テスト

Unity Test Frameworkを導入し、Test Runnerで `TY.ImageAssistant.Editor.Tests` のEditModeテストを実行します。Package Manager版では `Packages/manifest.json` の `testables` に `io.github.tumayouzyo.fx-item-assistant` を追加してください。

テストは専用プレビューシーンと、一意の名前を持つAssets内の一時フォルダを使用します。一時アセットは終了時に削除します。Test Runnerの保存確認を避けるため、検証用プロジェクトで未保存のシーン編集を残さずに実行してください。

## 変更の方針

- UI・撮影・保存・設定の責務を分けます。
- 元オブジェクト、共有メッシュ・マテリアル・ボーンを編集しません。
- Editor専用コードはEditorアセンブリへ置きます。
- コメントは、コードだけでは分かりにくい理由や制約に絞ります。
- 不具合修正は再現手順と回帰テストを用意し、修正範囲に合う検証を行います。
- PNG上書きの仕様とGUIDの維持を確認します。

## 報告

Issuesへ環境・再現手順・期待結果・実際の結果・関連するConsoleログを記載してください。ログや画像に個人情報、認証情報、再配布できない素材を含めないでください。個別サポートや修正時期は保証していません。
