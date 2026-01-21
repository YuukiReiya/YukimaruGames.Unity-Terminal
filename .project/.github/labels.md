# 概要

プロジェクトで利用するGithubラベルの定義

## ラベル種別

### 種別

* feat
  * New feature (機能追加)
* fix
  * Bug fix (バグ修正) 
* refactor
  * Refactoring (リファクタリング)
* perf
  * Performance (パフォーマンス)
* docs
  * Documentation (ドキュメント)
* test
  * Test (テスト)
* build
  * Build configuration / Dependencies (ビルド・依存関係)
* ci
  * CI/CD configuration (CI/CD設定)

### 影響範囲・規模

* size: S
  *  Small (< 50 lines)
* size: M
  * Medium (50-200 lines)
* size: L
  * Large (200-500 lines)
* size: XL
  * Huge (> 500 lines)
* priority: high
  * Urgent / Release Blocker
* priority: medium
  * Normal
* priority: low
  * Low priority

### アーキテクチャ

* layer: domain
  * Domain Logic (Core rule)
* layer: application
  * Application Business Logic
* layer: infrastructure
  * Infrastructure / External IO
* layer: presentation
  * UI / View / Presenter

### 領域

* domain: client
  * Client-side logic (Frontend)
* domain: server
  * Server-side logic (Backend)

### 言語

* lang: c#
* lang: shader
  * (HLSL/ShaderLab)
* lang: html/css
  * (UI Toolkit UXML/USS)

### プラットフォーム

* unity
  * Unity specific assets/components