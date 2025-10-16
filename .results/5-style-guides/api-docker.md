# api-docker Style Guide

- Use the two-stage build (SDK 9.0 → ASP.NET 9.0) to compile and publish the API; copy the entire solution into `/src` so project references remain intact.
- After publishing, create `/app/data` in the runtime image; the container mounts the SQLite volume there, so keep the directory in the image even before the volume attaches.
- Bind the web server to `http://+:8080` and expose port `8080`; compose files map external ports as needed.
- Keep the entrypoint as `dotnet ObsidianAI.Api.dll` with no shell wrapper to ensure container shutdown honors SIGTERM.
