## 概要

VLiveKitの一部として開発している、  
ライブ制作向けカメラシステムです。

ライブらしいカメラワークをUnity上で再現するための  
各種制御機能と補助ツールをまとめています。

---

## 主な機能

### カメラ制御

- Screen / Dutch / Zoom / Dolly などの基本的なカメラ挙動
- ライブ演出向けのカメラモーション制御

---

### プリセット

- カメラ設定の保存・再利用
- ループカメラなどのプリセット化（拡張・カスタマイズ可能）

---

### キャラクター連携

- Humanoidから自動でターゲットを設定
  - 頭部へのLookAt
  - 体幹へのFollow

---

### フォーカス

- AutoFocusによる被写体追従

---

### スイッチング

- RenderTextureベースのフェード
- カメラ切り替え機能

---

### Timeline連携

- Timelineと同期したカメラ制御
- 再現性のあるカメラワークの記録・再生

---

## 開発状況

本パッケージはライブ制作での使用を前提に、  
継続的に調整・改善を行っています。

---

## インストール

`Packages/manifest.json` の `dependencies` に以下を追加してください。

```json
{
  "dependencies": {
    "com.toshi.vlivekit.cameraunit": "https://github.com/toshi-kundesu/VLiveKit_camera.git?path=/Assets/toshi.VLiveKit/VLiveCameraUnit#main"
  }
}
