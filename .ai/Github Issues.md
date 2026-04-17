# Git Commit Message Convention

## 1. Template brut

```text
<type>(<scope>): <description>

<body>

<footer>
```

## 2. Guide de rédaction

### Format

- **Type** : `fix`, `feat`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`.
- **Scope** : optionnel, indique le module ou le fichier concerné, par exemple `api` ou `middleware`.
- **Subject** : utiliser l’impératif, sans majuscule au début, sans point final.
- **Body** : optionnel, explique le **pourquoi** et le **comment**, pas seulement le **quoi**.
- **Footer** : permet de référencer une issue, par exemple `Fixes #123`.

### Exemples

#### Correctif simple

```text
fix(ui): resolve button misalignment on login page
```

#### Correctif détaillé

```text
fix(api): handle null values in user profile update

The API was throwing a 500 error when the 'bio' field was sent as null.
Added a null-check validator before database persistence.

Closes #456
```

## 3. Cheat sheet GitHub

| Action          | Keyword                       | Syntax                   |
| --------------- | ----------------------------- | ------------------------ |
| Close an issue  | `Fixes`, `Closes`, `Resolves` | `Fixes #123`             |
| Link to issue   | `Ref`, `See`                  | `Ref #123`               |
| Multiple issues | `Fixes`                       | `Fixes #123, Fixes #124` |
