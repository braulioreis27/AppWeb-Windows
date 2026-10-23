# AppWeb

Aplicativo para Windows que transforma sites em aplicativos de desktop isolados, com janela própria, perfil separado, ícone personalizado e atalho no Menu Iniciar.

O AppWeb é um gerenciador nativo em WinUI 3. Ele usa o **WebView2** por padrão e também consegue abrir sites em navegadores instalados, como Edge, Chrome, Brave, Vivaldi e Firefox.

## Recursos

- Criação de Web Apps com nome, URL, descrição e ícone.
- Atalhos no Menu Iniciar com identidade própria no Windows.
- Perfil de navegador isolado por Web App.
- WebView2 integrado, sem barra de navegador quando não for necessária.
- Suporte a Edge, Chrome, Brave, Vivaldi e Firefox instalados.
- Ícone automático obtido do favicon do site.
- Ícone personalizado a partir de PNG, JPG, BMP, GIF, WebP ou ICO.
- Conversão automática de imagens para ICO com múltiplas resoluções.
- Modo de janela privada.
- Barra de ferramentas opcional no WebView2.
- Instância única por Web App no WebView2: abrir novamente traz a janela existente para frente.
- Exclusão do Web App, atalho, ícone e perfil local em uma única ação.

## Requisitos

- Windows 10 versão 1809 ou superior, ou Windows 11.
- Arquitetura x64.
- .NET SDK 8.0 para compilar.
- WebView2 Runtime para usar o modo WebView2. O Windows 11 normalmente já inclui o runtime.
- Pelo menos um navegador compatível instalado para usar navegadores externos.

O executável gerado é autocontido para Windows x64: não é necessário instalar o runtime do .NET na máquina de destino.

## Como usar

### Abrir o gerenciador

Depois de compilar, execute:

```powershell
.\src\AppWeb.Manager\bin\Debug\net8.0-windows10.0.19041.0\win-x64\AppWeb.Manager.exe
```

A janela principal mostra todos os Web Apps cadastrados e permite pesquisar por nome, endereço ou descrição.

### Criar um Web App

1. Clique em **Novo Web App**.
2. Informe o **Nome**.
3. Informe o **Endereço** do site.
4. Opcionalmente, adicione uma **Descrição**.
5. Escolha o ícone:
   - Deixe como está para buscar o favicon do site automaticamente.
   - Clique em **Escolher imagem...** para usar uma imagem própria.
   - Clique em **Usar favicon do site** para remover a imagem personalizada.
6. Escolha o **Navegador**.
7. Defina se a barra de ferramentas deve aparecer.
8. Defina se o Web App deve abrir em janela privada.
9. Clique em **Salvar**.

Depois de salvo, o Web App aparece na lista e um atalho é criado no Menu Iniciar.

Se o endereço for digitado sem esquema, o AppWeb assume `https://`. Por exemplo, `notion.so` vira `https://notion.so`. Apenas endereços HTTP e HTTPS são aceitos.

### Abrir, editar e excluir

- **Abrir**: inicia o Web App no navegador escolhido.
- **Editar**: altera nome, endereço, descrição, ícone, navegador e opções.
- **Excluir**: remove o atalho, o ícone e o perfil local do Web App.

Também é possível abrir pelo atalho criado no Menu Iniciar.

### Ícones

O AppWeb tenta obter o favicon automaticamente ao criar ou editar um Web App. A busca considera ícones declarados na página, imagens Open Graph e o arquivo `/favicon.ico`.

Imagens personalizadas são convertidas para `.ico` com tamanhos de 16, 24, 32, 48, 64, 128 e 256 pixels. Um ícone personalizado é preservado quando o endereço do site muda. Para voltar ao favicon automático, use **Usar favicon do site**.

### Perfis e janela privada

Cada Web App tem seu próprio diretório de perfil. Cookies, cache, logins e sessões de um Web App não são compartilhados com outro.

No modo WebView2, a janela privada usa um perfil temporário que é removido ao fechar a janela. No Chrome, Brave, Vivaldi e Edge, o AppWeb usa os modos de navegação privada correspondentes. No Firefox, usa uma janela privada com perfil dedicado.

### Barra de ferramentas

No WebView2, a barra de ferramentas do AppWeb inclui voltar, avançar, recarregar, início, endereço atual e abrir no navegador padrão.

Em navegadores Chromium externos:

- Com a barra desativada, o site abre em modo aplicativo (`--app`).
- Com a barra ativada, o site abre em uma janela normal do navegador.

O Firefox sempre abre em uma janela normal do próprio navegador.

## Como funciona

### Arquitetura

A solução é dividida em quatro projetos:

| Projeto | Responsabilidade |
| --- | --- |
| `AppWeb.Core` | Modelos, validação, interfaces e regras de negócio. |
| `AppWeb.Infrastructure` | Persistência JSON, descoberta de navegadores, atalhos, perfis, ícones e inicialização de processos. |
| `AppWeb.Manager` | Aplicativo WinUI 3. Atua como gerenciador e como host dos Web Apps. |
| `AppWeb.Tests` | Testes unitários e de integração. |

