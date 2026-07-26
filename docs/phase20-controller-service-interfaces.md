# Phase 20 — Controller/Service Interface Boundary

All API controllers now depend only on contracts from `Tatakae.Application.Interfaces.Services`.
Concrete application service classes are no longer constructor dependencies of the API layer.

## Request flow

```text
HTTP Controller
  -> I...Service
  -> Application use case
  -> Repository/Gateway interface
  -> Infrastructure implementation
```

Application services return `ResultDto` / `ResultDto<T>`. Controllers map the semantic status through
`ResultDtoActionResultExtensions` and do not implement business exception handling.

## DI rule

`AddTatakaeApplication()` registers only interface-to-implementation mappings. Concrete use cases are
not exposed as service registrations.

## Architecture guards

`CleanArchitectureServiceBoundaryTests` fails when:

- a controller constructor receives a concrete `...Service` class;
- a service dependency is not declared in `Tatakae.Application.Interfaces.Services`;
- Application DI exposes a concrete use case directly.
