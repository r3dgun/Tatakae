# Phase 16 ResultDto value-type test hotfix

`ResultDto<T>.Failed` intentionally assigns `default(T)` to `Data`.

- For reference types such as `string`, `default(T)` is `null`.
- For value types such as `int`, `default(T)` is `0`.

The ResultDto factory test now verifies both behaviors separately. Use `ResultDto<int?>` when a failed integer result must carry a nullable `Data` value.
