# web-program Style Guide

- Keep Program.cs minimal: configure services (SignalR, HttpClient, resilience) via extension methods and map endpoints at the end.
- Register shared defaults (ServiceDefaults) before feature-specific services to ensure logging and telemetry are active.
- Maintain the same middleware ordering as the API when adding cross-cutting features (exception handling, HTTPS redirection).
