# web-vendor-bootstrap-css Style Guide

- Treat vendor CSS as third-party; never edit files under `wwwroot/lib/bootstrap`. Instead, override styles in application CSS.
- When updating Bootstrap, replace the entire library folder from the same version to avoid mix-and-match builds.
- Exclude source maps from production publish if size becomes an issue; document any build tweaks in the README.
