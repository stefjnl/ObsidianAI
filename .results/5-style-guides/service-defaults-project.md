# service-defaults-project Style Guide

- Keep project references minimal; only include packages required for shared service configuration (logging, telemetry, resilience).
- Align target frameworks and nullable settings with the rest of the solution to prevent analyzer discrepancies.
- Document any new shared defaults in the README so downstream services know what to expect when opting in.
