# domain-project Style Guide

- The Domain project targets `net9.0` with warnings treated as errors; ensure new code meets nullable and analyzer expectations.
- Generate XML documentation but suppress CS1591 (missing XML comments) to keep entity definitions concise.
- Reference only core Microsoft Agent packages needed for domain abstractions; infrastructure dependencies belong elsewhere.
