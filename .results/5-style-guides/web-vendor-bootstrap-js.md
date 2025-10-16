# web-vendor-bootstrap-js Style Guide

- Do not modify vendor JS; updates should be applied by replacing files with the official Bootstrap distribution.
- Load only the bundles required by the app (typically `bootstrap.bundle.min.js`); remove unused files during publish steps if size is a concern.
- Re-run UI smoke tests after upgrading to catch breaking changes in interactive components.
