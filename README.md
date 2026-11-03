# FX Item Assistant
![Unity](https://img.shields.io/badge/Unity-2022.3-222c37?logo=unity)
![VRChat](https://img.shields.io/badge/VRChat-Menu_Icons-19b9e7)
![License](https://img.shields.io/badge/License-MIT-green)

**責任者:** [tumayouzyo](https://github.com/tumayouzyo)  
**作成者:** Codex

VRChatのExpression Menu用アイコンやデカール素材、サムネイルを撮影するUnity Editor拡張です。元オブジェクトを編集せず、専用プレビューで構図・照明・背景を調整してPNGへ保存します。

**メニュー:** `Tools > FXItemAssistant`

## できること

- 指定オブジェクトと子孫のSkinnedMeshRenderer・MeshRendererをプレビュー
- 外部ボーンを使う衣装と、PlayモードのNDMF処理後の実体を撮影
- Sceneビューと位置・向きを同期。SceneビューのFOVを変更せず、撮影用の85〜200mm・平行投影を調整
- 透過PNG、Solid／Skybox背景、元のDirectional Light／専用固定照明を切り替え
- 1x〜8xで撮影し、指定した最終解像度へ縮小
- 接頭辞・連番・保存先強制指定、追加プリセットの登録・削除
- 画像用途に合わせたインポート設定。PCはBC7、Android／iOSはASTC 4×4の高品質圧縮

## インストール

1. [Releases](https://github.com/tumayouzyo/FXItemAssistant/releases/latest) から `FXItemAssistant_*.unitypackage` をダウンロードしてImportします。
2. `Tools > FXItemAssistant` を開きます。

インポート先は `Assets/TY_ImageAssistant`。ソースを使う場合は、このリポジトリを同フォルダへcloneするか、Package ManagerのGit URLに `https://github.com/tumayouzyo/FXItemAssistant.git` を指定してください。Assets版とPackage Manager版の二重導入は避けてください。

## 基本操作

1. Hierarchyから「撮影アイテム」へドラッグするか、「選択中を指定」を押します。
2. プリセットを選び、プレビューで構図を調整します。
3. 保存名・保存先を確認して「PNGを保存」を押します。

| プリセット | 最終解像度 | 初期倍率 | 背景透過 | 他Object非表示 |
| --- | ---: | ---: | --- | --- |
| 規定（ExpMenu） | 256×256 | 2x | オン | オン |
| 素材 | 4096×4096 | 2x | オン | オン |
| サムネイル | 1200×900 | 2x | オフ | オフ |
| カスタム | 現在の値を維持 | 現在の値を維持 | 現在の値を維持 | 現在の値を維持 |

プリセットの撮影設定を編集すると自動でカスタムへ切り替わります。カスタムを手動選択しても値は変わりません。

| 操作 | 動作 |
| --- | --- |
| 左・中ドラッグ | 平行移動 |
| 右ドラッグ | アイテム中心を基準に周回 |
| ホイール | 距離調整。平行投影中は表示範囲を調整 |
| 全体を収める | 現在の見る方向を維持して対象全体へ移動 |
| 設定 | 接頭辞・連番・強制保存先・追加プリセット |

メイン画面の最小幅は420pxです。通常の撮影設定は次回へ持ち越しません。明示的に登録した追加プリセットだけを、このPC・プロジェクト用に保存します。

## 保存先

Material 0のBase Color／Albedoテクスチャと同じ場所にある `_icon` が既定です。自動判定できない場合はAssets内のフォルダを指定してください。フォルダは保存時に作成します。

**同名PNGは確認なしで上書きし、その画像のインポート設定も更新します。** 元のテクスチャと同じパス・名前を指定すると元画像も上書きできます。必要に応じて設定でナンバリング保存を有効にしてください。

## 対応環境と制約

- 確認環境：Unity 2022.3.22f1／Built-in Render Pipeline／Windows DX11。
- Editor専用。VRChat SDK・Modular Avatar・NDMFへの直接依存はありません。
- Play開始・終了時に対象を解除します。ビルド後の実体をHierarchyから再指定してください。
- 「他Object非表示」オフ時のメッシュは、対象と同じSceneだけを表示します。Directional Lightは別Sceneの有効なものも参照します。
- 元のTransform・Active・メッシュ・マテリアル・ボーンは編集しません。Sceneビューの位置・向き・表示距離・平行投影は操作に合わせて変わります。
- Terrain・ParticleSystem・Canvas・ポストプロセス・ライトマップ・Reflection Probeの再現は対象外です。背景依存の特殊シェーダーでは透過結果が異なる場合があります。
- 大きな撮影倍率にはGPUメモリが必要です。描画上限を超える組み合わせは保存できません。
- VRChat実機・Android実機の画質までは検証していません。

詳細は[操作マニュアル](Documentation~/manual.md)、[検証内容](Documentation~/validation.md)を参照してください。

## 開発・不具合報告

不具合は [Issues](https://github.com/tumayouzyo/FXItemAssistant/issues) へ。環境・再現手順・期待する結果・実際の結果・関連ログを添えてください。[開発手順](CONTRIBUTING.md)も参照できます。

## ライセンス

[MIT License](LICENSE)。個人・商用利用、改変、再配布が可能です。再配布時は著作権表示とライセンス文を保持してください。無保証です。第三者アセットは各権利者の規約に従ってください。生成画像へのクレジットは不要です。個別サポートや継続的な更新は保証していません。
