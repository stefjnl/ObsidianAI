# infrastructure-assembly-info Style Guide

- Only update `AssemblyInfo.cs` when project-wide attributes change (e.g., `[InternalsVisibleTo]` adjustments for test access).
- Keep attribute values synchronized with the domain/application projects so shared internals continue to compile during refactors.
