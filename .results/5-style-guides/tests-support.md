# tests-support Style Guide

- Keep factories lightweight and deterministic; reset state between tests to avoid cross-test pollution.
- Extend support utilities when new infrastructure components need fakes (e.g., additional repositories or services).
- Avoid referencing production-only dependencies; tests should compile without pulling in external SDKs.
