# Claude Code Commands - CQRS Event Sourcing Template

Diese Slash Commands helfen bei der strukturierten Entwicklung mit dem CQRS Event Sourcing Pattern.

## Verfügbare Commands

### `/add-feature` - Neues Feature implementieren

**Use Case**: Komplett neues Command mit Event, Projection, Controller und Tests erstellen.

**Input**: Verbale Feature-Beschreibung

**Beispiele**:
```
/add-feature
"User soll seine Email-Adresse ändern können"

/add-feature
"Admins sollen User deaktivieren können"

/add-feature
"User können ihre Sessions manuell beenden"
```

**Output**:
- Command + Event Records
- CommandService Methode
- Projection Update (Create oder Apply)
- Controller Endpoint
- FluentValidation Validator
- Unit Tests (Happy Path + Error Cases)
- Build & Test Execution

**Pattern**: Folgt exakt CLAUDE.md Architektur
- `ExecuteCommandInSession()` mit Session-Validierung
- BCrypt für Passwörter
- Structured Logging
- Testbare GuidProvider/DateTimeProvider

---

### `/extend-aggregate` - Aggregate erweitern

**Use Case**: Neue Property oder Event zu existierendem Aggregate hinzufügen.

**Input**: Beschreibung der Erweiterung

**Beispiele**:
```
/extend-aggregate
"User soll eine Telefonnummer haben"

/extend-aggregate
"Session soll IP-Adresse speichern"
```

**Output**:
- Aggregate Record erweitert (non-breaking mit Default-Werten)
- Neues Event + Command (falls nötig)
- Projection Apply-Methode
- Migration Notes

**Best Practice**: Immer non-breaking changes via optionale Properties!

---

### `/add-validator` - Validator erstellen

**Use Case**: FluentValidation Validator für existierendes Command hinzufügen.

**Input**: Command-Name

**Beispiele**:
```
/add-validator
CreateUserCmd

/add-validator
ChangeEmailCmd
```

**Output**:
- Validator mit passenden Rules für jeden Property-Typ
- Beispiele für String, Email, Guid, Password, etc.
- Optional: Validator Tests

**Katalog**: Enthält Beispiele für alle gängigen Validation-Szenarien

---

## Workflow-Übersicht

### Typischer Feature-Flow

1. **Planning**: User beschreibt Feature verbal
2. **`/add-feature`**: LLM implementiert komplett (Cmd, Event, Service, Controller, Tests)
3. **Build & Test**: `dotnet build && dotnet test`
4. **Review**: User prüft Business Logic, Validation, Tests
5. **Commit**: `git add . && git commit -m "feat: ..."`

### Erweitungs-Flow

1. **`/extend-aggregate`**: Property hinzufügen
2. **`/add-validator`**: Validation Rules ergänzen (optional)
3. **Manual**: Tests erweitern
4. **Commit**: `git add . && git commit -m "feat: ..."`

---

## Best Practices

### ✅ DO

- Nutze `/add-feature` für neue Commands (automatisiert den kompletten Flow)
- Folge dem Pattern aus CLAUDE.md exakt
- Lass LLM Build & Tests ausführen (spart Zeit)
- Review Business Logic manuell (LLM kann Domänen-Logik nicht 100% kennen)

### ❌ DON'T

- Manuelle Implementierung ohne Commands (fehleranfällig, inkonsistent)
- Pattern abweichen (macht Codebase inkonsistent)
- Ohne Tests committen (Commands generieren Tests automatisch)

---

## Erweiterung der Commands

Du kannst eigene Commands hinzufügen:

```markdown
# My Custom Command

<!-- Beschreibung -->

## Input
<!-- Was braucht der Command? -->

## Workflow
<!-- Schritte -->

## Output
<!-- Was wird erstellt? -->
```

Speichere als `.claude/commands/my-command.md`.

---

## Technische Details

### Command Execution

Claude Code führt Commands aus via:
1. User tippt `/add-feature` in Chat
2. Claude Code expandiert `add-feature.md` als System Prompt
3. LLM folgt den Anweisungen aus dem Command
4. Output ist strukturiert und konsistent

### Context Window Optimization

Commands sind so designed, dass sie:
- ✅ EXAKTE Patterns vorgeben (reduziert LLM-Kreativität)
- ✅ Templates enthalten (kein "erfinde ein Pattern")
- ✅ Validierung durch Build & Tests (kein "hoffentlich kompiliert es")
- ✅ Klein genug für Context Window (kein Memory Overflow)

### Integration mit CLAUDE.md

Commands referenzieren CLAUDE.md als "Single Source of Truth":
- Pattern aus CLAUDE.md wird 1:1 übernommen
- Keine Abweichungen erlaubt
- CLAUDE.md als Project Memory File geladen

---

## Support

Bei Fragen oder Problemen:
1. Prüfe CLAUDE.md (enthält alle Patterns)
2. Prüfe docs/Scaffolding.md (erklärt Architektur)
3. Prüfe docs/architecture-decisions.md (erklärt "Warum?")

**Feedback**: Wenn ein Command nicht wie erwartet funktioniert, passe die .md-Datei an!

---

**Happy Coding!** 🚀
