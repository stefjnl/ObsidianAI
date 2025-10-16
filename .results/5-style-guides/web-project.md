# web-project Style Guide

- Keep `ObsidianAI.Web.csproj` aligned with the API project regarding target framework and nullable context to minimize analyzer noise.
- Use `<StaticWebAssetBasePath>` consistently when adding new component libraries to ensure assets publish correctly.
- Reference component libraries explicitly; avoid wildcard `Content` includes that might bloat publish output.
