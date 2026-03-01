## 概要

`ITerminalTheme` の責務が「見た目（色・フォント）」と「動作（ウィンドウ状態・アニメーション）」で混在していた問題を解決するため、Animation パラメータを `ITerminalAnimation` として分離しました。

## 背景・経緯

<details>
<summary>問題点と分離の判断</summary>

- `ITerminalTheme` は名前の通り「テーマ（色・フォント）」を担うはずが、`BootupWindowState` や `Anchor` といったウィンドウの挙動まで保持しており、**単一責任原則（SRP）** に違反していました。
- `TerminalStandardInstaller.BuildRenderingContext()` でも、Animationパラメータは `TerminalWindowAnimatorDataConfigurator` へ、Viewパラメータは `ColorPaletteProvider` などへと明確に消費先が分かれており、実装上も分離を促す構造になっていました。
- ユーザーにとって「テーマを切り替える」ことと「起動時の位置（Anchor）を変える」ことは別の概念であるため、設定インターフェースを分離する判断に至りました。

</details>

## 変更点

- `ITerminalAnimation.cs`
  - 新規作成。`BootupWindowState`, `Anchor`, `WindowStyle`, `Duration`, `CompactScale` を定義
- `TerminalStandardAnimation.cs` / `TerminalNullAnimation.cs`
  - 新規作成。標準実装および Null Object パターンの実装を追加
- `TerminalStandardAnimation.editor.cs`
  - 新規作成。PropertyDrawerとして、従来の ThemeDrawer の Animation タブ相当のUIを独立して実装
- `ITerminalTheme.cs`
  - Animation 関連のプロパティを削除
- `TerminalStandardTheme.cs` / `TerminalNullTheme.cs`
  - Animation 関連の実装を削除
- `TerminalStandardInstaller.cs`
  - `_animation` フィールドを追加
  - `BuildRenderingContext` で Theme ではなく Animation インターフェースから設定値を渡すよう修正
- `TerminalStandardTheme.editor.cs`
  - タブ切り替え（View / Animation）を廃止し、常に View 設定のみを描画するよう修正
- `.agent/WIP_AnimationConfig_Separation.md`
  - タスク完了に伴い削除

## レビューポイント

- [ ] 設定ファイルのインスペクターでの表示（`TerminalStandardTheme` および `TerminalStandardAnimation`）が意図通り分離されているか
- [ ] ターミナルの起動位置・アニメーション動作にリグレッションがないか

## 検証結果

- [x] コンパイルチェック（`.bat/compile-test/run.bat`）をパス

## 目的

クラス・インターフェースの責務を明確にし、拡張性や保守性を向上させるため。

## 影響範囲

- ターミナルのブートストラップ設定において、新たに `Animation` の項目が独立して設定可能になります。
- 既存の Theme をカスタマイズしていた場合、Animation 項目が外出しされるため再設定が必要になる可能性があります。

## 備考

なし
