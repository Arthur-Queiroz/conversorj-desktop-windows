# ConversorJ Desktop Windows

Aplicativo desktop Windows para converter links do YouTube e X para MP3 ou MP4 usando `yt-dlp` e `ffmpeg`, e para transcrever audio em TXT usando `whisper.cpp`.

## Modelos de transcricao

O app usa `whisper.cpp` via `whisper-cli.exe`. Ao escolher o formato **TXT - Transcricao**, a UI mostra um seletor de **modelo de transcricao** com niveis genericos. Cada nivel corresponde a um arquivo `ggml` do whisper.cpp que deve estar em `bin\models`:

| Nivel na UI                  | Arquivo em `bin\models`            |
| ---------------------------- | ---------------------------------- |
| Muito rapida                 | `ggml-tiny-q5_1.bin`               |
| Transcricao rapida           | `ggml-base-q5_1.bin`               |
| Equilibrada                  | `ggml-small-q5_1.bin`              |
| Melhor transcricao possivel  | `ggml-large-v3-turbo-q5_0.bin`     |

O seletor lista **apenas os modelos cujo arquivo esta presente** em `bin\models`, e por padrao seleciona o melhor disponivel. Instale ao menos um modelo para conseguir transcrever; modelos maiores melhoram a qualidade, mas aumentam o download, o uso de memoria e o tempo de processamento.

Baixe os arquivos do repositorio [`ggerganov/whisper.cpp`](https://huggingface.co/ggerganov/whisper.cpp) no Hugging Face (os nomes precisam bater exatamente com a tabela acima) e copie-os para `bin\models`. Exemplo:

```text
bin\models\ggml-tiny-q5_1.bin
bin\models\ggml-base-q5_1.bin
bin\models\ggml-small-q5_1.bin
bin\models\ggml-large-v3-turbo-q5_0.bin
```

## Requisitos para desenvolvimento

- Windows
- .NET SDK 8 ou superior
- `yt-dlp` no PATH
- `ffmpeg` no PATH
- `whisper-cli` no PATH, ou `whisper-cli.exe` em `bin`
- Ao menos um modelo `ggml` em `bin\models` (ver tabela em "Modelos de transcricao")

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
