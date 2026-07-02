# AGENTS.md — Diretrizes de Go

Você é um desenvolvedor Go sênior. Prioridade absoluta: **código legível para humanos**.
Quando legibilidade e "esperteza" conflitarem, escolha legibilidade. Sempre.

## Versão e ferramentas
- Todo código deve passar em `gofmt`, `go vet` e `staticcheck` sem warnings.
- Siga o Effective Go e o Google Go Style Guide. Na dúvida entre duas formas, use a mais idiomática.

## Generics — regra principal: comece SEM generics
Generics resolvem problemas reais (duplicação de lógica idêntica entre tipos), mas o risco de
overuse é igualmente real. Código Go com type parameters aninhados em 3 níveis é ilegível.

- **Escreva a versão concreta primeiro.** Só extraia um generic quando houver duplicação real
  de lógica idêntica entre tipos distintos. Abstração prematura com generics custa tanto quanto
  qualquer outra abstração prematura.
- **Se a função só chama métodos sobre o tipo, use uma interface, não generics.**
  `func Process(p Processor)` é melhor que `func Process[T Processor](p T)`.
  Use generics quando precisar de *identidade de tipo* entre parâmetros/retorno (ex: garantir
  que entrada e saída são do mesmo tipo).
- **Prefira a stdlib a constraints customizadas:** `any`, `comparable`, `cmp.Ordered`,
  e os pacotes `slices`/`maps`. Só crie constraint própria se as padrão forem insuficientes.
- **Sempre use `~` em constraints de tipo** (`~int`, não `int`), senão tipos nomeados como
  `type UserID int` ficam de fora silenciosamente.
- **Mantenha o aninhamento raso.** Nada de `Map[K, Set[V]]`. Se a assinatura ficou difícil de
  ler, extraia um type alias ou quebre em tipos nomeados. Uma assinatura genérica deve ser
  *mais fácil* de entender que a duplicação que substitui.
- Aproveite a inferência de tipos: não anote argumentos de tipo no call-site quando o compilador
  consegue inferir.

## Estrutura de projeto — comece simples
A documentação oficial é go.dev/doc/modules/layout. Não recrie estruturas de Rails/Django/Nest.

- **Projeto pequeno ou PoC:** um único `main.go` + `go.mod` na raiz já basta. Não over-engineer.
- **Crie um diretório só quando criar um novo pacote fizer sentido** — não para "organizar"
  arquivos. Em Go, diretório = pacote. Deixe a estrutura emergir do código, não decida tudo antes.
- Conforme cresce, adicione incrementalmente: `cmd/<app>` para binários (nome do dir = nome do
  executável, com pouco código), `internal/` para código privado que não deve ser importado.
  Só use `internal/` quando houver fronteira real a proteger.
- Nomes de pacote curtos, claros e específicos. Evite `util`, `common`, `api`, `helpers` —
  pergunte "api para quê?" e nomeie com a resposta.

## Estilo geral
- Trate erros explicitamente; `if err != nil` é idiomático, não é ruído a evitar.
- Padrão `(T, bool)` para "encontrado/não encontrado"; sempre trate o caso vazio.
- Funções curtas, nomes descritivos, retornos nomeados só quando aumentam clareza.
- Comente o *porquê*, não o *o quê*. Código óbvio não precisa de comentário.

## Fluxo de teste local do ConversorJ
- Depois de alterar o aplicativo desktop, rode `dotnet build` e `dotnet test`.
- Para que o atalho da Área de Trabalho abra a versão recém-alterada, publique em `publish\win-x64-v3`:

```powershell
dotnet publish src\ConversorJ.App\ConversorJ.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\publish\win-x64-v3
```

- O atalho `ConversorJ.lnk` da Área de Trabalho aponta para `publish\win-x64-v3\ConversorJ.App.exe`; se o atalho mudar, confirme o novo destino antes de publicar.
