# Renaming Task — Plugin Database Classes

## Renames

| Old                                 | New                                              |
| ----------------------------------- | ------------------------------------------------ |
| `PluginDataBaseGameBase`            | `PluginGameEntry`                                |
| `PluginDataBaseGame<T>`             | `PluginGameCollection<T>`                        |
| `PluginDataBaseGameDetails<T, Y>`   | `PluginGameCollectionWithDetails<T, TDetails>`   |
| Paramètre de type`Y`                | `TDetails`                                       |

---

## Search & Replace

```text
Find    : PluginDataBaseGameBase
Replace : PluginGameEntry

Find    : PluginDataBaseGame<
Replace : PluginGameCollection<

Find    : PluginDataBaseGameDetails<
Replace : PluginGameCollectionWithDetails<
```

---

## Checklist

- [ ] `PluginGameEntry.cs`
- [ ] `PluginGameCollection.cs`
- [ ] `PluginGameCollectionWithDetails.cs`
- [ ] Build solution
