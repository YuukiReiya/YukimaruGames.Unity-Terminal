Title Format
【TargetBranch/Label】Title
Example: 【master/refactor】View Contextの簡易実装
Labels: feature, refactor, fix, docs, etc.
Body Format
## 概要
(何をしたかの概要を日本語で記述)

## 背景・経緯
(なぜこの実装に至ったか、どのような議論・制約があったかを簡潔に記述)

以下は実装に至るまでの詳細な経緯です。該当する項目のみ記載し、特にない場合は項目ごと省略してください。
**目安として5行を超える場合や、項目が多い場合のみ `<details>` タグで折りたたんでください。1~3行程度の短い記述であれば、折りたたまずにそのまま記述することを推奨します。**

> [!TIP]
> 重要な注意点や制約、技術的な工夫がある場合は、`> [!IMPORTANT]` や `> [!NOTE]` などのGitHubアラートを使用して視覚的に強調してください。

**すべて特になければ、この「背景・経緯」セクション全体を削除しても構いません。**

<details>
<summary>当初の提案</summary>

(最初に検討した実装案や設計方針を記述)

</details>

<details>
<summary>制約・問題点</summary>

(実装を進める上で直面した制約や問題点を記述)

</details>

<details>
<summary>最終的な判断</summary>

(なぜこの実装方法を選択したか、他の案を却下した理由を記述)

</details>

## 変更点
(ファイルごとに変更内容を箇条書きにする。ファイル名はバッククォートで囲む)
- `FileA.cs`
  - (インデント1: 変更の詳細1)
  - (インデント1: 変更の詳細2)
- `FileB.cs`
  - (インデント1: 変更の詳細1)

## レビューポイント
(レビュアーに特に注意して見てほしい点、懸念点、判断の根拠などを記述)
- [ ] 重要なロジックの変更点1
- [ ] 考慮漏れがないかの確認依頼

## 検証結果
(検証内容をチェックリスト形式で記述)
- [ ] テストケースA (実行結果)
- [ ] 実機での動作確認

## 目的
(なぜこの変更が必要か、またどのような意図があるかを記述)

## 影響範囲
(影響を受けるファイルと潜在的なリスクを記述)
- `FileA.cs`
  - (潜在的なリスクや副作用)
- `Bootstrapper.cs`
  - (初期化順序のリスクなど)

## 備考
(特記事項や注意点。なければ「特になし」)
Rules
Language & Readability (言語と可読性):

All descriptions must be in Japanese. (全ての記述は日本語で行うこと)
Prioritize readability for the reviewer. (読み手にとっての読みやすさを最優先すること)
Use appropriate line breaks to avoid overly long paragraphs. (適切な改行を入れ、長すぎる段落を避けること)
Ensure clear separation between sections. (セクション間の区切りを明確にすること)
Formatting (フォーマット):

Always use backticks (``) for filenames, class names, and code keywords. (ファイル名、クラス名、コードキーワードは必ずバッククォートで囲むこと)
Group changes by file using indented bullet points. (変更点はファイルごとにグループ化し、インデントした箇条書きを使用すること)
Content (内容):

"変更点" must be granular per file. (変更内容はファイル単位で具体的に記述すること)
"影響範囲" must list specific files and risks. (影響範囲は具体的なファイル名とリスクを挙げること)

## GitHub Operations Rules
- **Assignees**: Always assign `@Yuuki-CI-Bot`.
- **Reviewers**: Always add `@YuukiReiya` as a reviewer.
- **Labels**: Apply appropriate labels based on the change (e.g., `refactor`, `feat`, `fix`, `impact: core`, `size: M`, `priority: high`).