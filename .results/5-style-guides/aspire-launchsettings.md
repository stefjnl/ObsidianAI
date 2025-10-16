# aspire-launchsettings Style Guide

- Enable `launchBrowser` for the AppHost profiles so the Aspire dashboard opens automatically when running locally.
- Keep both HTTP and HTTPS profiles with the generated random ports; Aspire tooling coordinates multiple services and expects the resource/OTLP endpoints reflected here.
- Ensure both `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` are set to `Development`; the orchestrator reads either variable depending on host.
