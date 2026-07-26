# Windows build path fix

The archive deliberately contains a very short root folder named `T`.

## Required extraction location

Extract or move that folder to a short path, preferably:

```text
H:\T
```

Do not extract it into another folder with the long archive name. A path such as
`H:\mmd\MMD\TatakaeEmbroidery...\TatakaeEmbroidery...` causes generated
WebCIL/TestHost paths to exceed Windows path limits.

## Clean build

Close Visual Studio, open PowerShell, and run:

```powershell
cd H:\T
.\scripts\rebuild-windows.ps1
```

The project centralizes generated outputs in `.b` and `.o` to keep paths short.

## Optional Windows long-path policy

When the machine policy still blocks long paths, run PowerShell as Administrator:

```powershell
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name LongPathsEnabled -Type DWord -Value 1
```

Restart Windows after changing the policy.
