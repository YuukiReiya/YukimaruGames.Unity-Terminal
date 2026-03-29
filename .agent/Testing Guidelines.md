# Testing Guidelines

このドキュメントは、プロジェクトにおけるテストの方針、種類、および実施ルールを定義します。

## 1. 基本方針

- **依存性の排除 (Zero Extra Dependency)**: UPMパッケージとしての頒布を目的としているため、NSubstitute などの外部モッキングライブラリには依存させず、C# 標準および Unity 標準 (NUnit / UnityEngine.TestTools) の機能のみを使用します。
- **Mock/Stub の作成**: インターフェースに基づいた手動の Mock クラスや Stub クラスを作成してテストを構成します。
- **テスト対象の優先順位**:
  1. `Domain.Core`: ビジネスロジックの正当性。
  2. `Application`: 各機能のオーケストレーション。
  3. `Infrastructure/UI`: 外部依存やUnity APIとの統合（必要最小限）。

## 2. テストの種類

### EditMode Tests
- **用途**: Unity のランタイム（PlayMode）を必要としない純粋なロジックのテスト。
- **配置**: `Assets/YukimaruGames/Terminal/Tests/Editor/`
- **特徴**: 高速に実行可能。大部分のドメインロジックはこのモードでテストします。

### PlayMode Tests
- **用途**: Unity のライフサイクル（Awake/Update 等）、コルーチン、物理演算、UI の実挙動が絡むテスト。
- **配置**: `Assets/YukimaruGames/Terminal/Tests/Runtime/`
- **特徴**: 実行に時間がかかるため、ロジックテストでカバーできない部分に限定して使用します。

### Compilation Tests (必須)
- **用途**: 全ての `.asmdef` 間の参照整合性とコンパイルの成否を確認。
- **実行方法**: `.bat/compile-test/run.bat`
- **ルール**: PR作成前およびレビュー依頼前の最終確認として必ず実行してください（詳細は [Agent Workflow Guidelines.md](./Agent%20Workflow%20Guidelines.md) を参照）。

## 3. 実装ルール

### ファイル命名と構成
- **ファイル名**: `[対象クラス名]Tests.cs` (例: `CommandRegistryTests.cs`)
- **クラス名**: `[対象クラス名]Tests`
- **アセンブリ**: テスト専用の `.asmdef` を作成し、`Editor` のみ、または適切なプラットフォーム制限をかけます。

### 手動モックの例
外部ライブラリを使わずに、インターフェースを実装したプライベートな内部クラスを使用して状態を検証します。

```csharp
public sealed class CommandRegistryTests
{
    private sealed class MockLogger : ICommandLogger
    {
        public bool IsCalled { get; private set; }
        public void Log(string message) => IsCalled = true;
        // 他のメソッドの実装...
    }

    [Test]
    public void Add_ValidCommand_ReturnsTrue()
    {
        // Arrange
        var logger = new MockLogger();
        var registry = new CommandRegistry(logger);
        
        // Act & Assert...
    }
}
```

## 4. 実行と検証

- **自動テスト**: Unity Editor の `Window > General > Test Runner` から実行します。
- **継続的確認**: 実装の Phase 完了ごとに、関連するテストスイートを実行してデグレがないか確認してください。
