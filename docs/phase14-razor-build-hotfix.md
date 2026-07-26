# Phase 14 Razor build hotfix

This hotfix resolves two Razor/C# generated-code name collisions:

1. `Pages/Account/Artworks.razor`
   - The injected property was named `Artworks`, which matched the generated component type `Artworks`.
   - It is now named `ArtworkClient`.

2. `Pages/Admin/Seo.razor`
   - A Razor loop variable named `page` was used in attribute expressions such as `@page.Url`.
   - Razor parsed `@page` as the page directive, producing RZ9979 and page-directive diagnostics.
   - The variable is now named `auditPage`.

After replacing the project, remove all `bin` and `obj` directories before rebuilding so stale generated Razor files are not reused.
