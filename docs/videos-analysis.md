


# ME.BECS Video Series Analysis & Takeaways

This document outlines key architectural learnings, best practices, and constraints identified in the ME.BECS training series, accompanied by Mermaid diagrams to visualize the architecture.

## Video 1: Setup & Initialization
**Core Topic:** Setting up the World hierarchy and project structure.

### Key Takeaways & Learns
* **World Hierarchy:** The framework relies on nested worlds. For Multiplayer, the hierarchy is `Network World (Logic) -> Visual World (Local)`. For Singleplayer, a `Visual World` (acting as Logic) is sufficient, though treating Singleplayer as a local Multiplayer setup is recommended for scalability.
* **Execution Order:** Critical dependency management via Script Execution Order. Network Initializer must run before Visual Initializer (-103 vs -102).
* **Circular Reference:** The Network World's parent is set to itself; the Visual World's parent is the Network World. This ensures the prediction loop drives the visual state.
* **Defines:** Specific preprocessor directives are required for Multiplayer vs. Singleplayer builds.

### Diagram: Initialization Flow
``` mermaid
---
config:
  theme: dark
---
graph TD
    subgraph "Initialization Phase"
    A[Unity Awake] --> B{Project Type?}
    B -- Singleplayer --> C[Visual Initializer]
    B -- Multiplayer --> D[Network Initializer]
    D --> E[Create Logic World]
    E --> F[Link Modules: Network & View]
    F --> G[Initialize Feature Graph]
    
    G --> H[Visual Initializer]
    H --> I[Create Visual World]
    I --> J[Set Parent: Logic World]
    J --> K[Link View Module]
    end
```

* * *

Video 2: Basics (Systems, Components, Aspects)
----------------------------------------------

**Core Topic:** Data structures and the core update loop.

### Key Takeaways & Learns

*   **Component Constraints:** **Strictly no `bool` types** (not blittable across architectures). Use `byte` (0/1). No reference types or classes in Components; use internal `Collections` (Automated). Use FixedPoint math (`sfloat`, `float2`) instead of standard float/Vector.
*   **System Interfaces:** Systems are structs implementing `ISystem`, `IAwake`, `IUpdate`, etc. Public fields in Systems act as injection points for the Feature Graph editor.
*   **Aspects:** Primary tool for performance. Groups components into a single pointer lookup. Faster than querying components individually.
*   **Entity Lifecycle:** `Destroy(entity)` is dangerous; prefer `DestroyHierarchy` or `DestroyInTick`. Entities must be kept alive if referenced by Views.

### Diagram: Data Structure

``` mermaid
---
config:
  theme: dark
---
classDiagram
    class Entity {
        +int Id
        +int Version
    }
    class Component {
        <<Struct>>
        +Blittable Data Only
        -No Bools
        -No Classes
    }
    class Aspect {
        <<Struct>>
        +Ref Component A
        +Ref Component B
        +Method Action()
    }
    class System {
        <<Struct>>
        +OnUpdate(Context)
    }

    Entity "1" -- "many" Component : Has
    Aspect ..> Component : Points to
    System ..> Aspect : Iterates via Jobs
```

* * *

Video 3: Views & Visuals
------------------------

**Core Topic:** Rendering, ViewModules, and bridging Logic to Unity.

### Key Takeaways & Learns

*   **Logic vs. Visual Separation:** Logic world Entities cannot be modified directly by Views. Views only _react_ to state changes.
*   **ApplyState:** The critical method for Views. It is only triggered when the Entity's Version changes (i.e., data modified).
*   **Composition over Inheritance:** Use `ViewModules` (e.g., SelectionModule, HealthBarModule) attached to an `EntityView` rather than creating a massive inheritance tree of Monobehaviours.
*   **Culling:** `CullingType` on Views saves performance by disabling `ApplyState/Update` when off-screen. Requires injecting the Camera Aspect into the ViewModule.

### Diagram: View Update Loop

``` mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant LogicWorld
    participant VisualWorld
    participant EntityView
    participant ViewModule

    LogicWorld->>LogicWorld: System Modifies Component
    LogicWorld->>LogicWorld: Entity Version++
    
    par Sync to Visual
        VisualWorld->>EntityView: Check Entity Version
    end
    
    opt Version Changed
        VisualWorld->>EntityView: Call ApplyState()
        EntityView->>ViewModule: Delegate ApplyState()
        ViewModule->>ViewModule: Update Unity Transform/Mesh
    end
```

* * *

Video 4: Jobs, Queries & Networking
-----------------------------------

**Core Topic:** Multithreading, Input Prediction, and Rollback.

### Key Takeaways & Learns

*   **Job System:** Heavy reliance on `IJobEntity` (or similar interfaces). Avoid `Complete()` inside the loop; schedule dependencies (`JobHandle`) and let the framework manage completion.
*   **Input Architecture:** Inputs are not applied immediately. They are sent as Commands (RPCs) to the Server/Logic World.
*   **Rollback/Prediction:** The client predicts the result of the input locally. If the server state differs later, the framework rolls back the tick and replays inputs.
*   **Determinism:** Logic must be deterministic. Use framework Random and Math libraries.
*   **Replays:** By recording inputs and ticks, the entire game state can be replayed perfectly for debugging.

