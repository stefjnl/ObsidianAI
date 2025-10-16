# api-launchsettings Style Guide

- Offer both HTTP and HTTPS profiles but keep `launchBrowser` disabled; the Aspire dashboard typically opens the correct URL instead.
- Reuse `http://localhost:5095` for local runs to match Docker and compose defaults.
- Set `dotnetRunMessages` to true so developers get endpoint summary output when running `dotnet run` from the project directory.
