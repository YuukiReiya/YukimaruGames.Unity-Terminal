# Architecture Overview

This document outlines the high-level architecture of `YukimaruGames.Terminal`, focusing on the dependency graph and the strict separation of concerns between layers.

> [!NOTE]
> For the visual representation of the architecture, please refer to the existing PlantUML diagram at:
> `docs/diagrams/Architecture/architecture.puml`
>
> The dependency rules outlined below are derived from this authoritative diagram and the actual `.asmdef` configurations.

## Architecture Evolution (設計の変遷)

本プロジェクトは、機能の追加とパッケージとしての配布形態の模索に伴い、以下の変遷を経て現在の設計に至りました。

1.  **初期設計 (Technical Layering / 伝統的な階層化)**:
    - 技術的な役割（`Interfaces/`, `ValueObjects/` 等）ごとにフォルダを分割していた。
2.  **第2フェーズ (Vertical Slice アーキテクチャの導入 - PR #53)**:
    - **経緯**: AI 側からの「機能単位（Commands 等）での凝集性の向上」という提案に基づき実施された構造的な実験。
    - **成果**: UPM 配布を想定した「セマンティック・プレフィックス規則（公開クラスへの `Terminal` 冠与）」や名前空間の整理が進んだ。
    - **課題**: 機能を跨いで共用される要素（Scroll, Accessor 等）の配置が VSA の「機能フォルダ」という制約に縛られ、名前空間の意味的な不整合（偽のカテゴリ化）が生じる副作用が確認された。
3.  **最終結論 (Evolved Clean Architecture / 役割ベースへの再編)**:
    - **解決**: VSA を廃止し、再び「技術的役割」に基づく階層へ再編。ただし、単純な「差し戻し」ではなく、第2フェーズで確立された命名規則と、中盤で導入された設計パターンを統合している。

### 🛠️ 初期設計からの進化点 (What's New)
今回の再編後の構成は、初期設計と比較して以下の点が高度化されています。

| 機能 | 初期設計 | 現在 (Clean Architecture 2.0) |
| :--- | :--- | :--- |
| **アクセス制御** | 単純なプロパティ | **Accessor パターン** (`Provider/Mutator` 分離) の徹底 |
| **Application 層** | 境界が曖昧 | **Service** (Facade) と **UseCase** (ワークフロー) の明確な分離 |
| **Infrastructure** | 汎用的なフォルダ | **Discovery** (探索), **Factories** (生成) 等、目的別の専門化 |
| **UI 定数管理** | 散在 | **UI.Models** への完全なカプセル化 |
| **命名規則** | 不統一 | **セマンティック・プレフィックス規則** の適用 |

## Layer Definitions & Rules

### 1. SharedKernel (`YukimaruGames.Terminal.SharedKernel`)
- **Role**: Contains universal data structures, constants, and extensions used across the entire project.
- **Dependencies**: None (Zero Dependency).
- **Rule**: **Must never depend on any other layer.**

### 2. Domain Layer
Split into two distinct assemblies to enforce Dependency Inversion.

#### Domain.Abstractions (`YukimaruGames.Terminal.Domain.Abstractions`)
- **Role**: Defines the Interfaces (`IServices`, `IRepositories`), Domain Models, and Value Objects.
- **Organization**: Categorized by technical roles + functional subfolders:
  - `Interfaces/Services/`, `Interfaces/Repositories/`
  - `Models/Entities/`, `Models/ValueObjects/`
  - `Attributes/`, `Exceptions/`
- **Dependencies**: `SharedKernel`.
- **Rule**: **Pure abstraction.** Must NOT depend on Domain Implementation, Application, or UI.

#### Domain Implementation (`YukimaruGames.Terminal.Domain`)
- **Role**: Implements the Domain Logic defined in `Domain.Abstractions`.
- **Organization**:
  - `Services/`: Domain logic implementations (Parser, Registry etc.).
  - `Repositories/`: In-memory or domain-specific repository implementations (e.g., `CommandHistory`).
- **Dependencies**: `Domain.Abstractions`, `SharedKernel`.
- **Rule**: **Must NOT depend on Infrastructure or UI.**

### 3. Application Layer (`YukimaruGames.Terminal.Application`)
- **Role**: Orchestrates the Domain logic to fulfill user requests. Holds the application state.
- **Organization**:
  - `Services/`: Application Services (Facade/Coordinator).
  - `UseCases/`: Specific workflow scenarios (Logic heavy processes).
- **Dependencies**: `Domain.Abstractions`, `SharedKernel`.
- **Rule**: **Must NOT reference Infrastructure or Domain implementation directly.**

### 4. UI Layer (`YukimaruGames.Terminal.UI`)
- **Role**: Handles View and Presenter logic. Provides UI-framework-specific data access.
- **Organization**: Technical Top-level + Functional Subfolders (e.g., `Log/`, `Input/`).
  - `Presenters/`, `Renderers/`, `Coordinators/`, `Accessors/`, `Models/`, `Interfaces/`, `Events/`
- **Dependencies**: `Application` (for Services/UseCases), `SharedKernel`, `Domain.Abstractions`.
- **Rule**: **Protects the Application Layer boundary.**
  - > [!IMPORTANT]
  - > **特例 (UI完結)**: UnityEngine型（Color, GUIStyle等）に深く依存し、UnityのUI描画に関わる Accessor は、Infrastructure層にUIの都合を持ち込ませないための特例として**このレイヤー（UI 層）内で完結（Interfaceと実装を両方配置）**させます。これは以前確定した方針に基づきます。

### 5. Infrastructure Layer (`YukimaruGames.Terminal.Infrastructure`)
- **Role**: Implements interfaces defined in Domain/Application/UI layers that involve external systems or state management.
- **Organization**:
  - `Discovery/`: Command discovery logic (Reflection).
  - `Factories/`: Implementation for handler creation.
  - `Repositories/`: External data persistence (FileSystem, PlayerPrefs).
  - `UI/`: Concrete implementations of Accessors defined in the UI layer.
- **Dependencies**: `Domain.Abstractions`, `Domain` (for instantiation if needed), `SharedKernel`, `UI` (for Accessor implementations).
- **Rule**: **The tech-detail layer that respects Application purity.**
  - > [!NOTE]
  - > **原則 (DIP)**: UI層（またはApplication/Domain層）で定義されたインターフェースの実装は原則的にこの Infrastructure 層が担います。ただし、上述の通りUIフレームワークに強く密結合する一部の特別ケース（Color等）の Accessor のみ例外的に UI 層で完結させます。

### 6. Composition Root (`YukimaruGames.Terminal.Runtime`)
- **Role**: The Entry Point (Bootstrapper). Wires up the dependency injection container.
- **Dependencies**: All Layers.
- **Rule**: **The only place allowed to reference concrete implementations of all layers.**

## Common Violations to Avoid
| Violation Type | Description | Prevention |
| :--- | :--- | :--- |
| **Circular Dependency** | Domain.API referencing Domain.Core | Strict enforce via `.asmdef` references (already configured). |
| **Logic Leakage** | UI referencing Domain.Core directly | UI should talk to Application (Presenters) or Domain.API (Interfaces) only. |
| **Hidden Coupling** | SharedKernel depending on UI types | SharedKernel must remain pure C# without UI dependencies where possible. |

## Verification Strategy
The `.asmdef` definitions currently enforce these rules physically.
- `Domain.API` does NOT reference `Domain.Core`.
- `Application` does NOT reference `Domain.Core` (it relies on abstraction).
- `Bootstrapper` does NOT hold configuration state. It delegates configuration retrieval to the `Installer`.

## Configuration & Extensibility Principles
- **Zero Config Defaults**: Standard configurations must work out-of-the-box using POCOs (Plain Old C# Objects) without requiring asset creation (ScriptableObject).
- **Installer Responsibility**: The `Installer` implementation is responsible for defining and retrieving its configuration. The Bootstrapper should not inject configuration dependencies into the Installer unless necessary for the interface contract.
- **Meaningful Fields**: Do not expose `[SerializeField]` in the Bootstrapper that are only used by specific installer implementations. This is considered "Logic Leakage".

## Internal Module Organization (Technical Layering)
各レイヤー内部は、機能単位（Vertical Slice）ではなく、技術的な役割（Presenter/Renderer/Interface等）で整理します。
依存方向の透明性を高め、単一サービスとしての直感性を優先するためです。

### 1. Structure
- **UI/...**: `Presenters/`, `Renderers/`, `Models/`, `Interfaces/`, `Accessors/`
- **Domain/...**: `Services/`, `Interfaces/`, `Models/`
- **Application/...**: `Services/`, `UseCases/`, `Interfaces/`, `Mappers/`, `Models/`
- **Infrastructure/...**: `Discovery/`, `Factories/`, `Repositories/`

### 2. Semantic Prefix Rule (Semantic Naming)
「ライブラリ名（`Terminal`）のプリフィックス」の要否は、利用用途（公開APIか内部実装か）という「セマンティック」に基づいて決定します。

- **プレフィックスを【付与する】対象（Public API）**: 
  - ユーザーが直接 Inspector でアタッチしたり、コードから呼び出したりするもの。
  - **例**: `TerminalBootstrapper`, `TerminalStandardInstaller`, `TerminalService`
  - ユーザーが属性として使用するもの。
  - **例**: `TerminalCommandAttribute`
  - 他のライブラリや Unity 標準の型と名前が衝突しやすく、明示的な曖昧さ回避が必要なもの。

- **プレフィックスを【付与しない】対象（Internal Implementation）**:
  - 内部ロジックの実装クラス、インフラ層の具象クラス、UI層の内部 Presenter 等。
  - **例**: `CommandRegistry`, `LogPresenter`, `FontProvider`, `CommandParser`
  - 名前空間（Namespace）によって既に意味が限定されている内部的な抽象・具象。
