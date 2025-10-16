# domain-assembly-info Style Guide

- Keep `AssemblyInfo.cs` minimal; add domain-specific assembly attributes here only if needed for serialization or testing.
- The file exists to satisfy legacy tooling—do not remove it unless the build system no longer requires an assembly stub.
