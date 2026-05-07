# Damage Floating Text Formatting Feature

## Overview

Added formatting support to the damage floating text system, allowing toggling between "Simple" and "Detailed" display modes:

- **Simple Mode**: No decimal point displayed, e.g., `10`, `13`
- **Detailed Mode**: Two decimal places displayed, e.g., `10.45`, `13.06`

---

## Implementation Details

### 1. New Enum Type

```csharp
public enum DamageTextFormat
{
    Simple = 0,    // No decimal point
    Detailed = 1,  // Two decimal places
}
```

### 2. Configuration

In the Inspector, find DamageFloatingTextManager and set:

```
Damage Text Format Setting
├─ Damage Text Format  [Simple / Detailed]  ← Select display format
```

### 3. Public API

#### Set Format
```csharp
// Set to Simple mode
DamageFloatingTextManager.Instance.SetDamageTextFormat(
    DamageFloatingTextManager.DamageTextFormat.Simple
);

// Set to Detailed mode
DamageFloatingTextManager.Instance.SetDamageTextFormat(
    DamageFloatingTextManager.DamageTextFormat.Detailed
);
```

#### Get Current Format
```csharp
DamageTextFormat currentFormat = DamageFloatingTextManager.Instance.GetDamageTextFormat();
```

#### Show with Custom Format (One-time)
```csharp
// Display with Detailed format even if global setting is Simple
DamageFloatingTextManager.Instance.ShowDamageText(
    DamageFloatingTextManager.DamageType.普通伤害,
    14.4f,
    targetPosition,
    DamageFloatingTextManager.DamageTextFormat.Detailed
);
```

---

## Formatting Examples

| Raw Damage | Simple Mode | Detailed Mode |
|---------|---------|---------|
| 10.45   | 10      | 10.45   |
| 13.06   | 13      | 13.06   |
| 0.67    | 0       | 0.67    |
| 14.40   | 14      | 14.40   |

---

## Workflow

### Default Behavior (Simple Mode)

```
ShowDamageText(DamageType.普通伤害, 14.4f, position)
↓
FormatDamageValue(14.4f, Simple)  // Use global setting
↓
(int)14.4 = "14"
↓
Display: 14
```

### Custom Format

```
ShowDamageText(DamageType.普通伤害, 14.4f, position, DamageTextFormat.Detailed)
↓
FormatDamageValue(14.4f, Detailed)  // Use passed format
↓
14.4f.ToString("F2") = "14.40"
↓
Display: 14.40
```

---

## Modified Files

- `DamageFloatingTextManager.cs`
  - Added `DamageTextFormat` enum
  - Added `m_DamageTextFormat` field
  - Overloaded `ShowDamageText()` to support custom format
  - Added `SetDamageTextFormat()` and `GetDamageTextFormat()` methods
  - Added private `FormatDamageValue()` methods

---

## Usage Recommendations

### Scenario 1: Development Debugging
Use **Detailed Mode** to verify exact damage calculation values:
```csharp
DamageFloatingTextManager.Instance.SetDamageTextFormat(
    DamageFloatingTextManager.DamageTextFormat.Detailed
);
```

### Scenario 2: Production Game
Use **Simple Mode** for cleaner floating text display:
```csharp
DamageFloatingTextManager.Instance.SetDamageTextFormat(
    DamageFloatingTextManager.DamageTextFormat.Simple
);
```

### Scenario 3: Mixed Usage
Use Detailed mode for special damage types, Simple for others:
```csharp
// Normal damage in Simple mode
ShowDamageText(DamageType.普通伤害, damage, pos);

// Critical damage in Detailed mode (emphasize precision)
ShowDamageText(DamageType.暴击伤害, damage, pos, DamageTextFormat.Detailed);
```

---

## Important Notes

1. **Floor Division**: Simple mode uses **floor division** `(int)value`
   - 10.99 → 10
   - 0.67 → 0

2. **Precision Loss**: Simple mode loses decimal information for UI display only
   - Actual damage calculation is not affected
   - Only floating text display is affected

3. **Dynamic Switching**: Can switch global format at any time
   - No need to restart game or scene
   - Takes effect immediately for new floating text

---

## Summary

This feature provides a flexible floating text formatting solution that meets both development debugging requirements (precise values) and production game requirements (aesthetic display).
