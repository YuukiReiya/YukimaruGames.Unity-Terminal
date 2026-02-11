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
- **Role**: Defines the Interfaces (`IUseCases`, `IRepositories`, `IServices`) and Domain Models (pure POCOs).
- **Dependencies**: `SharedKernel`.
- **Rule**: **Must NOT depend on Core, Application, or UI.** Pure abstraction.

#### Domain.Core (`YukimaruGames.Terminal.Domain.Core`)
- **Role**: Implements the Domain Logic (UseCase implementations, Domain Services).
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
