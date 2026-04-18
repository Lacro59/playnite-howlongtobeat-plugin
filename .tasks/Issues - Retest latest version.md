# Retry with latest version

- #328 - Syncing playtime doesn’t sync game completion status on HLTB even with setting enabled
  - Link: [Issue #328](https://github.com/Lacro59/playnite-howlongtobeat-plugin/issues/328)
  - Note: verify whether this behavior is already covered by the fix from #337.

- #326 - empty setting
  - Link: [Issue #326](https://github.com/Lacro59/playnite-howlongtobeat-plugin/issues/326)
  - Status: Needs info / Retest latest version.
  - Note: logs show repeated HLTB auth issues (expired cookies + non-JSON response at GetUserId in HowLongToBeatApi.cs), but no clear HLTB Settings UI parsing exception.

- #290 - Setting a game to "Beaten" marks it to "Done" and "Completed" in How Long To Beat
  - Link: [Issue #290](https://github.com/Lacro59/playnite-howlongtobeat-plugin/issues/290)
  - Note: related to status mapping/sync behavior (same area as #328 and #337); retest on latest version.
