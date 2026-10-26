# Monitor Hub

Hub de configurações de monitores para Windows: ativar/desativar monitores, monitor principal,
posição (arrastar com ajuste fino), resolução, taxa de atualização, escala do Windows,
perfis salvos, calibração de cores e calibração de geometria para CRT. Interface em 5 idiomas.

## Download
Baixe em **Releases**:
- `MonitorHub-Setup-x.y.z.exe` — instalador (recomendado). Instala só para o seu usuário, sem pedir administrador.
- `MonitorHub.exe` — versão portátil.

## É seguro?
- **Código aberto:** todo o código está neste repositório (C# / WinForms, .NET Framework 4.8).
- **Compilado pelo GitHub, não à mão:** os arquivos das Releases são gerados pelo workflow
  [`release.yml`](.github/workflows/release.yml) direto deste código.
- **Prova de origem:** cada Release tem uma *attestation* assinada pelo GitHub. Para verificar:
  ```
  gh attestation verify MonitorHub-Setup-x.y.z.exe --repo Budart/MonitorHub
  ```
- **Hashes:** `SHA256SUMS.txt` em cada Release. Confira com `Get-FileHash arquivo.exe` no PowerShell.
- **Sem rede, sem coleta:** o app não acessa a internet. Ele só usa APIs do Windows de vídeo e grava
  suas preferências em `%AppData%\MonitorHub`.

## O que o app altera no sistema
- Configurações de vídeo (pelas APIs oficiais `SetDisplayConfig` / `ChangeDisplaySettingsEx`).
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\MonitorHub` — só se "Iniciar com o Windows" estiver ligado.

## Compilar
Requer .NET SDK (qualquer versão recente) no Windows:
```
dotnet build MonitorHub.csproj -c Release
```
Instalador: [NSIS](https://nsis.sourceforge.io) → `makensis installer/MonitorHub.nsi`
(copie o `MonitorHub.exe` compilado para a raiz antes).

## Licença
MIT
