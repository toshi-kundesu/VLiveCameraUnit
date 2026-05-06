# VLive Camera Unit

ライブ制作向けのカメラ制御、プリセット、キャラクター追従、Timeline 連携をまとめた Unity package です。

## Package

- Package name: `com.toshi.vlivekit.cameraunit`
- Version: `0.0.6`
- Unity: 2022.3
- Repository: https://github.com/toshi-kundesu/VLiveKit_camera
- Package root: `Assets/toshi.VLiveKit/VLiveCameraUnit`

## 主な内容

- Screen / Dutch / Zoom / Dolly などのカメラ操作
- LookAt / Follow などキャラクター連動の撮影補助
- AutoFocus、カメラスイッチング、Timeline 連携

## 依存・同梱 asset

- Cinemachine 2.9.7

## インストール

Unity の `Packages/manifest.json` の `dependencies` に追加します。

```json
{
  "dependencies": {
    "com.toshi.vlivekit.cameraunit": "https://github.com/toshi-kundesu/VLiveKit_camera.git?path=/Assets/toshi.VLiveKit/VLiveCameraUnit#main"
  }
}
```

VLiveKit sandbox では submodule として `Packages/VLiveKit_camera` に配置し、`file:` 参照で読み込んでいます。

## 注意

- ライブ運用で調整しながら使う前提のため、preset や rig の構成はプロジェクト側で上書きできます。

## License

この package 独自のコードと asset は repository の `LICENSE` に従います。third-party asset を含む場合は、それぞれの license / README を確認してください。
