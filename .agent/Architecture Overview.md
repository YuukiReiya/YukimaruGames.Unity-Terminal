# Architecture Overview

This document outlines the high-level architecture of `YukimaruGames.Terminal`, focusing on the dependency graph and the strict separation of concerns between layers.

> [!NOTE]
> For the visual representation of the architecture, please refer to the existing PlantUML diagram at:
> `docs/diagrams/Architecture/architecture.puml`
>
> The dependency rules outlined below are derived from this authoritative diagram and the actual `.asmdef` configurations.

## Layer Definitions & Rules

### 1. SharedKernel (`YukimaruGames.Terminal.SharedKernel`)
- **Role**: Contains universal data structures, constants, and extensions used across the entire project.
- **Dependencies**: None (Zero Dependency).
- **Rule**: **Must never depend on any other layer.**

### 2. Domain Layer
Split into two distinct assemblies to enforce Dependency Inversion.

#### Domain.API (`YukimaruGames.Terminal.Domain.API`)
- **Role**: Defines the Interfaces (`IUseCases`, `IRepositories`, `IServices`), Domain Models, and Value Objects.
- **Organization**: Categorized by functional areas (Vertical Slices):
  - `Commands/`: Primary area for command-related interfaces and metadata.
  - `Attributes/`, `Exceptions/`: Technical categories (pluralized).
- **Dependencies**: `SharedKernel`.
- **Rule**: **Pure abstraction.** Must NOT depend on Core, Application, or UI.

#### Domain.Core (`YukimaruGames.Terminal.Domain.Core`)
- **Role**: Implements the Domain Logic (UseCase implementations, Domain Services).
- **Organization**: Follows functional categorization (e.g., `Commands/`).
- **Dependencies**: `Domain.API`, `SharedKernel`.
- **Rule**: **Must NOT depend on Infrastructure or UI.**

### 3. Application Layer (`YukimaruGames.Terminal.Application`)
- **Role**: Orchestrates the Domain logic to fulfill user requests. Holds the application state.
- **Dependencies**: `Domain.API`, `SharedKernel`.
- **Rule**: **Should use `Domain.API` interfaces, not `Domain.Core` concrete classes.**

### 4. Presentation Layer (`YukimaruGames.Terminal.UI`)
- **Role**: Handles View and Presenter logic.
- **Dependencies**: `Application` (for Interactors/Presenters), `SharedKernel`.
- **Rule**: **Must NOT reference Infrastructure or Domain.Core directly.**

### 5. Infrastructure Layer (`YukimaruGames.Terminal.Infrastructure`)
- **Role**: Implements interfaces defined in `Domain.API` (e.g., Repositories, External Service Adapters).
- **Dependencies**: `Domain.API`, `Domain.Core` (to instantiate logic if needed), `SharedKernel`.
- **Rule**: **The detailed implementation detail layer.**

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

## Internal Module Organization (Vertical Slice)
各レイヤー（特にUI層やドメイン層）の内部構造は、技術的な役割（Presenter/View等）ではなく、機能単位（Vertical Slice）で整理します。

### 1. Functional Folders
単一の機能に関わるファイル（Presenter, Renderer, Interface等）は、同一のフォルダ内に集約します。
- **UI/...**: `Main/`, `Core/`, `Log/`, `Input/`, `Launcher/` 等
- **Domain/...**: `Commands/`, `Logging/` 等

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
