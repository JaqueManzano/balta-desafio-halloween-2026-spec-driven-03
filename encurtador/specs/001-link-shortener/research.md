# Pesquisa Técnica: Encurtador de Links

## Persistência e fronteiras de aplicação

- **Decisão**: Minimal API recebe e traduz requisições; `LinkService` aplica regras; repositório EF Core/SQLite persiste links. Identity gerencia usuários e credenciais.
- **Justificativa**: A constituição proíbe acesso direto à persistência e regras de negócio em endpoints. O usuário autenticado é a origem do proprietário para qualquer operação de gerenciamento.
- **Alternativas consideradas**: Consultas ao `DbContext` diretamente nos handlers foram rejeitadas por violarem a constituição. Uma abstração de repositório por todo o Identity foi rejeitada porque Identity já fornece sua própria integração de armazenamento.

## Autenticação e senhas

- **Decisão**: Usar ASP.NET Core Identity para hash/verificação e gerenciamento de contas; expor somente cadastro, entrada e saída necessários ao escopo. Emitir tokens Bearer por pessoa. Usar e-mail como identificador de entrada; não adicionar confirmação de e-mail ou recuperação de senha.
- **Justificativa**: Evita implementar armazenamento de senha próprio e permite aplicar autorização por ID de usuário. A especificação exclui verificação e recuperação e exige respostas de autenticação que não revelem se a conta ou a senha estava incorreta. O mínimo da senha será oito caracteres, sem requisitos de composição adicionais não especificados.
- **Alternativas consideradas**: Senhas em texto claro, criptografia reversível ou hash rápido genérico foram rejeitados. Cookie seria adequado a um cliente principalmente navegador, mas o plano define API com tokens Bearer. A chave de gerenciamento compartilhada do rascunho foi rejeitada porque não representa de forma segura a propriedade individual das contas.
- **Escopo do token**: O cliente descarta o token ao sair; o plano não acrescenta armazenamento de revogação por token, já que revogação antecipada não é requisito da primeira versão. Não usar JWT nem adicionar infraestrutura de provedor externo sem requisito de interoperabilidade.

## Unicidade e criação de códigos

- **Decisão**: Normalizar códigos em maiúsculas e criar índice único no banco. Para códigos personalizados, transformar a violação específica de unicidade em conflito de código indisponível. Para códigos gerados, repetir a geração somente quando houver colisão única.
- **Justificativa**: Uma checagem prévia não evita uma corrida entre requisições simultâneas. A restrição do banco garante unicidade; tratamento seletivo evita mascarar falhas de persistência não relacionadas.
- **Alternativas consideradas**: A checagem no serviço como única garantia foi rejeitada pela condição de corrida. Geração aleatória criptograficamente segura reduz previsibilidade; o alfabeto sem caracteres ambíguos respeita a preferência do rascunho sem contrariar a exigência de caracteres alfanuméricos.

## Viabilidade dos endpoints de autenticação

- **Decisão**: Configurar `AddIdentityApiEndpoints<TUser>()`, mas não chamar `MapIdentityApi<TUser>()`; mapear somente endpoints próprios para cadastro, entrada, renovação e saída.
- **Justificativa**: No .NET 9, a configuração de serviços e autenticação é separada do mapeamento de todas as rotas pré-definidas. Cadastro usa `UserManager`; entrada usa `SignInManager` com `IdentityConstants.BearerScheme`, permitindo ao handler Identity emitir seus tokens. A rota de renovação consome o refresh token; não se implementa formato de token ou hash de senha próprio.
- **Limitação**: O handler Bearer não revoga access tokens ao sair. O cliente descarta access e refresh tokens, mas access tokens previamente emitidos permanecem válidos até vencerem. Não afirmar revogação do lado do servidor.
- **Alternativas consideradas**: Mapear todas as rotas padrão foi rejeitado porque também expõe fluxos de verificação/recuperação fora do escopo. Cookies permitem encerramento de sessão no servidor, mas não correspondem à escolha de autenticação Bearer adotada.

## Expiração e datas

- **Decisão**: Salvar `DateTime` em UTC. Preencher expiração ausente com 30 dias após criação e tratar `ExpiresAtUtc > nowUtc` como único estado ativo.
- **Justificativa**: Reproduz a regra de borda da especificação e evita comparações dependentes de fuso horário local. Como toda criação recebe expiração fornecida ou padrão, `ExpiresAtUtc` pode ser obrigatório no banco.
- **Alternativas consideradas**: Expiração nula como ausência de prazo foi rejeitada pela clarificação de expiração padrão de 30 dias. Datas locais ou `DateTimeOffset` não são necessários para esta regra UTC e têm limitações de comparação no provedor SQLite.

## Contagem concorrente de redirecionamentos

- **Decisão**: Executar um `UPDATE` SQL parametrizado com incremento no próprio banco, limitado ao código ativo e retornando a URL de destino. Só redirecionar quando o comando retornar um registro.
- **Justificativa**: Elimina a sequência de leitura e regravação que pode perder atualizações concorrentes e garante que links inexistentes/expirados não sejam contabilizados. SQLite serializa escritores; esta abordagem prioriza contagem correta na escala não quantificada da primeira versão.
- **Alternativas consideradas**: Carregar a entidade EF, incrementar em memória e salvar foi rejeitado por perda de atualizações. Tabela de eventos por acesso foi rejeitada por não ser necessária para a contagem agregada atual.

## Estratégia de teste e migrações

- **Decisão**: Testar regras de serviço com xUnit e contratos HTTP com testes de integração usando SQLite, incluindo colisões concorrentes, borda de expiração e contagens com contextos/conexões independentes. Evoluir esquema com migrações EF Core.
- **Justificativa**: A constituição exige teste unitário de cada regra e teste de integração do caminho feliz de cada endpoint. SQLite real é necessário para validar semântica do comando atômico e restrições de unicidade.
- **Alternativas consideradas**: Usar somente provedor EF InMemory foi rejeitado porque não valida semântica nem concorrência específicas do SQLite.

## Referências

- Microsoft, ASP.NET Core Identity API: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-9.0
- Microsoft, `PasswordHasher<TUser>`: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-9.0
- Microsoft, EF Core `ExecuteUpdate`: https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete#executeupdate
- Microsoft, limitações do provedor SQLite EF Core: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations
- SQLite, `RETURNING`: https://www.sqlite.org/lang_returning.html
- SQLite, isolamento: https://www.sqlite.org/isolation.html
- Política de suporte do .NET: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core

## Restrição de plataforma

- **Decisão**: Manter .NET 9 por exigência da constituição; usar o patch de serviço mais recente disponível no momento da implementação.
- **Justificativa**: A constituição é normativa e deve prevalecer sobre preferência de versão. A pesquisa indica que o suporte de .NET 9 termina em 2026-11-10, portanto o plano registra o risco de ciclo de suporte curto.
- **Alternativa considerada**: .NET 10 LTS seria preferível para um projeto novo após sua disponibilidade, mas alterá-lo exige emenda à constituição e não será feito implicitamente neste plano.
