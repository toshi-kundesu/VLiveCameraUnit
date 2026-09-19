# VLive Camera Unit

ライブ制作向けのカメラ制御、プリセット、キャラクター追従、Timeline 連携をまとめた Unity package です。

## Package

- Package name: `com.toshi.vlivekit.cameraunit`
- Version: `0.1.8`
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

## カメラサンプルの動画再生

`Samples/Scenes/VLK_CAMERAUNIT.unity` は、Unity 6000.4.6f1 で発生する
映像停止・音声の乱れを回避するため、Video Player の `Skip On Drop` を無効にしています。
[Unity Issue Tracker](https://issuetracker.unity.com/issues/8978/video-player-glitches-when-skip-on-drop-is-enabled)

30fps の同梱動画が 24fps 制限で遅延しないよう、このサンプルの `SceneEssentials` だけ
フレームレートを 60fps に上書きしています。共通 prefab の設定は変更していません。

## 注意

- ライブ運用で調整しながら使う前提のため、preset や rig の構成はプロジェクト側で上書きできます。

## Multi Performer Look Targets

- `VLiveLookTargetRig` can add multiple performer sources from the current scene selection with `Add Selected Performers`.
- `Build Targets` creates one look target per humanoid bone and blends the selected performers with a `PositionConstraint`.
- When `Active Sources Only` is enabled, inactive performers are given zero constraint weight. If two performers are selected and one is disabled, cameras aim only at the active performer.

## License

この package 独自のコードと asset は repository の `LICENSE` に従います。third-party asset を含む場合は、それぞれの license / README を確認してください。
