# Checklist - SystemChecker and GameActivity Alignment

> Scope: apply in this repository the same evolutions already made in  
> `playnite-systemchecker-plugin/source` and `playnite-gameactivity-plugin/source`,  
> without handling internal `playnite-plugincommon` changes.

## 1) Database class renaming (legacy -> new base)

- [x] Replace `PluginDataBaseGameBase` with `PluginGameEntry` in:
  - [x] `source/Controls/PluginButton.xaml.cs`
  - [x] `source/Controls/PluginProgressBar.xaml.cs`
  - [x] `source/Controls/PluginViewItem.xaml.cs`
- [x] Replace `PluginDataBaseGame<...>` with `PluginGameCollection<...>` in:
  - [x] `source/Models/GameHowLongToBeat.cs`
- [ ] Check whether any `PluginDataBaseGameDetails<..., ...>` references remain and migrate them to `PluginGameCollectionWithDetails<..., ...>` if present
- [x] Ensure there are no remaining `PluginDataBaseGame*` occurrences in `source` (excluding `playnite-plugincommon`)

## 2) Core architecture alignment (pattern already applied elsewhere)

- [x] Fix `SettingsRoot` to point to `PluginSettingsViewModel.Settings` (as in SystemChecker/GameActivity)
  - [x] File: `source/HowLongToBeat.cs`
- [x] Extract menu logic from `HowLongToBeat.cs` into a dedicated `HowLongToBeatMenus` service
  - [x] `GetGameMenuItems(...)`
  - [x] `GetMainMenuItems(...)`
  - [x] Initialize `_menus` in the plugin constructor
- [x] Introduce a `HowLongToBeatWindows` service and centralize plugin window opening
  - [x] Call from the custom button (`OnCustomThemeButtonClick`)
  - [x] Calls from menus where relevant
- [x] Verify alignment with shared interfaces (`IPluginWindows`, etc.) without modifying `playnite-plugincommon`

## 3) Export / shared tools

- [x] Verify `PluginExportCsv` integration in main menus (similar pattern to other plugins)
- [x] Add/adapt menu entries if CSV export is not yet exposed

## 4) Technical migration cleanup

- [x] Check whether any legacy LiteDB/NuGet migration leftovers exist in this plugin (excluding `playnite-plugincommon`)
  - [x] `packages.config` is still referenced in `source/HowLongToBeat.csproj`
  - [x] Confirm whether removal is needed to stay consistent with other plugins

## 5) Validation

- [ ] Build the solution
- [ ] Verify compilation without errors on modified files
- [ ] Verify plugin menus and windows load correctly in Playnite runtime
- [ ] Verify quick non-regression: plugin view display, data refresh, main menu actions
