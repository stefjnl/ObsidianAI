# tests-infrastructure Style Guide

- Exercise repository behavior against the in-memory SQLite factory to mimic production EF Core behavior.
- Seed data using helpers in `TestEntityFactory`; avoid raw SQL inserts to keep tests portable.
- Assert on domain models rather than EF entities where possible to validate mapping integrity.
