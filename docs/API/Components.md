# API Reference: Components

This document provides a reference for the core component interfaces and attributes in ME.BECS. Components are `structs` that hold the data for an entity.

## Core Interfaces

All components must implement one of these base interfaces.

### `IComponent`

The most common interface. This marks a struct as a standard component that can be attached to an entity.

**Definition:**
```csharp
public interface IComponent : IComponentBase {}
```

**Usage:**
```csharp
public struct Health : IComponent
{
    public sfloat value;
}
```

---

### `IComponentShared`

This interface is for "shared" components. The data for these components is not stored per-entity, but in a shared location. All entities that have the same shared component data (determined by a hash) point to the same data instance. This is a powerful memory optimization for data that is identical across many entities (e.g., material properties, physics layers).

**Definition:**
```csharp
public interface IComponentShared : IComponent {
    uint GetHash();
}
```

**Usage:**
*   The `GetHash()` method is optional. If not implemented, the framework will use the type as the hash, meaning all entities with this component will share one instance of it.
*   If you implement `GetHash()`, you can create different groups of shared data based on the hash value.

```csharp
public struct MaterialInfo : IComponentShared
{
    public int materialId;
    public int colorId;

    // Entities with the same materialId and colorId will share this component data.
    public uint GetHash() => (uint)this.materialId ^ (uint)this.colorId;
}
```

---

### `IComponentDestroy`

This interface allows a component to run custom logic when it is removed from an entity or when the entity is destroyed. This is useful for cleaning up external resources or unmanaged memory that might be associated with the component.

**Definition:**
```csharp
public interface IComponentDestroy : IComponent {
    void Destroy(in Ent ent);
}
```

**Usage:**
```csharp
public struct CustomCollection : IComponentDestroy
{
    public MemPtr data; // Pointer to some unmanaged data

    public void Destroy(in Ent ent)
    {
        // Free the unmanaged memory when the component is removed.
        MemoryAllocator.Free(ent.World.state, this.data);
    }
}
```

---

## Config Component Interfaces

These interfaces are used for components that are part of an `EntityConfig`.

### `IConfigComponentStatic`

Marks a component within an `EntityConfig` as **static**. Its data is stored only once on the config asset itself and is shared by all entities instantiated from it. This is highly memory efficient for data that never changes at runtime.

**Usage:**
*   Access this data via `ent.ReadStatic<T>()`.
*   You cannot get write access to this data (`ent.Get<T>()` will not work).

```csharp
// In an EntityConfig, this component's data is shared, not copied.
public struct UnitStaticData : IConfigComponentStatic
{
    public sfloat maxHealth;
    public sfloat moveSpeed;
}
```

---

### `IConfigInitialize`

Allows a component to run custom initialization logic when its `EntityConfig` is applied to an entity.

**Definition:**
```csharp
public interface IConfigInitialize {
    void OnInitialize(in Ent ent);
}
```

---

## Attributes

### `[ComponentGroup(System.Type groupType)]`

Assigns a component to a specific group. Entity versions are tracked per-group. When a component in a group is modified, only that group's version number is incremented. This allows for more granular reactivity, as systems can subscribe to changes in specific component groups instead of the entire entity.

**Usage:**
```csharp
public struct CoreComponentGroup {}

[ComponentGroup(typeof(CoreComponentGroup))]
public struct Position : IComponent { ... }
```

---

### `[EditorComment("...")]`

Adds a help box comment to the component's inspector drawer within the Unity Editor, making it easier for designers and other team members to understand its purpose.

**Usage:**
```csharp
[EditorComment("Stores the current and max health of an entity.")]
public struct Health : IComponent { ... }
```
