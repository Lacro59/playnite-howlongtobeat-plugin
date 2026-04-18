# 🤖 Playnite Plugin Development

## 👤 User Context
- **Profile:** Experienced developer.
- **Communication Style:** Direct, technical, and concise. No conversational filler or unnecessary apologies.
- **Objective:** Production-ready, maintainable, and high-performance code for Playnite.
- **Role:** Help **improve existing code** (refactors, bug fixes, UX) and **apply coding best practices and clean code** in every change. When writing or reviewing code, enforce these standards and suggest concrete improvements when the current code diverges from them.

---

## 📚 Official Playnite Documentation

- **Tutorials:** https://api.playnite.link/docs/tutorials/index.html  
- **API Reference:** http://api.playnite.link/docs/api/index.html  
- Always align implementation with official Playnite documentation before applying custom patterns.

---

## ❓ Clarification & Decision Policy

- **Ambiguity Handling:** If requirements are ambiguous, incomplete, contradictory, or technically unclear, request clarification before implementation.
- **Missing Dependencies:** If a required file, interface, model, API contract, or configuration is missing, explicitly request it.
- **Architectural Choices:** If multiple valid architectural approaches exist and the choice impacts structure, maintainability, or performance, briefly present the alternatives and request direction.
- **No Hidden Assumptions:** Do not silently assume business logic, data structure, or workflow behavior.
- **Explicit Assumptions:** If assumptions are strictly necessary to proceed, clearly state them before implementation.
- **Scope Validation:** If the request exceeds defined constraints (framework version, C# version, plugin architecture), explicitly flag the conflict before proceeding.
- **Partial Context:** If only partial code is provided, do not invent surrounding architecture. Request missing context when required for correctness.

---

## 🌍 Language & Documentation
- **Code & Docs:** All code elements, comments, and documentation (XML Doc, README) must be in **English**.
- **Always document:** Create documentation (XML Doc on public API, README when relevant) for any new or modified code; do not leave public types or members undocumented.
- **Relevant comments:** Add comments where they add value: non-obvious logic, business rules, workarounds, non-trivial algorithms, or non-obvious "why" (not redundant "what"). Avoid comments that merely repeat the code.

---

## 📏 Naming Conventions (Standard C#)
- **PascalCase:** Classes, Methods, Properties, Public Fields, and Enums.
- **camelCase:** Local variables and Method parameters.
- **_camelCase:** Private fields (must start with an underscore).
- **Interfaces:** Must always start with a capital "I" (e.g., `IGameProvider`).
- **Meaningful Names:** Avoid abbreviations (use `userRepository` instead of `uRepo`).

---

## ⚙️ Versions & Frameworks Specifications
- **Target Framework:** .NET Framework 4.6.2 (C#).
- **SDK:** `Playnite.SDK` (Latest compatible with 4.6.2).
- **Strict Compliance:** Use **ONLY** features compatible with **C# 7.0** and below.
- **Restrictions:**
    - No range operators (`..`).
    - No `using` declarations without braces.
    - No `records` or `init-only` properties.
    - No `switch` expressions (use `switch` statements).
    - No `nullable reference types` (C# 8.0 features).
    - No pattern matching enhancements beyond C# 7.0.
- **Dependencies:** Ensure all NuGet packages are compatible with .NET Framework 4.6.2.

---

## 📐 Pattern & Dependency Priority

When creating or reusing patterns, follow this **strict priority order**:

1. **`source/playnite-plugincommon/` (first)**  
   - Use or extend existing code in the project's common plugin library.  
   - Prefer improving shared utilities, helpers, and UI components there rather than duplicating logic in the plugin.

2. **Playnite SDK**  
   - Use types, APIs, and patterns from `Playnite.SDK` (logging, serialization, dialogs, database, etc.) before writing custom equivalents.

3. **Native .NET / BCL**  
   - Use .NET Framework BCL or simple, well-known patterns (e.g. `HttpClient`, `MemoryCache`, `Task`, standard collections) when the above do not cover the need.

4. **NuGet library**  
   - Propose a NuGet package only when native code would be significantly more complex, error-prone, or hard to maintain.  
   - When suggesting a library: **explicitly state pros and cons vs. native code** for the given use case (e.g. maintenance, surface area, compatibility with .NET 4.6.2, licensing, bundle size).  
   - Do not add a dependency without this comparison.

**Summary:** Prefer `playnite-plugincommon` → Playnite SDK → native/BCL → NuGet (with pros/cons vs native).

---

## 🎮 Playnite Plugin Specifics

### Plugin Architecture
- **Environment:** Playnite Desktop Client Plugin.
- **Base Classes:** Implement `GenericPlugin`, `LibraryPlugin`, or `MetadataPlugin` depending on plugin type.
- **API Access:** Always pass `IPlayniteAPI` via dependency injection to sub-services.
- **Settings:** Implement `ISettings` interface and use `GetSettings<T>()` pattern.

### Plugin Lifecycle
- **Initialization:** `OnApplicationStarted()` - Use for startup tasks.
- **Game Events:** `OnGameInstalled()`, `OnGameUninstalled()`, `OnGameStarting()`, `OnGameStarted()`, `OnGameStopped()`.
- **Library Events:** `OnLibraryUpdated()` - For library plugins.
- **Shutdown:** `OnApplicationStopped()` - Clean up resources.

### Core Features
- **Game Actions:** Override `GetGameActions()` to add custom game actions.
- **Menu Items:** Override `GetGameMenuItems()` and `GetMainMenuItems()` for UI integration.
- **Metadata Providers:** Implement `OnDemandMetadataProvider` for metadata plugins.
- **Database Access:** Use `IPlayniteAPI.Database` for game operations.

### API Usage
- **Logging:** Use `Playnite.SDK.LogManager.GetLogger()` for all logging.
- **Serialization:** Use `Playnite.SDK.Data.Serialization` for JSON operations.
- **Resources:** Access via `ResourceProvider.GetString("LOC_KEY")` for localization.
- **Dialogs:** Use `IPlayniteAPI.Dialogs` for user notifications and input.

### UI / XAML
- For views, controls, styling, and localization: follow **`.ai/Playnite UI Modernization, Localization & Common Styling.md`** (theme variables in `ResourcesPlaynite/Constants.xaml`, styles in `ResourcesPlaynite/Common.xaml` and `Resources/Common.xaml`, LocSource, MVVM).

---

## 🧵 Threading & Asynchrony
- **UI Thread Access:** Use `Application.Current.Dispatcher.Invoke()` or `InvokeAsync()` for UI updates.
- **Async/Await:** Supported in .NET 4.6.2. Use for I/O-bound operations.
- **Background Work:** Use `Task.Run()` for CPU-bound operations.
- **Blocking Calls:** **NEVER** block UI thread with `.Result` or `.Wait()`. Always use `await` or callbacks.
- **Cancellation:** Use `CancellationToken` for long-running operations.

---

## 🚀 Performance Guidelines
- **Caching:**
    - Cache metadata and images to avoid repeated API calls.
    - Use `System.Runtime.Caching.MemoryCache` for in-memory caching.
    - Invalidate cache appropriately on data updates.
- **Lazy Loading:** Use `Lazy<T>` for expensive initializations.
- **Batch Operations:**
    - Use `IPlayniteAPI.Database.Games.BeginBufferUpdate()` / `EndBufferUpdate()` for bulk updates.
    - Minimize database queries in loops.
- **Resource Management:**
    - Always dispose `HttpClient`, `Stream`, database connections.
    - Use `using` statements with explicit braces for `IDisposable` objects.
- **Image Handling:**
    - Resize images before storing in Playnite database.
    - Use appropriate image formats (WebP for size, PNG for quality).

---

## 🛠 Coding Standards
- **Design Principles:** Follow SOLID principles and clean architecture.
- **Clean Code (mandatory):** Apply clean code philosophy in all new or modified code: small, single-responsibility functions; meaningful names; minimal nesting and early returns; avoid duplication (DRY); keep methods and classes focused and readable. Prefer clarity over cleverness.
- **Improve existing code:** When touching existing code, bring it in line with these standards: refactor when it improves readability or maintainability, add guard clauses, extract methods, reduce complexity. Do not leave clearly improvable code as-is without at least proposing the improvement.
- **Suggest improvements:** When reviewing or analyzing code, identify and propose **concrete** improvements so the code better respects clean code (e.g. extracting methods, renaming, reducing complexity, removing duplication). Do not limit to describing issues—suggest the change or show the refactor when relevant.
- **Typing:** Strict typing is mandatory. No `dynamic` unless absolutely necessary.
- **Error Handling:**
    - Use robust try-catch blocks around external API calls.
    - Use guard clauses at method entry points.
    - Log all exceptions with context using `LogManager.GetLogger().Error(ex, "context")`.
- **Null Checks:** Always validate inputs and check for null before dereferencing.
- **Async Methods:** Methods returning `Task` should have `Async` suffix (e.g., `LoadDataAsync()`).

---

## 📊 Logging Best Practices
- **Levels:**
    - `Debug`
    - `Info`
    - `Warn`
    - `Error`
- **Exception Logging:** Always use `logger.Error(ex, "Context message")`.
- **Sensitive Data:** Never log passwords, API keys, or personal data.

---

## 🧪 Testing
- **Framework:** NUnit 3.x (.NET Framework 4.6.2 compatible).
- **Mocking:** Moq 4.x.
- **Coverage:** >70% on business logic.
- **Structure:** Arrange / Act / Assert.
- **Naming:** `MethodName_Scenario_ExpectedBehavior`.

---

## 📝 Response Rules
1. **Code First**
2. **Modular**
3. **XML Documentation** — Document public API; create or update README when relevant.
4. **Relevant comments** — Comment non-obvious logic, business rules, workarounds, and non-trivial "why"; avoid redundant or filler comments.
5. **Complete working solutions**
6. **Follow Clarification Policy before implementation**
7. **Best practices & clean code** — Apply and enforce coding best practices and clean code in every proposal; when improving existing code, suggest or apply concrete refactors that align with these rules.

---

## 🚫 Out of Scope
- No deprecated code (unless required by 4.6.2).
- No obvious comments.
- No filler.
- No TODO placeholders.

---

## ✅ Quality Checklist
- [ ] Compiles on .NET Framework 4.6.2
- [ ] No C# 8.0+ features
- [ ] IDisposable wrapped in `using`
- [ ] External calls protected
- [ ] Logging implemented
- [ ] Dispatcher for UI operations
- [ ] Naming conventions respected
- [ ] No hardcoded strings
- [ ] Documentation and relevant comments in place (XML Doc, README when needed)
- [ ] Clean code principles applied (small functions, clear names, low complexity)
- [ ] Assumptions stated if required

---

## 📋 Consistency Notes (project vs rules)
- **Plugin type:** This project uses `PluginExtended` from `playnite-plugincommon` (extends `GenericPlugin`). Base is in `source/playnite-plugincommon/`.
- **Tests:** No test project in repo yet; Testing section is the target when tests are added.
- **UI:** XAML and localization must follow `.ai/Playnite UI Modernization, Localization & Common Styling.md`. Theme variables: `CommonPluginsResources/ResourcesPlaynite/Constants.xaml`. Base styles: `ResourcesPlaynite/Common.xaml` (e.g. `BaseTextBlockStyle`). Derived styles: `CommonPluginsResources/Resources/Common.xaml`. Plugin loads `Resources\Common.xaml` at runtime.

---

**Last Updated:** 2026-02-28  
**Version:** 2.7
