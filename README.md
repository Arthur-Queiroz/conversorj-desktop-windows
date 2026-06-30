# ConversorJ Desktop Windows

Aplicativo desktop Windows para converter links do YouTube e X para MP3 ou MP4 usando `yt-dlp` e `ffmpeg`.

## Requisitos para desenvolvimento

- Windows
- .NET SDK 8 ou superior
- `yt-dlp` no PATH
- `ffmpeg` no PATH

## Rodar localmente

```powershell
dotnet run --project src\ConversorJ.App\ConversorJ.App.csproj
```

## Testes

```powershell
dotnet run --project tests\ConversorJ.Core.Tests\ConversorJ.Core.Tests.csproj
```

## Publicar executavel local

```powershell
dotnet publish src\ConversorJ.App\ConversorJ.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\publish\win-x64
```

O executavel publicado fica em:

```text
publish\win-x64\ConversorJ.App.exe
```
