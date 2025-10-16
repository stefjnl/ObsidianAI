# web-dockerfile Style Guide

- Base images should match the API project to keep runtime compatibility; update tags in lockstep with SDK upgrades.
- Publish the Blazor Server app using `dotnet publish` in Release mode and copy the published output only to keep images slim.
- Expose the same ports configured in Aspire to avoid orchestration drift.
