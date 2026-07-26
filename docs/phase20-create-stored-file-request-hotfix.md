# CreateStoredFileRequest hotfix

`CreateStoredFileRequest` is an application contract and now lives in `Tatakae.Application.Contracts.Files`.

It was previously declared beside `IMediaAssetRepository` in `Tatakae.Application.Interfaces`, which made API consumers depend on a repository namespace and caused `FilesController` compilation to fail.
