<!-- markdownlint-disable MD033 MD041 -->

[![Crowdin](https://badges.crowdin.net/playnite-extensions/localized.svg)](https://crowdin.com/project/playnite-extensions)
[![GitHub release](https://img.shields.io/github/v/release/Lacro59/playnite-howlongtobeat-plugin?logo=github&color=8A2BE2)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/Lacro59/playnite-howlongtobeat-plugin?logo=github)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/Lacro59/playnite-howlongtobeat-plugin/total?logo=github)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/releases)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/Lacro59/playnite-howlongtobeat-plugin/devel?logo=github)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/Lacro59/playnite-howlongtobeat-plugin?logo=github)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/graphs/contributors)
[![GitHub license](https://img.shields.io/github/license/Lacro59/playnite-howlongtobeat-plugin?logo=github)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/LICENSE)

[![HLTB Search Smoke](https://github.com/Lacro59/playnite-howlongtobeat-plugin/actions/workflows/hltb-search-smoke.yml/badge.svg)](https://github.com/Lacro59/playnite-howlongtobeat-plugin/actions/workflows/hltb-search-smoke.yml)

# HowLongToBeat for Playnite

Fetch, display, and sync [HowLongToBeat](https://howlongtobeat.com/) completion times and playtime directly inside [Playnite](https://playnite.link).

## ✨ Features

- **Time to beat data**: look up Main Story, Extra, Completionist, and related averages from HowLongToBeat for games in your library.
- **Profile & playtime sync**: view your HowLongToBeat data, upload Playnite playtime after a session, and optionally sync completion status both ways.
- **Tags & progress**: add time-to-beat tags automatically and show progress toward HowLongToBeat estimates in supported views.
- **VNDB support**: use [VNDB](https://vndb.org/) reading-time estimates as an alternate data source when it fits better than HowLongToBeat.
- **Search integrations**: filter your library by time to beat from Playnite Global Search and via the QuickSearch plugin.
- **Theme integration**: custom controls (`PluginButton`, `PluginProgressBar`, `PluginViewItem`) for game details and list views when the theme supports them.

## 📸 Screenshots

### Main interface

<a href="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/main_01.jpg?raw=true">
  <picture>
    <img alt="HowLongToBeat main window showing time-to-beat data for a selected game" src="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/main_01.jpg?raw=true" height="200px">
  </picture>
</a>
<a href="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/main_02.jpg?raw=true">
  <picture>
    <img alt="HowLongToBeat statistics and library overview" src="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/main_02.jpg?raw=true" height="200px">
  </picture>
</a>

### In-view controls

<a href="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/control_01.jpg?raw=true">
  <picture>
    <img alt="HowLongToBeat progress bar and controls in the game details view" src="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/control_01.jpg?raw=true" height="200px">
  </picture>
</a>

### Settings panel

<a href="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/settings_01.jpg?raw=true">
  <picture>
    <img alt="HowLongToBeat plugin settings for sync and data preferences" src="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/settings_01.jpg?raw=true" height="200px">
  </picture>
</a>
<a href="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/settings_02.jpg?raw=true">
  <picture>
    <img alt="HowLongToBeat plugin settings for appearance and mapping options" src="https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/forum/settings_02.jpg?raw=true" height="200px">
  </picture>
</a>

## 🔍 Global Search

HowLongToBeat registers a Playnite Global Search provider (`hltb`). Combine a game name keyword with optional flags.

Example queries:

- `zelda -time=<50h`
- `-np -fav`
- `rpg -stores=steam -time=10min<>5h`
- `-status=played,playing -time=>2h`

| Parameter  | Purpose                          | Syntax                                            | Example              |
| ---------- | -------------------------------- | ------------------------------------------------- | -------------------- |
| `-time`    | Filter by time to beat           | `-time=<value>`, `-time=>value`, `-time=min<>max` | `-time=<50min`       |
| `-np`      | Only games you have not played   | `-np`                                             | `-np`                |
| `-fav`     | Only favorites                   | `-fav`                                            | `-fav`               |
| `-stores`  | Filter by library source name    | `-stores=name` or `-stores=a,b`                   | `-stores=steam,epic` |
| `-status`  | Filter by completion status name | `-status=name` or `-status=a,b`                   | `-status=played`     |

Notes:

- Flags can be combined in one query with an optional keyword.
- Parameter names are case-insensitive.
- Time values accept units such as `h`, `min`, and `s` (for example `5h`, `50min`, `30s`).

## 🔍 QuickSearch

HowLongToBeat integrates with Playnite QuickSearch (command key: `HowLongToBeat` / `hltb`) and adds a `ttb` sub-command to filter games by cached time to beat.

Example queries:

- `ttb > 2 h`
- `ttb < 30 s`
- `ttb 30 min <> 1 h`
- `ttb > 2 h -np`

| Parameter | Purpose                                     | Syntax                                                                                     | Example             |
| --------- | ------------------------------------------- | ------------------------------------------------------------------------------------------ | ------------------- |
| `ttb`     | Filter by time to beat                      | `ttb > <value> <unit>`, `ttb < <value> <unit>`, `ttb <min> <unit> <> <max> <unit>`          | `ttb 30 min <> 1 h` |
| `-np`     | Only games with no play activity (optional) | append `-np`                                                                               | `ttb > 2 h -np`     |

Notes:

- Units accepted: `h`, `min`, `s`.
- Commands are case-insensitive for parameter names.
- Requires the QuickSearch extension to be installed and the HowLongToBeat command enabled.

## ⚙️ Configuration

Open **Settings → Extensions → HowLongToBeat**.

### General behavior

- Enable header and sidebar integration buttons.
- Prefer HowLongToBeat cover images in plugin windows.
- Configure auto-accept and match threshold when selecting HowLongToBeat results.

### HowLongToBeat sync

- Sign in to your HowLongToBeat account.
- Automatically upload playtime after a game session (with optional confirmation and success notifications).
- Optionally send Playnite user score as the HowLongToBeat review score.
- Sync completion status between Playnite and HowLongToBeat, including playtime and completion date options.
- Maintain an ignore list for games excluded from automatic playtime sync.

### Data preferences

- Choose Classic / Average / Median / Rushed / Leisure data types to display.
- Set the preferred time-to-beat metric and default data provider (HowLongToBeat or VNDB).
- Export cached data to CSV or JSON.

### Tags & mapping

- Enable automatic time-to-beat tags.
- Map Playnite platforms and library sources to HowLongToBeat platforms and storefronts.
- Maintain title aliases when Playnite names differ from HowLongToBeat titles.

### Appearance

- Toggle progress bar options in data views (tooltips, time labels, placement).
- Enable theme controls for buttons, progress bars, and list items.

> Sign in under the HowLongToBeat sync settings if you want playtime or status upload. Local time-to-beat lookup works without an account.

## 📥 Installation

### Install from Playnite Add-ons Browser (recommended)

1. Open Playnite.
2. Go to **Add-ons → Browse → Generic**.
3. Search for `HowLongToBeat` and install it.
4. Restart Playnite if requested.

Official Playnite guide: [Installing Extensions](https://api.playnite.link/docs/manual/features/extensionsSupport/installingExtensions.html)

### Manual installation (`.pext`)

1. Download the latest `.pext` file from [Releases](https://github.com/Lacro59/playnite-howlongtobeat-plugin/releases/latest).
2. In Playnite, open **Add-ons → Install from file**.
3. Select the downloaded `.pext`.
4. Restart Playnite, then optionally sign in under **Settings → Extensions → HowLongToBeat**.

## 🤝 Contributing & Feedback

- **Bug reports**: [Open an issue](https://github.com/Lacro59/playnite-howlongtobeat-plugin/issues/new?template=bug_report.md)
- **Feature requests**: [Request an enhancement](https://github.com/Lacro59/playnite-howlongtobeat-plugin/issues/new?template=feature_request.md)
- **Pull requests**: [Submit a PR](https://github.com/Lacro59/playnite-howlongtobeat-plugin/pulls) targeting the `devel` branch
- **Translations**: [Contribute on Crowdin](https://crowdin.com/project/playnite-extensions)
- **Wiki & troubleshooting**: [Project wiki](https://github.com/Lacro59/playnite-howlongtobeat-plugin/wiki) (including [custom theme integration](https://github.com/Lacro59/playnite-howlongtobeat-plugin/wiki/Addition-in-a-custom-theme))

## 💝 Support

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-FF5E5B?logo=ko-fi&logoColor=white)](https://ko-fi.com/lacro59)

If this plugin helps you, you can also support:

- [Playnite](https://www.patreon.com/playnite)
- [HowLongToBeat](https://howlongtobeat.com/)

## 📄 License

This project is licensed under the [MIT License](https://github.com/Lacro59/playnite-howlongtobeat-plugin/blob/master/LICENSE).
