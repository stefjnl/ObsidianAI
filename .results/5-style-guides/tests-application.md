# tests-application Style Guide

- Mirror application use-case naming when creating test classes (`<UseCase>NameTests`) to keep navigation simple.
- Favor Arrange-Act-Assert structure with explicit data builders from `TestEntityFactory` to keep scenarios readable.
- Cover both happy path and edge cases (null conversations, missing messages, cancellation) to guard against regressions.
