<!--
Relatório de impacto da atualização
- Versão: 1.0.0 → 1.0.1 (ajuste PATCH: tradução sem alteração de significado)
- Princípios traduzidos:
  Application Service Boundary → Limite do Serviço de Aplicação;
  Business Rule Isolation → Isolamento das Regras de Negócio;
  Business Rule Unit Tests → Testes Unitários das Regras de Negócio;
  Endpoint Integration Tests → Testes de Integração dos Endpoints;
  Validation Error Contract → Contrato de Erros de Validação
- Seções traduzidas: Core Principles; Technology and Conventions;
  Planning and Compliance; Governance
- Seções adicionadas/removidas: nenhuma
- Pendências: nenhuma
-->
# Constituição do Encurtador de URLs

## Princípios Fundamentais

### I. Limite do Serviço de Aplicação
Os endpoints da Minimal API DEVEM delegar as operações de persistência aos serviços de aplicação.
Os endpoints NÃO DEVEM acessar diretamente o banco de dados nem o contexto de persistência.
Isso mantém as responsabilidades HTTP separadas do acesso a dados e permite testar o comportamento
da aplicação de forma independente.

### II. Isolamento das Regras de Negócio
As regras de negócio DEVEM ficar fora dos endpoints e dos manipuladores HTTP, em serviços de
aplicação ou de domínio. Endpoints e manipuladores PODEM coordenar requisições e respostas, mas
NÃO DEVEM implementar decisões de negócio. Isso evita que o transporte HTTP se torne a fonte do
comportamento de negócio.

### III. Testes Unitários das Regras de Negócio
Toda regra de negócio DEVE ter pelo menos um teste unitário que verifique seu comportamento
esperado. Os testes DEVEM exercitar a regra por meio do componente de aplicação ou domínio que a
implementa, em vez de depender apenas de testes de endpoint.

### IV. Testes de Integração dos Endpoints
Todo endpoint DEVE ter pelo menos um teste de integração para o caminho feliz. Esses testes DEVEM
verificar o contrato do endpoint observável externamente.

### V. Contrato de Erros de Validação
Erros de validação DEVEM retornar HTTP 400 com uma resposta ProblemDetails. Esse formato uniforme
permite que os clientes tratem requisições inválidas de maneira previsível.

## Tecnologias e Convenções

- A aplicação DEVE usar .NET 9, C# e Minimal APIs.
- A persistência DEVE usar SQLite por meio do Entity Framework Core.
- Os testes automatizados DEVEM usar xUnit.
- Os nomes de classes e arquivos DEVEM estar em inglês. As mensagens de erro apresentadas aos
  usuários DEVEM estar em português.
- Uma nova dependência NÃO DEVE ser adicionada sem uma justificativa registrada no plano da
  funcionalidade.
- As mensagens de commit DEVEM seguir o formato `tipo(escopo): descrição - identificador-do-requisito`
  e incluir o identificador do requisito aplicável.

## Planejamento e Conformidade

- Os planos e a implementação DEVEM estar em conformidade com esta constituição. Se um plano
  entrar em conflito com ela, o plano DEVE ser revisado; a constituição NÃO DEVE ser flexibilizada
  para acomodar o plano.
- Todo plano de funcionalidade DEVE identificar os requisitos aplicáveis e justificar previamente
  quaisquer dependências novas propostas.
- Implementadores e revisores DEVEM verificar os princípios aplicáveis nas especificações, nos
  planos, na implementação e nos testes antes de considerar uma alteração concluída.

## Governança

Esta constituição é o padrão de governança do projeto. Suas alterações DEVEM ser revisadas e
registradas em um commit dedicado, separado dos commits de funcionalidades ou implementação.
Qualquer plano que entre em conflito com esta constituição DEVE ser atualizado antes do início
da implementação.

A versão da constituição DEVE seguir o versionamento semântico: MAJOR para remoções ou redefinições
incompatíveis de princípios; MINOR para novos princípios ou expansão material das orientações; e
PATCH para esclarecimentos ou refinamentos semânticos que não alterem os requisitos. Cada alteração
DEVE atualizar a data da última emenda e registrar o impacto na versão.

A conformidade DEVE ser verificada durante o planejamento e a revisão de código. A implementação
e os testes DEVEM demonstrar que os requisitos aplicáveis foram atendidos. Desvios exigem uma
emenda aprovada antes do prosseguimento do trabalho.

**Versão**: 1.0.1 | **Ratificada**: 2026-10-22 | **Última emenda**: 2026-10-22
