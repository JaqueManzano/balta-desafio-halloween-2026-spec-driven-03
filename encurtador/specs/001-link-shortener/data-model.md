# Modelo de Dados: Encurtador de Links

## Account (usuário Identity)

Representa uma pessoa autenticada e proprietária dos links que criou. A implementação usa a entidade de usuário do ASP.NET Core Identity e seu armazenamento via EF Core.

| Campo | Tipo/conteúdo | Restrições |
|-------|---------------|------------|
| `Id` | Identificador Identity | Chave primária; referenciado pelo proprietário do link. |
| `Email` | E-mail | Obrigatório; usado para cadastro e entrada. |
| `NormalizedEmail` | E-mail normalizado | Índice único para impedir contas duplicadas sem distinção de caixa. |
| `UserName` / `NormalizedUserName` | E-mail normalizado | Configurado como identificador de login baseado em e-mail. |
| `PasswordHash` | Hash Identity | Obrigatório; nunca expor em respostas. Senha em texto claro não é persistida. |
| Campos internos Identity | Conforme Identity | Mantidos pelo mecanismo para segurança da conta; fora dos contratos públicos. |

Política: senha de pelo menos 8 caracteres. Não há verificação de e-mail, recuperação de senha nem perfil adicional nesta versão.

## Link (agregado)

Representa o endereço curto e a URL de destino. Sua contagem total é mantida no próprio registro.

| Campo | Tipo | Restrições |
|-------|------|------------|
| `Id` | Inteiro/long | Chave primária. |
| `Code` | Texto | Obrigatório, 4–20 caracteres para código próprio; 7 caracteres se gerado. Alfanumérico; armazenado em forma canônica maiúscula; índice único. |
| `TargetUrl` | Texto | Obrigatório; URL absoluta válida com esquema HTTP ou HTTPS. |
| `CreatedAtUtc` | `DateTime` UTC | Obrigatório; definido na criação. |
| `ExpiresAtUtc` | `DateTime` UTC | Obrigatório; futuro no momento de criação. Usa o valor informado ou o padrão `CreatedAtUtc + 30 dias`. |
| `AccessCount` | `long` | Obrigatório; inicializado em zero; incremento atômico somente em redirecionamento bem-sucedido. |
| `OwnerId` | Tipo do ID Identity | Obrigatório; chave estrangeira para Account. Operações de gerenciamento filtram por este valor. |

### Regras e estados

- Código é único sem distinção entre maiúsculas e minúsculas. Código personalizado concorrente: no máximo uma criação vence; as outras recebem conflito. Colisão de código gerado: tentar outro código.
- Um link está ativo somente quando `ExpiresAtUtc > nowUtc`. Em `nowUtc >= ExpiresAtUtc`, está expirado.
- O redirecionamento incrementa `AccessCount` no banco apenas se o link estiver ativo no instante da operação. Inexistente/expirado não incrementa.
- `OwnerId` não muda; o usuário pode listar e consultar somente links próprios.
- Não há entidade por visita; não há edição, exclusão ou limpeza automática nesta versão.

## Relações

- `Account (1) → (N) Link`: cada link pertence a exatamente uma conta; uma conta pode possuir zero ou mais links.
- Não há tabela de `Access` individual; cada redirecionamento bem-sucedido soma uma unidade ao agregado `Link`.

## Índices e persistência

- Chave única em `Link.Code` (forma canônica).
- Chave/índice único normalizado em e-mail conforme configuração de Identity.
- Chave estrangeira `Link.OwnerId → Account.Id`.
- Migrações EF Core mantêm o esquema e a persistência SQLite entre reinicializações.