### Gerenciador e host no mesmo executável

O `AppWeb.Manager.exe` tem dois modos:

- Sem argumentos: abre o gerenciador.
- Com `--webapp <guid>`: abre um Web App específico como host.

O host é um processo separado do gerenciador. Para WebView2, o processo cria uma janela própria, configura um `CoreWebView2Environment` com o diretório de perfil do Web App e navega para a URL salva.

Os atalhos do Menu Iniciar apontam para o executável atual com o argumento `--webapp <guid>`. O Windows usa um AppUserModelID por Web App para agrupar a janela, o atalho e o ícone corretamente na barra de tarefas.

### Navegadores externos

O AppWeb procura navegadores no registro do Windows (`App Paths`) e em caminhos de instalação conhecidos.

Para navegadores Chromium, cada Web App recebe um `--user-data-dir` próprio. Para Firefox, é usado um perfil dedicado com `-profile`, `-no-remote` e `-new-instance`.

Se um Web App estiver configurado para um navegador externo, o host apenas inicia o navegador e encerra.

### Segurança de URLs

- Apenas HTTP e HTTPS são aceitos para o endereço do Web App.
- URLs com usuário/senha embutidos são rejeitadas.
- URLs UNC, com host inválido ou com caracteres de controle são rejeitadas.
- Links não HTTP/HTTPS abertos dentro do WebView2 são enviados ao Windows, quando aplicável, como `mailto:`.
- Novas janelas abertas pelo site são encaminhadas para o navegador padrão.
- No WebView2, preenchimento automático de formulários e salvamento automático de senhas ficam desativados.
- Ferramentas de desenvolvedor ficam desativadas no WebView2.

### Onde os dados ficam

| Dado | Local |
| --- | --- |
| Catálogo de Web Apps | `%LOCALAPPDATA%\AppWeb\catalog.json` |
| Perfis e sessões | `%LOCALAPPDATA%\AppWeb\Profiles\<guid>` |
| Perfil WebView2 | `%LOCALAPPDATA%\AppWeb\Profiles\<guid>\webview2` |
| Ícones | `%LOCALAPPDATA%\AppWeb\Icons\<guid>-<hash>.ico` |
| Atalhos | `%APPDATA%\Microsoft\Windows\Start Menu\Programs\AppWeb` |

O catálogo é um arquivo JSON local. Não há sincronização em nuvem.

## Desenvolvimento

### Compilar

```powershell
dotnet build AppWeb.sln -c Debug
```

Saída principal:

```text
src\AppWeb.Manager\bin\Debug\net8.0-windows10.0.19041.0\win-x64\AppWeb.Manager.exe
```

### Executar os testes

```powershell
dotnet test tests\AppWeb.Tests\AppWeb.Tests.csproj -c Debug
```

Os testes cobrem validação de URL e nome, repositório JSON, serviço de Web Apps, criação real de atalhos, geração de ícones e o fluxo de integração com ícone personalizado.

### Publicar

Para gerar uma pasta distribuível e autocontida:

```powershell
dotnet publish src\AppWeb.Manager\AppWeb.Manager.csproj -c Release -r win-x64 -o publish
```

Depois, execute `publish\AppWeb.Manager.exe` ou distribua a pasta `publish`.

### Estrutura do repositório

```text
AppWeb.sln
Directory.Build.props
src/
  AppWeb.Core/
  AppWeb.Infrastructure/
  AppWeb.Manager/
tests/
  AppWeb.Tests/
```

## Limitações e solução de problemas

### WebView2 não abre

Instale o **Microsoft Edge WebView2 Runtime** (Evergreen). O Windows 11 normalmente já o inclui.

### Nenhum navegador encontrado

Além do WebView2, o AppWeb só lista navegadores que consegue localizar no registro ou nos caminhos padrão. Instale o navegador ou verifique se ele está em uma instalação padrão.

### Atalhos deixam de funcionar depois de mover o projeto

Os atalhos apontam para o caminho do executável no momento em que foram criados. Se você mover ou renomear a pasta do AppWeb, recrie os atalhos editando e salvando cada Web App.

### O ícone demora para aparecer

O Windows pode manter cache de ícones. O ícone e o atalho são atualizados pelo AppWeb, mas pode ser necessário fechar e reabrir o Explorer ou o Menu Iniciar para o Windows atualizar a exibição.

### Login com pop-up ou OAuth

Novas janelas abertas pelo site são encaminhadas para o navegador padrão. Alguns fluxos de login que dependem de pop-ups dentro da mesma sessão do WebView2 podem não funcionar como esperado.

### Navegador externo abre várias janelas

A instância única é aplicada aos Web Apps WebView2. Navegadores externos podem abrir mais de uma janela para o mesmo Web App.

### Aplicativo não assinado

O executável não possui assinatura de código. O SmartScreen pode exibir um aviso ao executar uma versão baixada ou publicada.

## Créditos

O conceito é inspirado no projeto `linuxmint/webapp-manager`. Esta é uma implementação independente em C# e WinUI 3.
