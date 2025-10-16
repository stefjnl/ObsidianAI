# infrastructure-migrations Style Guide

- Generate migrations via `dotnet ef migrations add <Name>` from the Infrastructure project to keep designer metadata in sync.
- Review generated SQL for destructive changes; prefer additive updates and create custom SQL in `Up`/`Down` if EF guesses are unsafe.
- Commit both the migration and the updated model snapshot together to avoid version skew.
