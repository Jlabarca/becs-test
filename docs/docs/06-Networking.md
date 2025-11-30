# 06: The Networking Model

ME.BECS is architected around a high-performance, deterministic networking model designed for real-time multiplayer games. It uses a client-side prediction and server rollback architecture to provide a responsive player experience even with network latency.

## Architecture: Prediction, Rollback, and Determinism

The entire model is built on three pillars: the client predicts, the server validates, and deterministic logic ensures both can arrive at the same result.

```mermaid
---
config:
  theme: dark
---
graph LR
    subgraph Client
        A[Input Hardware] --> B[Input System]
        B --> C{Diff vs Last Frame?}
        C -- Yes --> D[Send Network Event to Server]
        D --> E[Apply Input Locally (Prediction)]
        E --> F[Render Predicted Frame]
    end

    subgraph Server
        D -.-> G[Receive Input Packet]
        G --> H[Run Logic Tick]
        H --> I[Authoritative State]
    end

    subgraph Client Correction
        I -.-> J[Receive Server State]
        J --> K{State Mismatch?}
        K -- Yes --> L[Rollback & Replay Inputs]
        L --> F
        K -- No --> F
    end
    
    style E fill:#4682B4,stroke:#fff
    style L fill:#B22222,stroke:#fff
```

### 1. Client-Side Prediction

When a player performs an action (e.g., presses a move key), the input is not held until the server responds. Instead:
1.  The input is immediately sent to the server as a **Network Event** or **Command**.
2.  Simultaneously, the client's **Logic World** *predicts* the outcome of that input and simulates the next few ticks locally.
3.  The **Visual World** renders this predicted state.

To the player, the action appears to happen instantly.

### 2. Server Authority

The server is the ultimate source of truth.
1.  It receives a stream of input events from all clients.
2.  It processes these inputs in its own deterministic simulation (the authoritative Logic World).
3.  Periodically, it sends snapshots of the authoritative game state back to the clients.

### 3. Rollback and Replay

When the client receives a state snapshot from the server, it compares it to its own predicted state for that same tick.

*   **If they match:** The prediction was correct, and everything continues smoothly.
*   **If they mismatch:** A "misprediction" occurred. This could be due to another player's action that the client hadn't received yet, or network latency. The framework then automatically performs a **rollback**:
    1.  The client's Logic World is reset to the last known authoritative state from the server.
    2.  It then **replays** all the local player's inputs from that point forward to the present tick.
    3.  The Visual World snaps to this newly corrected state.

Because this process is extremely fast, the correction is often imperceptible to the player, appearing as a minor visual "pop" or adjustment.

## Determinism: The Cornerstone of Networking

This entire model falls apart if the client and server simulations don't produce the exact same results from the same inputs. **Deterministic logic is a strict requirement.**

### Best Practices for Determinism:

1.  **No `UnityEngine.Random`:** Use the framework's provided random number generator, which can be seeded to produce a repeatable sequence.
2.  **No `Time.deltaTime`:** The simulation is advanced in fixed-step ticks. Use the `deltaTime` provided by the system context or injected into jobs.
3.  **No `UnityEngine.Physics`:** Unity's physics engine is not deterministic. For collision and spatial queries, use the **Trees (QuadTree/Octree) Addon** provided with ME.BECS.
4.  **Use Deterministic Math:** Use the framework's `sfloat` fixed-point math types and functions from the `ME.BECS.Math` namespace for any gameplay calculations.
5.  **Floating Point Order of Operations:** Be aware that floating-point math is not perfectly associative (e.g., `(a + b) + c` is not always identical to `a + (b + c)`). Process inputs and calculations in a consistent order.

## Network Events & Commands

Inputs are sent as simple `structs` that implement `INetworkEvent`.

```csharp
using ME.BECS.Network;

public struct MoveCommand : INetworkEvent
{
    public int targetEntityId;
    public float2 targetPosition;
}

// On the client:
var cmd = new MoveCommand { ... };
// Get the network module and send the event
networkModule.AddEvent(cmd);

// On the server (or in a logic system):
// The event will be available in the context to be processed.
```

By adhering to these principles, you can build complex, responsive, and robust multiplayer experiences with ME.BECS.
