# API Reference: Ent

The `Ent` struct is the primary handle for an entity in ME.BECS. It is a lightweight struct containing the entity's ID, generation, and world ID. All operations on an entity, such as adding components or destroying it, are performed through methods on the `Ent` struct.

## Entity Lifecycle

### `Ent.New(...)`
Creates a new entity. There are several overloads for creating an entity in a specific world or within a system's context.

```csharp
// Create a new entity in the default world
var ent1 = Ent.New();

// Create a new entity in a specific world
var ent2 = Ent.New(in myWorld);

// Create a new entity from within a system
var ent3 = Ent.New(in systemContext);
```

### `ent.Destroy()`
Destroys an entity immediately. This removes the entity and all its components from the world.

```csharp
myEnt.Destroy();
```

### `ent.Destroy(tfloat lifetime)`
Adds a `DestroyWithLifetime` component to the entity. The `DestroyWithLifetimeSystem` will automatically destroy the entity after the specified lifetime (in seconds) has passed.

```csharp
// Destroy the entity after 5.5 seconds
myEnt.Destroy(5.5f);
```

### `ent.DestroyEndTick()`
Adds a component that flags the entity to be destroyed at the end of the current tick.

---

## State & Lifecycle Queries

### `ent.IsAlive()`
Returns `true` if the entity has been created and has not yet been destroyed. This is the most common check to ensure an entity handle is valid.

### `ent.SetActive(bool state)`
Activates or deactivates an entity by adding or removing the `IsInactive` tag component. Inactive entities are excluded from most queries.

### `ent.IsActive()`
Returns `true` if the entity does not have the `IsInactive` component.

---

## Cloning & Copying

### `ent.Clone()`
Creates a new entity and copies all components from the source entity to it.

### `ent.Clone(bool cloneHierarchy)`
If `cloneHierarchy` is `true`, this will clone the entity and its entire child hierarchy (from the Transform addon).

### `ent.CopyFrom(in Ent source)`
Copies all components from the `source` entity to the existing `ent`, overwriting any components that already exist on the destination.

---

## Standard Component Management

These are the most common methods for interacting with component data.

*   **`ent.Set<T>(in T data)`**: Adds or replaces a component on the entity.
*   **`ref T ent.Get<T>()`**: Gets a writable reference to a component. If the component does not exist, it is added with its default value.
*   **`ref readonly T ent.Read<T>()`**: Gets a read-only reference to a component. If the component does not exist, it returns a reference to a static default value. This is faster than `Get<T>()`.
*   **`bool ent.Has<T>()`**: Returns `true` if the entity has the component.
*   **`bool ent.Remove<T>()`**: Removes a component from the entity.
*   **`ent.SetTag<T>(bool value)`**: A convenience method for adding or removing "tag" components (components with no data).

---

## Shared Component Management

These methods are used to interact with `IComponentShared` data.

*   **`ent.SetShared<T>(in T data)`**: Sets a shared component. The data's hash determines which shared instance is used.
*   **`ref T ent.GetShared<T>()`**: Gets a writable reference to a shared component.
*   **`ref readonly T ent.ReadShared<T>()`**: Gets a read-only reference to a shared component.
*   **`bool ent.HasShared<T>()`**: Checks if the entity has a specific shared component.
*   **`bool ent.RemoveShared<T>()`**: Removes a shared component from the entity.

---

## One-Shot Components

### `ent.SetOneShot<T>(in T data, OneShotType type)`
Sets a component on an entity that will be automatically removed later.
*   `OneShotType.CurrentTick`: The component is removed at the end of the current tick.
*   `OneShotType.NextTick`: The component is added at the start of the next tick and removed at the end of it.

---

## Versioning

### `ent.Version` (Property)
Gets the entity's overall version number. This number increments every time any component is added, removed, or changed on the entity.

### `ent.GetVersion(uint groupId)`
Gets the version number for a specific `ComponentGroup`. This allows for more granular change detection.

---

## Read-Only Access: `EntRO`

The `EntRO` struct is a read-only wrapper around an `Ent`. It provides a subset of the `Ent` API that cannot modify state (e.g., `Read<T>`, `Has<T>`). It is useful for passing entities to systems or functions that should not be able to change them, enforcing correctness at compile time.

```csharp
public void MyReadOnlySystem(in EntRO ent)
{
    // This is allowed:
    if (ent.Has<Position>()) { ... }

    // This would cause a compile error:
    // ent.Set(new Position());
}
```
