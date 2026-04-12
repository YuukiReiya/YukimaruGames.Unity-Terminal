# Architecture Overview

This document outlines the high-level architecture of `YukimaruGames.Terminal`, focusing on the dependency graph and the strict separation of concerns between layers.

> [!NOTE]
> For the visual reUI of the architecture, please refer to the existing PlantUML diagram at:
> `docs/diagrams/Architecture/architecture.puml`
>
> The dependency rules outlined below are derived from this authoritative diagram and the actual `.asmdef` configurations.

## Architecture Evolution (險ｭ險医・螟蛾・)

譛ｬ繝励Ο繧ｸ繧ｧ繧ｯ繝医・縲∵ｩ溯・縺ｮ霑ｽ蜉縺ｨ繝代ャ繧ｱ繝ｼ繧ｸ縺ｨ縺励※縺ｮ驟榊ｸ・ｽ｢諷九・讓｡邏｢縺ｫ莨ｴ縺・∽ｻ･荳九・螟蛾・繧堤ｵ後※迴ｾ蝨ｨ縺ｮ險ｭ險医↓閾ｳ繧翫∪縺励◆縲・

1.  **蛻晄悄險ｭ險・(Technical Layering / 莨晉ｵｱ逧・↑髫主ｱ､蛹・**:
    - 謚陦鍋噪縺ｪ蠖ｹ蜑ｲ・・Interfaces/`, `ValueObjects/` 遲会ｼ峨＃縺ｨ縺ｫ繝輔か繝ｫ繝繧貞・蜑ｲ縺励※縺・◆縲・
2.  **隨ｬ2繝輔ぉ繝ｼ繧ｺ (Vertical Slice 繧｢繝ｼ繧ｭ繝・け繝√Ε縺ｮ蟆主・ - PR #53)**:
    - **邨檎ｷｯ**: AI 蛛ｴ縺九ｉ縺ｮ縲梧ｩ溯・蜊倅ｽ搾ｼ・ommands 遲会ｼ峨〒縺ｮ蜃晞寔諤ｧ縺ｮ蜷台ｸ翫阪→縺・≧謠先｡医↓蝓ｺ縺･縺榊ｮ滓命縺輔ｌ縺滓ｧ矩逧・↑螳滄ｨ薙・
    - **謌先棡**: UPM 驟榊ｸ・ｒ諠ｳ螳壹＠縺溘後そ繝槭Φ繝・ぅ繝・け繝ｻ繝励Ξ繝輔ぅ繝・け繧ｹ隕丞援・亥・髢九け繝ｩ繧ｹ縺ｸ縺ｮ `Terminal` 蜀荳趣ｼ峨阪ｄ蜷榊燕遨ｺ髢薙・謨ｴ逅・′騾ｲ繧薙□縲・
    - **隱ｲ鬘・*: 讖溯・繧定ｷｨ縺・〒蜈ｱ逕ｨ縺輔ｌ繧玖ｦ∫ｴ・・croll, Accessor 遲会ｼ峨・驟咲ｽｮ縺・VSA 縺ｮ縲梧ｩ溯・繝輔か繝ｫ繝縲阪→縺・≧蛻ｶ邏・↓邵帙ｉ繧後∝錐蜑咲ｩｺ髢薙・諢丞袖逧・↑荳肴紛蜷茨ｼ亥⊃縺ｮ繧ｫ繝・ざ繝ｪ蛹厄ｼ峨′逕溘§繧句憶菴懃畑縺檎｢ｺ隱阪＆繧後◆縲・
3.  **譛邨らｵ占ｫ・(Evolved Clean Architecture / 蠖ｹ蜑ｲ繝吶・繧ｹ縺ｸ縺ｮ蜀咲ｷｨ)**:
    - **隗｣豎ｺ**: VSA 繧貞ｻ・ｭ｢縺励∝・縺ｳ縲梧橿陦鍋噪蠖ｹ蜑ｲ縲阪↓蝓ｺ縺･縺城嚴螻､縺ｸ蜀咲ｷｨ縲ゅ◆縺縺励∝腰邏斐↑縲悟ｷｮ縺玲綾縺励阪〒縺ｯ縺ｪ縺上∫ｬｬ2繝輔ぉ繝ｼ繧ｺ縺ｧ遒ｺ遶九＆繧後◆蜻ｽ蜷崎ｦ丞援縺ｨ縲∽ｸｭ逶､縺ｧ蟆主・縺輔ｌ縺溯ｨｭ險医ヱ繧ｿ繝ｼ繝ｳ繧堤ｵｱ蜷医＠縺ｦ縺・ｋ縲・

### 屏・・蛻晄悄險ｭ險医°繧峨・騾ｲ蛹也せ (What's New)
莉雁屓縺ｮ蜀咲ｷｨ蠕後・讒区・縺ｯ縲∝・譛溯ｨｭ險医→豈碑ｼ・＠縺ｦ莉･荳九・轤ｹ縺碁ｫ伜ｺｦ蛹悶＆繧後※縺・∪縺吶・

| 讖溯・ | 蛻晄悄險ｭ險・| 迴ｾ蝨ｨ (Clean Architecture 2.0) |
| :--- | :--- | :--- |
| **繧｢繧ｯ繧ｻ繧ｹ蛻ｶ蠕｡** | 蜊倡ｴ斐↑繝励Ο繝代ユ繧｣ | **Accessor 繝代ち繝ｼ繝ｳ** (`Provider/Mutator` 蛻・屬) 縺ｮ蠕ｹ蠎・|
| **Application 螻､** | 蠅・阜縺梧尠譏ｧ | **Service** (Facade) 縺ｨ **UseCase** (繝ｯ繝ｼ繧ｯ繝輔Ο繝ｼ) 縺ｮ譏守｢ｺ縺ｪ蛻・屬 |
| **Infrastructure** | 豎守畑逧・↑繝輔か繝ｫ繝 | **Discovery** (謗｢邏｢), **Factories** (逕滓・) 遲峨∫岼逧・挨縺ｮ蟆る摩蛹・|
| **UI 螳壽焚邂｡逅・* | 謨｣蝨ｨ | **UI.Models** 縺ｸ縺ｮ螳悟・縺ｪ繧ｫ繝励そ繝ｫ蛹・|
| **蜻ｽ蜷崎ｦ丞援** | 荳咲ｵｱ荳 | **繧ｻ繝槭Φ繝・ぅ繝・け繝ｻ繝励Ξ繝輔ぅ繝・け繧ｹ隕丞援** 縺ｮ驕ｩ逕ｨ |

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
- **Rule**: **Protects the Application Layer boundary (Boundary Protection).**
  - > [!IMPORTANT]
  - > `UnityEngine` 蝙具ｼ・Color`, `GUIStyle` 遲会ｼ峨ｒ繧､繝ｳ繧ｿ繝ｼ繝輔ぉ繝ｼ繧ｹ縺ｫ蜷ｫ繧 Accessor 縺ｯ縲、pplication 螻､繧剃ｿ晁ｭｷ縺吶ｋ縺溘ａ縲・*縺薙・繝ｬ繧､繝､繝ｼ・・I 螻､・峨↓繧､繝ｳ繧ｿ繝ｼ繝輔ぉ繝ｼ繧ｹ繧帝・鄂ｮ**縺励∪縺吶・
  - > 縺溘□縺励√◎縺ｮ **螳滉ｽ難ｼ亥ｮ溯｣・け繝ｩ繧ｹ・峨・ Infrastructure 螻､** 縺ｫ驟咲ｽｮ縺励．ependency Inversion 繧堤ｶｭ謖√＠縺ｾ縺吶・

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
  - > UI繝輔Ξ繝ｼ繝繝ｯ繝ｼ繧ｯ・・nity・峨↓蠑ｷ縺丞ｯ・ｵ仙粋縺吶ｋ繧､繝ｳ繧ｿ繝ｼ繝輔ぉ繝ｼ繧ｹ縺ｯ UI 螻､縺ｧ螳夂ｾｩ縺輔ｌ縺ｾ縺吶′縲√◎縺ｮ螳滉ｽ難ｼ医ョ繝ｼ繧ｿ縺ｮ菫晄戟繝ｻ蜷梧悄繝ｭ繧ｸ繝・け・峨・縺薙・繝ｬ繧､繝､繝ｼ・・nfrastructure・峨′諡・＞縺ｾ縺吶・

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
蜷・Ξ繧､繝､繝ｼ蜀・Κ縺ｯ縲∵ｩ溯・蜊倅ｽ搾ｼ・ertical Slice・峨〒縺ｯ縺ｪ縺上∵橿陦鍋噪縺ｪ蠖ｹ蜑ｲ・・resenter/Renderer/Interface遲会ｼ峨〒謨ｴ逅・＠縺ｾ縺吶・
萓晏ｭ俶婿蜷代・騾乗・諤ｧ繧帝ｫ倥ａ縲∝腰荳繧ｵ繝ｼ繝薙せ縺ｨ縺励※縺ｮ逶ｴ諢滓ｧ繧貞━蜈医☆繧九◆繧√〒縺吶・

### 1. Structure
- **UI/...**: `Presenters/`, `Renderers/`, `Models/`, `Interfaces/`, `Accessors/`
- **Domain/...**: `Services/`, `Interfaces/`, `Models/`
- **Application/...**: `Services/`, `UseCases/`, `Interfaces/`, `Mappers/`, `Models/`
- **Infrastructure/...**: `Discovery/`, `Factories/`, `Repositories/`

### 2. Semantic Prefix Rule (Semantic Naming)
縲後Λ繧､繝悶Λ繝ｪ蜷搾ｼ・Terminal`・峨・繝励Μ繝輔ぅ繝・け繧ｹ縲阪・隕∝凄縺ｯ縲∝茜逕ｨ逕ｨ騾費ｼ亥・髢帰PI縺句・驛ｨ螳溯｣・°・峨→縺・≧縲後そ繝槭Φ繝・ぅ繝・け縲阪↓蝓ｺ縺･縺・※豎ｺ螳壹＠縺ｾ縺吶・

- **繝励Ξ繝輔ぅ繝・け繧ｹ繧偵蝉ｻ倅ｸ弱☆繧九大ｯｾ雎｡・・ublic API・・*: 
  - 繝ｦ繝ｼ繧ｶ繝ｼ縺檎峩謗･ Inspector 縺ｧ繧｢繧ｿ繝・メ縺励◆繧翫√さ繝ｼ繝峨°繧牙他縺ｳ蜃ｺ縺励◆繧翫☆繧九ｂ縺ｮ縲・
  - **萓・*: `TerminalBootstrapper`, `TerminalStandardInstaller`, `TerminalService`
  - 繝ｦ繝ｼ繧ｶ繝ｼ縺悟ｱ樊ｧ縺ｨ縺励※菴ｿ逕ｨ縺吶ｋ繧ゅ・縲・
  - **萓・*: `TerminalCommandAttribute`
  - 莉悶・繝ｩ繧､繝悶Λ繝ｪ繧・Unity 讓呎ｺ悶・蝙九→蜷榊燕縺瑚｡晉ｪ√＠繧・☆縺上∵・遉ｺ逧・↑譖匁乂縺募屓驕ｿ縺悟ｿ・ｦ√↑繧ゅ・縲・

- **繝励Ξ繝輔ぅ繝・け繧ｹ繧偵蝉ｻ倅ｸ弱＠縺ｪ縺・大ｯｾ雎｡・・nternal Implementation・・*:
  - 蜀・Κ繝ｭ繧ｸ繝・け縺ｮ螳溯｣・け繝ｩ繧ｹ縲√う繝ｳ繝輔Λ螻､縺ｮ蜈ｷ雎｡繧ｯ繝ｩ繧ｹ縲ゞI螻､縺ｮ蜀・Κ Presenter 遲峨・
  - **萓・*: `CommandRegistry`, `LogPresenter`, `FontProvider`, `CommandParser`
  - 蜷榊燕遨ｺ髢難ｼ・amespace・峨↓繧医▲縺ｦ譌｢縺ｫ諢丞袖縺碁剞螳壹＆繧後※縺・ｋ蜀・Κ逧・↑謚ｽ雎｡繝ｻ蜈ｷ雎｡縲・

