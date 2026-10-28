# Plano de Implementação: Encurtador de Links

**Branch**: `001-link-shortener` | **Data**: 2026-10-22 | **Spec**: [spec.md](./spec.md)

## Resumo

Implementar uma API HTTP para cadastro e autenticação por e-mail e senha, criação e listagem de links curtos próprios, redirecionamento público e consulta de acessos. A API será uma Minimal API em .NET 9; serviços de aplicação concentram as regras de negócio, e Entity Framework Core com SQLite persiste as contas e os links. O redirecionamento incrementa o contador em uma operação atômica no banco.

## Contexto Técnico

**Linguagem/Versão**: C# / .NET 9, conforme a constituição  
**Dependências principais**: ASP.NET Core Minimal APIs e Identity; Entity Framework Core SQLite; xUnit; `Swashbuckle.AspNetCore`  
**Armazenamento**: SQLite persistido em arquivo  
**Testes**: xUnit; testes unitários das regras e testes de integração HTTP com SQLite  
**Plataforma alvo**: Serviço HTTP compatível com .NET 9  
**Tipo de projeto**: API web, sem interface de usuário de produto; Swagger UI é documentação interativa para desenvolvimento  
**Metas de desempenho**: Não especificadas na funcionalidade; validar correção do incremento concorrente, sem assumir meta de latência ou throughput.  
**Restrições**: Endpoints não acessam persistência diretamente; regras de negócio ficam em serviços; erros de validação usam HTTP 400 e ProblemDetails em português; alterações de esquema devem preservar os dados. O cliente envia tokens Bearer por conta; não se usa chave compartilhada de gerenciamento.  
**Escala/escopo**: Primeira versão com contas, links e contagem total; sem analytics por visita, edição/exclusão de links ou política de retenção/limpeza. Não há volume de carga especificado.

## Verificação da Constituição

| Princípio | Estado | Evidência no plano |
|-----------|--------|--------------------|
| Limite do Serviço de Aplicação | PASS | Endpoints coordenam HTTP e delegam persistência e operações a serviços. |
| Isolamento das Regras de Negócio | PASS | `LinkService` concentra validação e decisões de criação, expiração, propriedade e redirecionamento. |
| Testes Unitários das Regras de Negócio | PASS | Cada regra de negócio terá pelo menos um teste xUnit no serviço/componente que a implementa. |
| Testes de Integração dos Endpoints | PASS | Cada endpoint terá teste de caminho feliz que verifica o contrato HTTP externamente. |
| Contrato de Erros de Validação | PASS | Validações retornam HTTP 400 com ProblemDetails localizado em português. |
| .NET 9, C# e Minimal APIs | PASS | Plataforma e modelo HTTP definidos pelo plano. |
| SQLite via Entity Framework Core | PASS | Persistência definida e usada pelos repositórios. |
| xUnit | PASS | Framework de testes definido. |
| Dependências novas justificadas | PASS | EF Core SQLite e Identity fornecem persistência relacional e armazenamento/verificação seguros de senhas; `Swashbuckle.AspNetCore` gera o documento OpenAPI e a Swagger UI de desenvolvimento para inspecionar e validar manualmente os contratos HTTP. As dependências atendem aos requisitos e seus pacotes/versões serão definidos na implementação .NET 9. |

**Gate pré-pesquisa**: PASS. Não há conflito com a constituição.

## Estrutura do Projeto

### Documentação desta funcionalidade

```text
specs/001-link-shortener/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── openapi.yaml
```

### Código-fonte

```text
src/
├── Shortener.Api/
│   ├── Endpoints/
│   │   ├── AccountEndpoints.cs
│   │   ├── LinkEndpoints.cs
│   │   └── RedirectEndpoints.cs
│   ├── Application/
│   │   └── LinkService.cs
│   └── Data/
│       ├── AppDbContext.cs
│       └── Repositories/
│           └── LinkRepository.cs
tests/
├── Shortener.UnitTests/
└── Shortener.IntegrationTests/
```

**Decisão de estrutura**: A solução `Shortener.sln` fica na raiz do repositório. O código de produção da API está em `src/Shortener.Api`; os projetos de teste ficam em `tests/`, separados do código de produção e referenciando a API. Endpoints são agrupados por recurso, com camada de aplicação e acesso a dados separados. Identity gerencia a entidade e as credenciais das contas; o agregado Link usa repositório.

## Decisões de Arquitetura

- A autenticação será por conta e token Bearer do ASP.NET Core Identity; serão mapeados somente handlers próprios de cadastro, entrada, renovação e saída, sem mapear o conjunto inteiro de endpoints Identity. O identificador autenticado será usado para filtrar operações de gerenciamento por proprietário. A chave de gerenciamento sugerida no rascunho foi descartada por não atender à autorização por proprietário exigida pela especificação.
- Identity fornece armazenamento e verificação de senhas; a política de senha será configurada para exigir no mínimo 8 caracteres sem impor requisitos de composição adicionais não especificados. Serão expostos os fluxos de cadastro, entrada, renovação de tokens e saída; verificação de e-mail e recuperação de senha permanecem fora do escopo.
- `Swashbuckle.AspNetCore` será usado para gerar o documento OpenAPI e disponibilizar Swagger UI somente em desenvolvimento, facilitando inspeção manual e validação dos contratos HTTP. Isso não constitui uma interface de usuário do produto.
- Um código canônico em maiúsculas preserva a unicidade sem diferenciar maiúsculas/minúsculas. Índice único no SQLite é a garantia final; checagem antecipada serve apenas para feedback. Colisões de código gerado tentam novamente; colisões de código escolhido retornam conflito.
- Código gerado terá 7 caracteres alfanuméricos usando alfabeto que evita caracteres visualmente ambíguos. A geração usará aleatoriedade criptograficamente segura.
- Link sem expiração solicitada recebe `CreatedAtUtc + 30 dias`. Datas persistidas e comparações usam UTC; o instante final é exclusivo.
- A contagem será atualizada atomicamente no SQLite apenas se o link ainda estiver ativo. A transação de redirecionamento usa `UPDATE ... RETURNING` para obter o destino do mesmo comando que incrementa; link inexistente ou expirado não retorna destino e não soma acesso.
- A listagem e as estatísticas só retornam links cujo proprietário corresponde ao usuário autenticado. Nenhuma resposta expõe hash de senha ou dado interno de Identity.
- Para manter as dependências reduzidas, não serão armazenados eventos individuais de acesso; o produto exige apenas a contagem total.

## Gate pós-design

**Constituição**: PASS. O desenho mantém serviços entre endpoints e persistência, isola regras, prevê testes unitários e de integração para os fluxos, usa SQLite/EF Core/.NET 9/xUnit e justifica as dependências. Nenhuma violação requer exceção.

O logout com token Bearer encerra a sessão no cliente ao descartar access e refresh tokens; não revoga access tokens já emitidos no servidor antes do vencimento. Revogação antecipada não é requisito definido para a primeira versão.

**Riscos acompanhados**: escrita concorrente no SQLite pode limitar throughput; não há requisito de escala para dimensioná-la. A tabela de links cresce sem limpeza nesta versão, conforme o escopo.
