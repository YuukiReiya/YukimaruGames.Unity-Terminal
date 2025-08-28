# YukimaruGames.Terminal

A powerful, extensible, and runtime command terminal for Unity.

🚀<b>Unityで利用可能なランタイムターミナル</b>🚀
<br>ゲーム中の演出やデバッグ機能の利用にご活用ください。

---
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/YuukiReiya/YukimaruGames.Unity-Terminal)](https://github.com/YukimaruGames.Unity-Terminal/releases)
[![License](https://img.shields.io/github/license/YuukiReiya/YukimaruGames.Unity-Terminal)](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/blob/main/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/YuukiReiya/YukimaruGames.Unity-Terminal)](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/commits/main)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/YuukiReiya/YukimaruGames.Unity-Terminal)](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/pulls)
[![GitHub issues](https://img.shields.io/github/issues/YuukiReiya/YukimaruGames.Unity-Terminal)](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/issues)
[![GitHub stars](https://img.shields.io/github/stars/YuukiReiya/YukimaruGames.Unity-Terminal?style=social)](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/stargazers)
[![GitHub watchers](https://img.shields.io/github/watchers/YuukiReiya/YukimaruGames.Unity-Terminal?style=social)](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/watchers)
---

# ✨Overview

このリポジトリはUnity上で疑似的なターミナルの表示とコマンド入力を可能します。<br>
入力したコマンドに対してメソッドを登録することでデバッグ機能を呼び出したりゲーム中で疑似簡易的なターミナルの表示をお手軽に行う機能を提供するための自作パッケージとなります。

# 🌟Features

-   **🖥️ ランタイム実行:** ゲーム実行中にリアルタイムでコマンドを呼び出し。
-   **✍️ 簡単なコマンド登録:** C#メソッドに`[Register]`属性を付けるだけで、`static`メソッドを自動的にコマンドとして認識。
-   **💪 厳密な型引数:** `int`, `float`, `bool`, `Vector3`など、文字列の引数をメソッドの型に合わせて自動で変換・検証。
-   **🚀 高速な実行:** `Expression Tree`を利用して、リフレクションを使わない高速なデリゲートを動的に生成。
-   **🎮 デュアル入力対応:** Unityの旧Input Managerと新しいInput Systemの両方をサポート。
-   **🎨 高いカスタマイズ性:** `TerminalBootstrapper`コンポーネントを通じて、フォント、色、レイアウト、キーバインドなどをInspectorから柔軟にカスタマイズすることが可能。
-   **🧩 クリーンアーキテクチャ:** レイヤー分離されたクリーンな設計により、高い保守性と拡張性を実現。

---

# ✅Requirements

-   Unity 2021.3 以降
-   Input System パッケージ (任意)

# 📦Installation

UnityPackageManagerを介して以下のGitURLから導入が可能です。

### ⬇️Install via UPM

1.  Unityエディタの上部メニューから `Window > Package Manager` を選択します。
2.  `+` ボタンを押し、`Add package from git URL...` を選択します。
3.  以下のURLを入力し、`Add`を押します。

<b>https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal.git?path=Assets/YukimaruGames/Terminal</b>

## 🚀Getting Started

### 1. シーンにターミナルを追加
-   空のGameObjectを作成します。
-   `TerminalBootstrapper` コンポーネントをアタッチします。
-   Inspector上で、ターミナルの見た目やキーバインドを好みに設定します。

### 2. コマンドを登録
-   プロジェクト内の任意のC#スクリプトに、`public static`なメソッドを作成します。
-   そのメソッドに `[Register]` 属性を付けるだけで、コマンドとして自動的に登録されます。

#### ▼ コマンド定義の例
```csharp
using UnityEngine;
using YukimaruGames.Terminal.Domain.Attribute; // Register属性のために必要

public static class MyCommands
{
    [Register("player.heal", Help = "プレイヤーを指定した量だけ回復させます。")]
    public static void HealPlayer(int amount)
    {
        // プレイヤーを回復させるロジック...
        Debug.Log($"Player healed by {amount} HP.");
    }

    [Register("scene.load", Help = "指定した名前のシーンをロードします。")]
    public static void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
```

# 🧩Usage

## 1. Add Terminal to Your Scene.

## 2. Registering Commands.



## 🤝Contribution

ご意見、ご提案、プルリクエストを歓迎します。

## 📄License

このプロジェクトは[MITライセンス](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/blob/main/LICENSE)の下でライセンスされています。
