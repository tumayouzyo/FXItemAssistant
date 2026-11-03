# FX Item Assistant
![Unity](https://img.shields.io/badge/Unity-2022.3-222c37?logo=unity)
![VRChat](https://img.shields.io/badge/VRChat-Menu_Icons-19b9e7)
![License](https://img.shields.io/badge/License-MIT-green)

**責任者:** [tumayouzyo](https://github.com/tumayouzyo)  
**作成者:** Codex

VRChatのExpression Menu用アイコン、デカール素材、サムネイルをPNGへ撮影するUnity Editor拡張。

**メニュー:** `Tools > FXItemAssistant`

## インストール

1. [Releases](https://github.com/tumayouzyo/FXItemAssistant/releases/latest) から `FXItemAssistant_*.unitypackage` をダウンロードしてImport。
2. `Tools > FXItemAssistant` を開く。

インポート先は `Assets/TY_ImageAssistant`。

## 基本操作

1. Hierarchyから「撮影アイテム」へドラッグ、または「選択中を指定」で対象を指定。
2. プリセットを選び、プレビューで構図・照明・背景を調整。
3. 保存名・保存先を確認して「PNGを保存」を押す。

| プリセット | 最終解像度 | 初期倍率 | 背景透過 | 他Object非表示 |
| --- | ---: | ---: | --- | --- |
| 規定（ExpMenu） | 256×256 | 2x | オン | オン |
| 素材 | 4096×4096 | 2x | オン | オン |
| サムネイル | 1200×900 | 2x | オフ | オフ |

撮影倍率は1x〜8x。指定倍率で撮影し、最終解像度へ縮小。撮影設定を編集するとカスタムへ切り替わる。カスタムの手動選択では値を維持。

| 操作 | 動作 |
| --- | --- |
| 左・中ドラッグ | 平行移動 |
| 右ドラッグ | アイテム中心を基準に周回 |
| ホイール | 距離調整。平行投影中は表示範囲を調整 |
| 全体を収める | 見る方向を維持して対象全体へ移動 |
| 設定 | 接頭辞・連番・強制保存先・追加プリセット |

Sceneビューと位置・向きを同期。FOVは変更せず、撮影用の焦点距離を85〜200mmで調整。透過PNG、Solid／Skybox背景、専用固定照明に対応。

## 保存先

Material 0のBase Color／Albedoテクスチャと同じフォルダ内の `_icon` が既定。自動判定できない場合はAssets内のフォルダを指定。フォルダは保存時に作成。

**同名PNGは確認なしで上書きし、インポート設定も更新。** 元素材と同じパス・名前を指定すると元画像も上書きされる。連番保存は設定から有効化。

通常の撮影設定は次回起動へ持ち越さず、登録した追加プリセットだけをPC・プロジェクト別に保存。

## 対応環境・制約

- 確認環境：Unity 2022.3.22f1／Built-in Render Pipeline／Windows DX11。
- SMR・MRに対応。元のTransform・Active・メッシュ・マテリアル・ボーンは編集しない。
- VRChat SDK・Modular Avatar・NDMFへの直接依存なし。Play中はNDMF処理後の実体を指定可能。Play開始・終了後は対象の再指定が必要。
- 他Object表示は対象と同じSceneのメッシュのみ。Directional Lightは別Sceneも参照。
- Terrain・ParticleSystem・Canvas・ポストプロセス・ライトマップ・Reflection Probeの再現は対象外。背景依存の特殊シェーダーは透過結果が異なる場合あり。
- 大きな倍率にはGPUメモリが必要。描画上限を超える組み合わせは保存不可。
- VRChat実機・Android実機の画質は未検証。

詳細は[操作マニュアル](Documentation~/manual.md)、[検証内容](Documentation~/validation.md)、[開発手順](CONTRIBUTING.md)を参照。

## 不具合報告

[Issues](https://github.com/tumayouzyo/FXItemAssistant/issues) へ、環境・再現手順・期待結果・実際の結果・関連ログを記載。

## ライセンス

[MIT License](LICENSE)。個人・商用利用、改変、再配布可。再配布時は著作権表示とライセンス文を保持。第三者アセットは各権利者の規約に従う。生成画像へのクレジット不要。無保証。個別サポート・継続的な更新の保証なし。