### Diagram: Input Prediction Loop

``` mermaid
---
config:
  theme: dark
---
graph LR
    subgraph Client
    A[Input Hardware] --> B[Input System]
    B --> C{Diff vs Last Frame?}
    C -- Yes --> D[Send Network Event]
    D --> E[Apply Input Locally (Prediction)]
    E --> F[Render Frame]
    end

    subgraph Server
    D -. Network .-> G[Receive Input Packet]
    G --> H[Logic Tick]
    H --> I[Authoritative State]
    end

    subgraph Client Correction
    I -. Network .-> J[Receive Server State]
    J --> K{Mismatch?}
    K -- Yes --> L[Rollback & Replay]
    end
```

* * *

Video 5: Entity Configs
-----------------------

**Core Topic:** Data definitions and "Prefabs" for ECS.

### Key Takeaways & Learns

*   **Static Components:** Data that never changes at runtime (e.g., MaxHealth, FiringRate). Stored on the Config, not the Entity chunk, saving memory. Accessed via `ReadStatic`.
*   **Join Options:** When applying a config (e.g., an "Upgrade Config"), you can choose `LeftJoin` (update existing only), `RightJoin` (add new only), or `FullJoin`.
*   **Config Inheritance:** Configs can inherit from other configs (BaseUnit -\> SoldierUnit), allowing layered data definitions.

### Diagram: Config Application

``` mermaid
---
config:
  theme: dark
---
graph TD
    A[Base Config] --> B[Unit Config]
    B --> C[Upgrade Config]
    
    D[Entity]
    
    subgraph "Application Process"
    B -- Full Join --> D
    C -- Left Join (Overwrite only existing) --> D
    end
    
    style D fill:#f9f,stroke:#333
```

* * *

Video 6: Addressables & UI
--------------------------

**Core Topic:** Asset Management and UI Communication.

### Key Takeaways & Learns

*   **Lazy Loading:** `EntityView` references in Configs (via Addressables) load on demand. `ObjectReference` loads on ECS initialization.
*   **UI Event Bus:** UI should not query ECS directly every frame. Use `GlobalEvents` (Framework Event Bus).
*   **Event Accumulation:** Multiple events fired in one tick are accumulated/de-bounced so the UI only redraws once per frame with the latest data.
*   **Safety:** Wrap Global Events in a static helper class to enforce type safety (prevent sending a Vector3 when the listener expects an Entity).

### Diagram: UI Event Propagation

``` mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant LogicSystem
    participant GlobalEventBus
    participant UI_Widget

    loop Game Tick
        LogicSystem->>LogicSystem: Calculate HP Change
        LogicSystem->>GlobalEventBus: Raise Event (Target, NewHP)
        Note right of GlobalEventBus: Accumulate/Debounce
    end

    loop Late Update / Render
        GlobalEventBus->>UI_Widget: Trigger Callback (Once)
        UI_Widget->>UI_Widget: Update Health Bar
    end
```

* * *

Video 7: Players & Teams
------------------------

**Core Topic:** Metadata ownership.

### Key Takeaways & Learns

*   **Player Entity:** Represents the human connection. distinct from the Unit Entity.
*   **ID Strategy:** `ClientID + 1` = PlayerID. Player 0 is reserved for "Neutral/Nature".
*   **Team Management:** Teams are Entities. Updating teams requires `UpdateTeams` to refresh collision/interaction masks (e.g., friend-or-foe logic).

### Diagram: Ownership Hierarchy

``` mermaid
---
config:
  theme: dark
---
graph BT
    U1[Unit Entity A] -->|Owner| P1[Player Entity 1]
    U2[Unit Entity B] -->|Owner| P1
    
    P1 -->|Member Of| T1[Team Entity Red]
    
    U3[Unit Entity C] -->|Owner| P2[Player Entity 2]
    P2 -->|Member Of| T2[Team Entity Blue]
```

* * *

Video 8: Spatial Partitioning (Trees)
-------------------------------------

**Core Topic:** Collision and Proximity (The "Physics" of ME.BECS).

### Key Takeaways & Learns

*   **No Physics Engine:** ME.BECS uses QuadTrees (Trees Addon) instead of Unity Physics/Colliders for determinism.
*   **Seekers & Sensors:** To detect objects, attach a `QuadTreeQuery` aspect (Seeker). It queries the tree every frame.
*   **Optimization:** For "One-off" checks (e.g., "Find nearest tree to chop"), do not create a Sensor Entity. Use `GetNearest` API directly in the System logic to avoid the overhead of a persistent Seeker.
*   **Custom Filtering:** Use `ISubFilter` to inject logic into the tree query (e.g., "Find nearest unit with Health \< 50").

### Diagram: QuadTree Query

``` mermaid
---
config:
  theme: dark
---
graph TD
    A[System Update] --> B{Need continuous check?}
    
    B -- Yes (Aura/Range) --> C[Entity with Seeker Aspect]
    C --> D[Query QuadTree every Tick]
    D --> E[Store Results in Component]
    
    B -- No (Command) --> F[Direct API Call: GetNearest]
    F --> G[Apply Custom SubFilter]
    G --> H[Return Result immediately]
```


