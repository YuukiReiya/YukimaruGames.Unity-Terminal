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
-   **✍️ 柔軟なコマンド登録:**
    -   <b>自動登録</b>: C#のstaticメソッドに[Register]属性を付けるだけで、コマンドとして自動認識。
    -   <b>手動登録</b>: staticでないインスタンスメソッドも、公開APIを通じて柔軟に登録可能。
-   **⌨️ 入力補助機能:**
    -   <b>コマンド履歴</b>: 実行したコマンドを上下矢印キーで簡単に再呼び出し。
    -   <b>自動補完</b>: `Tab`キーによるコマンド名の自動補完。<br>※キーはカスタマイズ可能。
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

```markdown
https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal.git?path=Assets/YukimaruGames/Terminal
```

## 🧩Getting Started

### 1. Add Terminal to Your Scene.
-   空のGameObjectを作成します。
-   `TerminalBootstrapper` コンポーネントをアタッチします。
-   Inspector上で、ターミナルの見た目やキーバインドを好みに設定します。

### 2. Registering Commands.
-   プロジェクト内の任意のC#スクリプトに、`public static`なメソッドを作成します。
-   そのメソッドに `[Register]` 属性を付けるだけで、コマンドとして自動的に登録されます。

#### ▼ コマンド定義の例

##### a. `static`メソッドの自動登録
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

##### b. `instance`メソッドの手動登録

```cs
using UnityEngine;
using YukimaruGames.Terminal.Domain.Model;
// ...

public class MyPlayer : MonoBehaviour
{
    // Bootstrapperの参照をInspectorなどから設定
    public TerminalBootstrapper Terminal;

    // これはインスタンスメソッド
    public void Heal(int amount)
    {
        Debug.Log($"{this.name} healed by {amount}");
    }

    void Start()
    {
        // 1. 登録したいメソッドからデリゲートを作成
        System.Action<int> healDelegate = this.Heal;

        // 2. コマンドのメタデータを定義
        var meta = new CommandMeta("heal_me", "このプレイヤーを回復させます。", 1, 1);
        
        // 3. Bootstrapperの公開APIを使って登録
        Terminal.RegisterCommand(healDelegate, meta);
    }
}
```

## 🤝Contribution

ご意見、ご提案、プルリクエストを歓迎します。

## 📄License

このプロジェクトは[MITライセンス](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/blob/main/LICENSE)の下でライセンスされています。
