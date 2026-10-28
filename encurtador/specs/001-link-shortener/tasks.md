---

description: "Tarefas de implementação do encurtador de links"
---

# Tasks: Encurtador de Links

**Input**: Documentos de design em `specs/001-link-shortener/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md` e `contracts/openapi.yaml`

**Tests**: Incluídos porque a constituição e o plano exigem testes unitários das regras de negócio e testes de integração do caminho feliz de cada endpoint.

**Organization**: Tarefas agrupadas por história de usuário para permitir implementação e validação incremental.

## Formato

- **[P]**: Tarefa pode ser executada em paralelo com outras tarefas paralelas da mesma fase; altera arquivos distintos e não depende de trabalho incompleto.
- **[US#]**: História de usuário atendida pela tarefa.
- Os caminhos de código são relativos à pasta `encurtador/`; a solução `Shortener.sln` fica na raiz do repositório, com os testes em `encurtador/tests/`.

## Phase 1: Setup

**Purpose**: Criar a solução .NET 9 e os projetos da API e dos testes conforme `plan.md`.

- [X] T001 Criar obrigatoriamente o arquivo de solução `Shortener.sln` na raiz do repositório e adicionar nele os projetos `encurtador/src/Shortener.Api/Shortener.Api.csproj`, `encurtador/tests/Shortener.UnitTests/Shortener.UnitTests.csproj` e `encurtador/tests/Shortener.IntegrationTests/Shortener.IntegrationTests.csproj`; definir `net9.0`, referências entre projetos, EF Core SQLite e Identity EF Core na API, e xUnit, `Microsoft.NET.Test.Sdk` e `Microsoft.AspNetCore.Mvc.Testing` nos projetos de teste.
- [X] T002 Criar o arquivo `.gitignore` na raiz do repositório para ignorar artefatos gerados pelo .NET, arquivos de build (`bin/`, `obj/`), bancos SQLite locais e arquivos de configuração local que não devem ser versionados.
- [X] T003 Adicionar `Swashbuckle.AspNetCore` em `src/Shortener.Api/Shortener.Api.csproj` e configurar a geração do documento OpenAPI e a interface Swagger UI em `src/Shortener.Api/Program.cs`, habilitando a interface somente no ambiente de desenvolvimento como ferramenta de documentação da API, não como interface de produto.

---

## Phase 2: Foundational

**Purpose**: Preparar persistência compartilhada, tratamento uniforme de validação e infraestrutura de testes antes das histórias.

- [X] T004 Configurar `src/Shortener.Api/Data/AppDbContext.cs`, `src/Shortener.Api/Program.cs` e `src/Shortener.Api/appsettings.json` para EF Core com SQLite persistente e Identity EF Core.
- [X] T005 Implementar respostas HTTP 400 `ProblemDetails` com mensagens em português em `src/Shortener.Api/Extensions/ValidationProblemExtensions.cs` e conectar a configuração em `src/Shortener.Api/Program.cs`.
- [X] T006 Criar o host de integração reutilizável com banco SQLite isolado por teste em `tests/Shortener.IntegrationTests/Infrastructure/ApiFactory.cs`.

**Checkpoint**: A solução compila, a API inicializa com armazenamento SQLite persistente e os testes de integração conseguem inicializar a aplicação com banco isolado.

---

## Phase 3: User Story 4 - Acessar a conta (Priority: P1)

**Goal**: Permitir cadastro, login, renovação de tokens e logout, sem revelar qual credencial falhou.

**Independent Test**: Cadastrar uma conta, entrar, renovar tokens e sair; verificar que credenciais inválidas são recusadas sem identificar se o e-mail ou a senha falhou e que senha com menos de 8 caracteres não cria conta.

### Tests

- [X] T007 [P] [US4] Criar testes unitários das regras de cadastro, política de senha mínima de 8 caracteres e autenticação com resposta genérica em `tests/Shortener.UnitTests/Application/AccountServiceTests.cs`.
- [X] T008 [P] [US4] Criar testes de integração HTTP dos caminhos felizes de cadastro, login, renovação e logout, além das respostas para credenciais inválidas e senha curta, em `tests/Shortener.IntegrationTests/AccountEndpointsTests.cs`.

### Implementation

- [X] T009 [US4] Configurar Identity para usar e-mail como identificador, exigir senha com no mínimo 8 caracteres sem requisitos adicionais de composição, e emitir/renovar tokens pessoais em `src/Shortener.Api/Program.cs` e `src/Shortener.Api/Application/AccountService.cs`.
- [X] T010 [US4] Implementar cadastro, login, renovação e logout com respostas do contrato e mensagens genéricas para credenciais incorretas em `src/Shortener.Api/Endpoints/AccountEndpoints.cs`.
- [X] T011 [US4] Gerar a migração inicial das tabelas de Identity para SQLite em `src/Shortener.Api/Data/Migrations/`.

**Checkpoint**: Os quatro endpoints de conta passam seus testes de integração e a persistência das contas funciona após reiniciar a aplicação.

---

## Phase 4: User Story 1 - Criar e compartilhar um link curto (Priority: P1)

**Goal**: Permitir que uma pessoa autenticada crie links com código gerado ou personalizado, com destino válido e código único.

**Independent Test**: Autenticar uma pessoa, criar um link gerado de 7 caracteres e outro com código próprio válido, e confirmar que cada resposta contém o link curto; entradas inválidas ou códigos personalizados duplicados não criam links.

### Tests

- [X] T012 [P] [US1] Criar testes unitários para validação de URL/código, geração aleatória, normalização, colisão e expiração padrão de criação em `tests/Shortener.UnitTests/Application/LinkCreationTests.cs`.
- [X] T013 [P] [US1] Criar testes de integração HTTP para criação autenticada, formatos da resposta, validações e conflito de código personalizado em `tests/Shortener.IntegrationTests/LinkCreationTests.cs`.

### Implementation

- [X] T014 [US1] Criar a entidade e o mapeamento persistente de Link em `src/Shortener.Api/Data/Entities/Link.cs` e `src/Shortener.Api/Data/AppDbContext.cs`, incluindo a migração em `src/Shortener.Api/Data/Migrations/`; aplicar as restrições do modelo: `4–20 caracteres para código próprio; 7 caracteres se gerado`, `Alfanumérico; armazenado em forma canônica maiúscula; índice único`, `URL absoluta válida com esquema HTTP ou HTTPS`, `Futuro no momento de criação` e `o padrão CreatedAtUtc + 30 dias`.
- [X] T015 [US1] Implementar persistência de links e tratamento seletivo de colisões de unicidade no SQLite em `src/Shortener.Api/Data/Repositories/LinkRepository.cs`.
- [X] T016 [US1] Implementar a regra de criação no serviço, incluindo código criptograficamente aleatório de 7 caracteres, repetição somente para colisões geradas, conflito para código próprio indisponível e expiração padrão de 30 dias, em `src/Shortener.Api/Application/LinkService.cs`.
- [X] T017 [US1] Implementar `POST /api/links` autenticado, validar a entrada e devolver os campos definidos em `CreatedLink` em `src/Shortener.Api/Endpoints/LinkEndpoints.cs`.

**Checkpoint**: Uma pessoa autenticada pode criar links válidos e os códigos são únicos no banco; entradas inválidas não produzem respostas de sucesso nem persistem recursos.

---

## Phase 5: User Story 2 - Acessar um link compartilhado (Priority: P1)

**Goal**: Redirecionar visitantes sem autenticação para destinos de links ativos; códigos inexistentes e expirados não redirecionam.

**Independent Test**: Abrir um código ativo sem token e observar HTTP 302 com `Location` igual ao destino; verificar 404 para código inexistente, 410 para expirado e nenhuma contagem para acessos sem redirecionamento.

### Tests

- [X] T018 [P] [US2] Criar testes unitários para redirecionamento, fronteira exclusiva de expiração e incremento somente quando ativo em `tests/Shortener.UnitTests/Application/LinkRedirectTests.cs`.
- [X] T019 [P] [US2] Criar testes de integração do endpoint público para HTTP 302/404/410, visitas repetidas e contagem concorrente em SQLite em `tests/Shortener.IntegrationTests/RedirectEndpointsTests.cs`.

### Implementation

- [X] T020 [US2] Implementar no repositório o incremento atômico condicionado a link ativo e a obtenção do destino na mesma operação SQLite com `UPDATE ... RETURNING` em `src/Shortener.Api/Data/Repositories/LinkRepository.cs`.
- [X] T021 [US2] Implementar no `LinkService` o resultado de redirecionamento ativo, inexistente ou expirado sem contabilizar os dois últimos casos em `src/Shortener.Api/Application/LinkService.cs`.
- [X] T022 [US2] Implementar `GET /{code}` sem autenticação, retornando redirecionamento HTTP 302, 404 ou 410 conforme o resultado do serviço em `src/Shortener.Api/Endpoints/RedirectEndpoints.cs`.

**Checkpoint**: Visitantes sem autenticação são redirecionados somente para links ativos e cada redirecionamento bem-sucedido incrementa exatamente uma vez.

---

## Phase 6: User Story 3 - Consultar acessos e gerenciar expiração (Priority: P2)

**Goal**: Permitir que cada pessoa consulte somente seus próprios links e contagens, e informe uma expiração futura ao criar um link.

**Independent Test**: Criar um link com expiração futura, acessá-lo e consultar lista/estatísticas como criador; verificar expiração no instante definido, recusa de data passada ou atual e negação de dados para outra conta.

### Tests

- [X] T023 [P] [US3] Criar testes unitários para expiração informada, expiração igual/passada, filtragem por proprietário e consulta de estatísticas em `tests/Shortener.UnitTests/Application/LinkManagementTests.cs`.
- [X] T024 [P] [US3] Criar testes de integração HTTP para listagem, consulta de estatísticas, isolamento entre contas e criação com expiração futura ou inválida em `tests/Shortener.IntegrationTests/LinkManagementEndpointsTests.cs`.

### Implementation

- [X] T025 [US3] Implementar consultas de listagem e estatísticas filtradas por `OwnerId`, sem retornar dados de links de outra conta, em `src/Shortener.Api/Data/Repositories/LinkRepository.cs`.
- [X] T026 [US3] Estender `LinkService` para validar `ExpiresAtUtc` futura em UTC na criação e disponibilizar lista/estatísticas somente dos links do proprietário autenticado em `src/Shortener.Api/Application/LinkService.cs`.
- [X] T027 [US3] Implementar `GET /api/links` e `GET /api/links/{code}/stats`, além de aceitar `expiresAtUtc` em `POST /api/links`, em `src/Shortener.Api/Endpoints/LinkEndpoints.cs`.

**Checkpoint**: A pessoa criadora vê os próprios destinos e contagens; outra conta não consegue consultar esses dados, e a expiração é aplicada a partir do instante definido.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Alinhar os documentos com a implementação e validar os fluxos completos.

- [X] T028 [P] Atualizar os exemplos de inicialização, migração, autenticação e cenários HTTP para os caminhos e contratos implementados em `specs/001-link-shortener/quickstart.md`.
- [X] T029 [P] Conferir e alinhar os esquemas, códigos de resposta e descrições de todos os endpoints implementados em `specs/001-link-shortener/contracts/openapi.yaml`.
- [X] T030 Executar `dotnet restore`, `dotnet build` e `dotnet test` para `Shortener.sln`; em `tests/Shortener.IntegrationTests/PersistenceTests.cs`, automatizar um teste que crie conta e link com expiração definida, registre acessos, encerre o host da API e o reinicie usando o mesmo arquivo SQLite, então confirme que a conta, o link, a expiração e a contagem persistiram; validar também concorrência e contratos descritos em `specs/001-link-shortener/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências.
- **Foundational (Phase 2)**: Depende de Setup e bloqueia as histórias.
- **US4 (Phase 3)**: Depende da fundação; habilita autenticação necessária ao gerenciamento.
- **US1 (Phase 4)**: Depende de US4 para identificar o proprietário e exige a persistência configurada.
- **US2 (Phase 5)**: Depende de US1 para que existam links persistidos a redirecionar.
- **US3 (Phase 6)**: Depende de US1 para criar links e US2 para validar contagem e expiração em redirecionamentos.
- **Polish (Phase 7)**: Depende das histórias selecionadas para entrega.

### User Story Dependencies

```text
US4 (P1: conta/autenticação)
  └── US1 (P1: criação de links)
        └── US2 (P1: acesso/redirecionamento)
              └── US3 (P2: estatísticas e expiração configurável)
```

US4 aparece primeiro entre as histórias P1, embora esteja listada depois no `spec.md`, porque US1 exige uma pessoa autenticada; US2 e US3 também dependem dos links criados anteriormente.

### Parallel Opportunities

- As tarefas de teste unitário e de integração de cada história podem ser desenvolvidas em paralelo, pois usam arquivos distintos.
- Em cada história, concluir ambos os conjuntos de testes antes da implementação facilita validar o comportamento esperado.
- As histórias não são independentes nesta especificação: a sequência de dependências acima prevalece sobre paralelismo entre histórias.
- As tarefas de documentação T026 e T027 podem ser executadas em paralelo depois das histórias correspondentes.

## Parallel Examples

### User Story 4

```text
T007 tests/Shortener.UnitTests/Application/AccountServiceTests.cs  ||  T008 tests/Shortener.IntegrationTests/AccountEndpointsTests.cs
```

### User Story 1

```text
T012 tests/Shortener.UnitTests/Application/LinkCreationTests.cs  ||  T013 tests/Shortener.IntegrationTests/LinkCreationTests.cs
```

### User Story 2

```text
T018 tests/Shortener.UnitTests/Application/LinkRedirectTests.cs  ||  T019 tests/Shortener.IntegrationTests/RedirectEndpointsTests.cs
```

### User Story 3

```text
T023 tests/Shortener.UnitTests/Application/LinkManagementTests.cs  ||  T024 tests/Shortener.IntegrationTests/LinkManagementEndpointsTests.cs
```

## Implementation Strategy

### MVP First

1. Completar Setup e Foundational.
2. Completar US4 para habilitar cadastro e autenticação.
3. Completar US1 como incremento central de criação (MVP da funcionalidade de criação).
4. Completar US2 para validar que o link compartilhado realmente abre e redireciona; a entrega mínima ponta a ponta de criação e compartilhamento inclui US4, US1 e US2.
5. Validar cada história pelos critérios independentes acima antes de avançar.

### Incremental Delivery

1. Setup + Foundational: solução executável, SQLite persistente e host de testes.
2. US4: cadastro, autenticação e ciclo de tokens.
3. US1: criação validada e persistida de links.
4. US2: redirecionamento público e incremento atômico de acessos.
5. US3: listagem, estatísticas privadas e expiração configurável.
6. Polish: documentação alinhada e suíte completa validada.
