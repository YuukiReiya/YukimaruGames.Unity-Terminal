# ユニットテスト追加ワークフロー

指定されたクラスのユニットテスト雛形を生成する。
Domain層・Application層の純粋C#クラスを対象とする。

---

## Step 1: 対象クラスの確認

ユーザーに以下を確認する:

- テスト対象のクラス名 / ファイルパス
- テストフレームワーク（NUnit / デフォルトUnity Test Framework）

指定がない場合はUnity Test Framework（NUnit）を使用する。

---

## Step 2: 対象クラスの読み込み

対象クラスを読み込み、以下を把握する:

- publicメソッドの一覧と引数・戻り値
- コンストラクタの引数
- ドメインルール（上限値・例外条件など）

---

## Step 3: テストケースの提案

以下の観点でテストケースを提案し、ユーザーの承認を得る:

各publicメソッドに対して:
- **正常系**: 通常の入力で期待通りの結果になるか
- **境界値**: 上限・下限・ゼロなどの境界条件
- **異常系**: 不正な入力・例外が発生するケース

例:
```
【PlayerHp.Damage() のテストケース案】
正常系: ダメージを受けてHPが減少する
境界値: ダメージ量がHPと同じ場合、HPが0になる
境界値: ダメージ量がHPを超えた場合、HPが0未満にならない
異常系: 負のダメージ量を渡した場合の挙動
```

---

## Step 4: テストファイルの生成

承認されたテストケースをもとに、以下のパスにテストファイルを生成する:

```
Assets/YukimaruGames/Terminal/Tests/EditMode/[元のフォルダ構成を維持]/〇〇Tests.cs
```

生成するコードの形式:

```csharp
using NUnit.Framework;

public class 〇〇Tests
{
    // テスト対象のセットアップ
    private 〇〇 _sut; // sut = System Under Test

    [SetUp]
    public void SetUp()
    {
        // 初期化処理
    }

    [Test]
    public void メソッド名_条件_期待結果()
    {
        // Arrange: テストデータの準備
        // Act: テスト対象の実行
        // Assert: 結果の検証
    }
}
```

---

## Step 5: テスト実行の確認

テストファイルの生成後、以下をユーザーに伝える:

- テストの実行方法（Unity Test Runner: Window > General > Test Runner）
- EditModeテストとPlayModeテストの違い
- 生成したテストがEditModeで実行可能であること（Domain層はUnity非依存のため）
