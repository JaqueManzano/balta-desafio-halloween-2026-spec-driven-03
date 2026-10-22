# Guia de Validação: Encurtador de Links

## Pré-requisitos

- SDK .NET 9 instalado.
- Solução/projetos da API e testes implementados conforme o plano.
- SQLite disponível por meio do provedor EF Core; a aplicação deve apontar para um arquivo local de teste/desenvolvimento.

## Inicialização

Na raiz da solução, restaurar e compilar:

```powershell
dotnet restore
dotnet build
```

Aplicar as migrações no arquivo SQLite configurado e iniciar a API:

```powershell
dotnet ef database update --project src/Shortener.Api
dotnet run --project src/Shortener.Api
```

Usar a URL local informada pela aplicação como `BASE_URL`. Os nomes e caminhos de solução/projeto podem ser ajustados quando a estrutura executável for criada.

## Cenários HTTP de ponta a ponta

Os exemplos abaixo são sequenciais; substitua `BASE_URL`, `TOKEN` e valores JSON pelos retornados/configurados pela aplicação.

1. **Cadastro e entrada**
   - Enviar cadastro com e-mail válido e senha de pelo menos 8 caracteres ao endpoint de conta.
   - Entrar com as mesmas credenciais e guardar o token Bearer retornado.
   - Resultado esperado: conta criada e token obtido; credenciais incorretas são recusadas com mensagem que não revela qual campo falhou.
2. **Criar link com autenticação**
   - Enviar `POST /api/links` com `Authorization: Bearer TOKEN`, `targetUrl` HTTPS válida e sem `code`/`expiresAtUtc`.
   - Resultado esperado: `shortUrl` e código de 7 caracteres; a expiração é 30 dias após a criação.
3. **Redirecionar publicamente**
   - Abrir `GET /{code}` sem token.
   - Resultado esperado: HTTP 302 com `Location` igual à URL de destino; a contagem aumenta uma vez.
4. **Listar e consultar**
   - Enviar `GET /api/links` e `GET /api/links/{code}/stats` com token da pessoa criadora.
   - Resultado esperado: aparecem os dados e a contagem do link próprio.
   - Repetir com token de outra conta; o link não pode revelar destino nem contagem.
5. **Validação, conflito e expiração**
   - Tentar URL com esquema não HTTP(S), senha inferior a 8 caracteres, código inválido/duplicado e expiração igual ou anterior ao instante atual.
   - Resultado esperado: HTTP 400 com ProblemDetails em português para entradas inválidas; conflito de código próprio indisponível; nenhum link inválido persistido.
   - Para link inexistente, esperar 404; para link expirado, esperar 410 e nenhuma alteração no contador.
6. **Concorrência e persistência**
   - Fazer acessos concorrentes ao mesmo link ativo e verificar que a contagem final equivale ao número de redirecionamentos bem-sucedidos.
   - Reiniciar a aplicação e verificar que contas, links e contagens continuam disponíveis.

## Testes automatizados

Executar todos os testes:

```powershell
dotnet test
```

Os testes unitários devem validar regras do serviço; os testes de integração devem exercitar os endpoints e o provedor SQLite real, incluindo unicidade e incremento concorrente. Consulte [data-model.md](./data-model.md) para restrições persistidas e [contracts/openapi.yaml](./contracts/openapi.yaml) para formatos HTTP.

Para sair, descartar access e refresh tokens no cliente. Essa operação não revoga no servidor access tokens já emitidos; eles permanecem válidos até vencerem.

Antes de sair, validar `POST /api/accounts/refresh` com o refresh token e confirmar que a resposta contém novas credenciais. O endpoint de saída e o de renovação também devem ter testes de integração do caminho feliz.
