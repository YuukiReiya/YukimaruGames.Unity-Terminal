# WIP: Animation Config Separation（作業チェックポイント）

> [!IMPORTANT]
> このファイルはコンテキストリセット後の作業復帰用チェックポイントです。
> 作業完了後は削除し、変更内容を `Architecture Overview.md` に反映してください。

## 作業概要

`ITerminalTheme` に混在していた **Animationパラメータ（ウィンドウ動作設定）** を、独立したインターフェース `ITerminalAnimation` として分離するリファクタリング。

## 背景・判断根拠

- `ITerminalTheme` の名前が示す責務は「見た目・配色・フォント」であるが、現状はウィンドウの振る舞い（起動状態・アンカー・アニメーション時間など）も含まれており、**SRP違反**。
- `TerminalStandardInstaller.BuildRenderingContext()` 内で、Animationパラメータは `TerminalWindowAnimatorDataConfigurator` にのみ渡されており、Viewパラメータとは **消費先が明確に分離**されている。
- Themeを差し替えてもウィンドウ位置・動作は変わらないのが自然であり、ユーザー観点でも概念が異なる。

## 対象コードの現状

### `ITerminalTheme` の現プロパティ構成

| カテゴリ | プロパティ |
|---|---|
| Font | `Font`, `FontSize` |
| Colors | `BackgroundColor`, `MessageColor`, `EntryColor`, `WarningColor`, `ErrorColor`, `AssertColor`, `ExceptionColor`, `SystemColor`, `InputColor`, `CaretColor`, `SelectionColor`, `PromptColor`, `ExecuteButtonColor`, `ButtonColor`, `CopyButtonColor` |
| Cursor | `CursorFlashSpeed` |
| **Animation（分離対象）** | `BootupWindowState`, `Anchor`, `WindowStyle`, `Duration`, `CompactScale` |

## 変更計画

### 新規作成ファイル

| ファイル | 場所 | 内容 |
|---|---|---|
| `ITerminalAnimation.cs` | `Runtime/Runtime/Interface/` | Animationパラメータのインターフェース定義 |
| `TerminalStandardAnimation.cs` | `Runtime/Runtime/Configuration/` | 標準実装（デフォルト値あり） |
| `TerminalNullAnimation.cs` | `Runtime/Runtime/Configuration/` | Null Objectパターン実装 |
| `TerminalStandardAnimation.editor.cs` | `Editor/Configuration/` | Editor拡張（PropertyDrawer） |

### 変更ファイル

| ファイル | 変更内容 |
|---|---|
| `ITerminalTheme.cs` | `BootupWindowState`, `Anchor`, `WindowStyle`, `Duration`, `CompactScale` を削除 |
| `TerminalStandardTheme.cs` | 上記フィールド・プロパティを削除 |
| `TerminalNullTheme.cs` | 上記プロパティを削除 |
| `TerminalStandardInstaller.cs` | `_animation` フィールド（`ITerminalAnimation`）を追加。`BuildRenderingContext()` の引数に追加 |
| `TerminalStandardTheme.editor.cs` | Animationタブの描画を `TerminalStandardAnimationDrawer` に移管（Animationタブ自体をTheme Drawerから除去） |

### `ITerminalAnimation` 定義（予定）

```csharp
public interface ITerminalAnimation
{
    TerminalState BootupWindowState { get; }
    TerminalAnchor Anchor { get; }
    TerminalWindowStyle WindowStyle { get; }
    float Duration { get; }
    float CompactScale { get; }
}
```

### `TerminalStandardInstaller` 変更後のフィールド（予定）

```csharp
[SerializeReference, SerializeInterface]
private ITerminalTheme _theme = new TerminalStandardTheme();

[SerializeReference, SerializeInterface]
private ITerminalAnimation _animation = new TerminalStandardAnimation();  // NEW

[SerializeReference, SerializeInterface]
private ITerminalOptions _options = new TerminalStandardOptions();
```

## 作業ステータス

- [x] 現状コード解析
- [x] 分離方針の合意 (ユーザー承認済み)
- [x] チェックポイント作成
- [ ] `ITerminalAnimation.cs` 新規作成
- [ ] `TerminalStandardAnimation.cs` 新規作成
- [ ] `TerminalNullAnimation.cs` 新規作成
- [ ] `ITerminalTheme.cs` からAnimationプロパティを削除
- [ ] `TerminalStandardTheme.cs` からAnimationフィールドを削除
- [ ] `TerminalNullTheme.cs` からAnimationプロパティを削除
- [ ] `TerminalStandardInstaller.cs` に `_animation` フィールドを追加・`BuildRenderingContext()` を修正
- [ ] `TerminalStandardTheme.editor.cs` のAnimationタブを除去
- [ ] `TerminalStandardAnimation.editor.cs` 新規作成（PropertyDrawer）
- [ ] コンパイルチェック（`.bat/compile-test/run.bat`）
- [ ] ユーザーレビュー依頼
- [ ] PRの作成

## 関連ファイルパス（参照用）

```
Assets/YukimaruGames/Terminal/
├── Runtime/Runtime/
│   ├── Interface/
│   │   ├── ITerminalTheme.cs
│   │   ├── ITerminalAnimation.cs        ← NEW
│   │   └── ITerminalInstaller.cs
│   ├── Configuration/
│   │   ├── TerminalStandardTheme.cs
│   │   ├── TerminalNullTheme.cs
│   │   ├── TerminalStandardAnimation.cs ← NEW
│   │   └── TerminalNullAnimation.cs     ← NEW
│   └── Installer/
│       └── TerminalStandardInstaller.cs
└── Editor/Configuration/
    ├── TerminalStandardTheme.editor.cs
    └── TerminalStandardAnimation.editor.cs ← NEW
```
