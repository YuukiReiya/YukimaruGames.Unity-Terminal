# ワークフロー・禁止事項

## 作業開始時のチェック

新しい作業を始める前に以下を確認すること:

- [ ] 作業ブランチが作成されているか（`feature/`, `fix/`, `refactor/` のいずれかのprefixを使う）
- [ ] 作業対象のレイヤーが明確か
- [ ] 既存の関連クラスを把握しているか

## コミットのタイミング

以下のタイミングでコミットを提案すること:

- 1つの機能・修正が完成したとき
- テストがパスしたとき
- リファクタリングが完了したとき

コミットメッセージは以下の形式を使う:

```
[feat] PlayerHpにダメージ計算ロジックを追加
[fix] PlayerControllerのnull参照例外を修正
[refactor] EnemyAIをDomainServiceに移動
[test] PlayerHpのユニットテストを追加
```

## 禁止事項

### コード品質
- `// TODO` を残したまま作業完了としない
- マジックナンバーをハードコードしない
- `Debug.Log` を本番コードに残さない（`#if UNITY_EDITOR` で囲む）
- `#region` の使用禁止

### アーキテクチャ
- Domain層に `using UnityEngine` を追加しない
- Presentation層からInfrastructure層を直接参照しない
- `FindObjectOfType()` / `GameObject.Find()` の使用禁止
- `MonoBehaviour` をDomain・Application・Infrastructure層に配置しない

### ファイル操作
- ユーザーの承認なしに既存ファイルを削除・リネームしない
- `.meta` ファイルを手動編集しない
- `Assets/Plugins/` 以下のファイルを変更しない

## レビュー観点（コード生成後の自己チェック）

Clineはコード生成後に以下を確認すること:

1. レイヤーの依存方向が守られているか
2. 命名規則（01-coding-style.md）に従っているか
3. `SerializeField` の使い方が正しいか
4. Domain層にUnityの型が混入していないか
5. nullチェックの方法が適切か
