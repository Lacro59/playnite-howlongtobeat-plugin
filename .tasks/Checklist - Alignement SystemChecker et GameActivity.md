# Checklist - SystemChecker and GameActivity Alignment

> Scope: apply in this repository the same evolutions already made in  
> `playnite-systemchecker-plugin/source` and `playnite-gameactivity-plugin/source`,  
> without handling internal `playnite-plugincommon` changes.

## 1) Database class renaming (legacy -> new base)

- [ ] Replace `PluginDataBaseGameBase` with `PluginGameEntry` in:
  - [ ] `source/Controls/PluginButton.xaml.cs`
  - [ ] `source/Controls/PluginProgressBar.xaml.cs`
  - [ ] `source/Controls/PluginViewItem.xaml.cs`
- [ ] Replace `PluginDataBaseGame<...>` with `PluginGameCollection<...>` in:
  - [ ] `source/Models/GameHowLongToBeat.cs`
- [ ] Check whether any `PluginDataBaseGameDetails<..., ...>` references remain and migrate them to `PluginGameCollectionWithDetails<..., ...>` if present
- [ ] Ensure there are no remaining `PluginDataBaseGame*` occurrences in `source` (excluding `playnite-plugincommon`)

## 2) Core architecture alignment (pattern already applied elsewhere)

- [ ] Fix `SettingsRoot` to point to `PluginSettingsViewModel.Settings` (as in SystemChecker/GameActivity)
  - [ ] File: `source/HowLongToBeat.cs`
- [ ] Extract menu logic from `HowLongToBeat.cs` into a dedicated `HowLongToBeatMenus` service
  - [ ] `GetGameMenuItems(...)`
  - [ ] `GetMainMenuItems(...)`
  - [ ] Initialize `_menus` in the plugin constructor
- [ ] Introduce a `HowLongToBeatWindows` service and centralize plugin window opening
  - [ ] Call from the custom button (`OnCustomThemeButtonClick`)
  - [ ] Calls from menus where relevant
- [ ] Verify alignment with shared interfaces (`IPluginWindows`, etc.) without modifying `playnite-plugincommon`

## 3) Export / shared tools

- [ ] Verify `PluginExportCsv` integration in main menus (similar pattern to other plugins)
- [ ] Add/adapt menu entries if CSV export is not yet exposed

## 4) Technical migration cleanup

- [ ] Check whether any legacy LiteDB/NuGet migration leftovers exist in this plugin (excluding `playnite-plugincommon`)
  - [ ] `packages.config` is still referenced in `source/HowLongToBeat.csproj`
  - [ ] Confirm whether removal is needed to stay consistent with other plugins

## 5) Validation

- [ ] Build the solution
- [ ] Verify compilation without errors on modified files
- [ ] Verify plugin menus and windows load correctly in Playnite runtime
- [ ] Verify quick non-regression: plugin view display, data refresh, main menu actions
