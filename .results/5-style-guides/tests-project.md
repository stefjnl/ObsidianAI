# tests-project Style Guide

- Keep the test project targeting the same framework as production projects to ensure parity (currently net8.0).
- Reference only the projects under test plus required test libraries (xUnit, FluentAssertions); avoid transitive package creep.
