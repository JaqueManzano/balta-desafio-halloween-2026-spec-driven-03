# Feature Specification: Encurtador de Links

**Feature Branch**: `[not created]`

**Created**: 2026-10-22

**Status**: Draft

**Input**: User description: "Um encurtador de links. Permitir transformar uma URL longa em um link curto, compartilhar esse link e acompanhar quantos acessos recebeu, com autenticação de usuários."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar e compartilhar um link curto (Priority: P1)

Uma pessoa autenticada informa uma URL de destino e recebe um link curto para compartilhar. Pode deixar o sistema gerar o código ou escolher um código próprio.

**Why this priority**: A criação e o compartilhamento do link são o valor central do produto.

**Independent Test**: Autenticar uma pessoa, criar um link com URL válida e confirmar que o link curto retornado pode ser aberto e direciona para o destino.

**Acceptance Scenarios**:

1. **Given** uma pessoa autenticada e uma URL de destino válida, **When** ela cria um link sem informar um código, **Then** o sistema devolve um link curto com código alfanumérico de exatamente 7 caracteres.
2. **Given** uma pessoa autenticada e uma URL de destino válida, **When** ela informa um código próprio único de 4 a 20 caracteres alfanuméricos, **Then** o sistema devolve um link curto usando esse código.
3. **Given** uma pessoa autenticada, **When** ela informa uma URL inválida ou um código fora do tamanho permitido, **Then** o link não é criado e ela recebe uma indicação clara do dado inválido.
4. **Given** que o código informado já pertence a outro link, **When** a pessoa tenta criá-lo novamente, **Then** o sistema recusa a criação e informa que o código não está disponível.

### User Story 2 - Acessar um link compartilhado (Priority: P1)

Qualquer pessoa que abra um link curto ativo é encaminhada para a URL de destino sem precisar estar autenticada.

**Why this priority**: Um link curto só cumpre sua finalidade quando pode ser compartilhado e aberto por outras pessoas.

**Independent Test**: Abrir um link curto ativo em uma sessão sem autenticação e verificar que o navegador é encaminhado à URL cadastrada.

**Acceptance Scenarios**:

1. **Given** um link curto existente e não expirado, **When** alguém o acessa, **Then** essa pessoa é redirecionada à URL de destino.
2. **Given** um código que não corresponde a um link, **When** alguém o acessa, **Then** o sistema informa que o link não foi encontrado e não redireciona.
3. **Given** um link que já expirou, **When** alguém o acessa, **Then** o sistema informa que o link expirou e não redireciona.
4. **Given** um link ativo que já foi acessado, **When** a mesma pessoa o abre novamente, **Then** ocorre novo redirecionamento e o novo acesso é contabilizado.

### User Story 3 - Consultar acessos e gerenciar expiração (Priority: P2)

Uma pessoa autenticada consulta os links que criou e o total de acessos de cada link. Ao criar um link, pode definir uma data e hora de expiração.

**Why this priority**: A contagem permite avaliar o alcance dos links, enquanto a expiração permite limitar por quanto tempo eles funcionam.

**Independent Test**: Criar um link, acessá-lo, consultar sua contagem como criador e confirmar que uma pessoa diferente não consegue consultar os dados privados desse link.

**Acceptance Scenarios**:

1. **Given** uma pessoa autenticada que criou um link, **When** consulta seus links, **Then** vê o destino, o link curto, o estado de expiração e o total de acessos de cada um.
2. **Given** um link ativo criado por uma pessoa, **When** ele é aberto com sucesso, **Then** o total de acessos aumenta em uma unidade.
3. **Given** um link criado por outra pessoa, **When** uma pessoa autenticada tenta consultar sua contagem, **Then** o acesso aos dados é negado.
4. **Given** uma pessoa autenticada e uma data e hora futuras, **When** ela cria um link com essa expiração, **Then** o link redireciona antes do instante definido e é considerado expirado a partir desse instante.
5. **Given** uma pessoa autenticada e uma expiração igual ou anterior ao instante atual, **When** ela tenta criar o link, **Then** a criação é recusada com indicação clara do erro.

### User Story 4 - Acessar a conta (Priority: P1)

Uma pessoa pode criar uma conta e entrar com suas credenciais para criar links e consultar informações privadas dos links que criou.

**Why this priority**: A autenticação identifica quem criou cada link e protege a consulta de estatísticas.

**Independent Test**: Criar uma conta, entrar e sair, e verificar que apenas a sessão autenticada consegue acessar a área de gerenciamento de links.

**Acceptance Scenarios**:

1. **Given** uma pessoa sem conta, **When** ela fornece dados válidos para se cadastrar, **Then** sua conta é criada e ela pode entrar.
2. **Given** uma conta existente, **When** a pessoa fornece credenciais válidas, **Then** entra e pode gerenciar seus links.
3. **Given** credenciais incorretas, **When** a pessoa tenta entrar, **Then** o acesso é recusado sem revelar se a conta ou a senha estava incorreta.
4. **Given** uma pessoa não autenticada, **When** tenta criar um link ou consultar estatísticas, **Then** o sistema solicita autenticação ou nega o acesso.

### Edge Cases

- URL sem esquema, malformada ou com esquema diferente de HTTP/HTTPS deve ser recusada; nenhum link deve ser criado.
- Dois pedidos simultâneos com o mesmo código personalizado devem resultar em no máximo um link criado com esse código; os demais devem receber aviso de indisponibilidade.
- Se a geração automática produzir um código já utilizado, o sistema deve gerar outro código antes de concluir a criação.
- O instante de expiração é exclusivo: um link deixa de redirecionar quando o instante atual é igual ou posterior à expiração.
- Falhas ao salvar um link não devem devolver um link curto como se a criação tivesse sido concluída.
- A contagem deve incluir cada requisição que resulte em redirecionamento bem-sucedido, inclusive visitas repetidas pela mesma pessoa; acessos a códigos inexistentes ou expirados não contam.
- Encerrar e reiniciar a aplicação não deve remover contas, links ou contagens já registrados.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir que uma pessoa crie uma conta e entre com credenciais válidas.
- **FR-002**: O sistema MUST exigir autenticação para criar links e consultar estatísticas ou dados de gerenciamento.
- **FR-003**: O sistema MUST aceitar uma URL de destino HTTP ou HTTPS válida e recusar valores inválidos ou esquemas diferentes.
- **FR-004**: O sistema MUST gerar automaticamente, quando nenhum código for informado, um código alfanumérico de exatamente 7 caracteres.
- **FR-005**: O sistema MUST aceitar um código alfanumérico personalizado com comprimento entre 4 e 20 caracteres, inclusive, desde que ainda não esteja em uso.
- **FR-006**: O sistema MUST garantir que cada código identifica no máximo um link; códigos personalizados duplicados devem ser recusados e códigos automáticos em conflito devem ser gerados novamente.
- **FR-007**: O sistema MUST devolver o link curto correspondente após a criação bem-sucedida.
- **FR-008**: O sistema MUST redirecionar qualquer visitante de um link ativo à URL de destino, sem exigir autenticação para o redirecionamento.
- **FR-009**: O sistema MUST permitir que o criador informe uma data e hora de expiração futura para o link e recusar uma data e hora igual ou anterior ao instante atual.
- **FR-010**: O sistema MUST recusar o redirecionamento de links expirados ou inexistentes e apresentar uma resposta apropriada.
- **FR-011**: O sistema MUST incrementar em uma unidade a contagem de acessos a cada requisição que resulte em redirecionamento bem-sucedido.
- **FR-012**: O sistema MUST permitir que uma pessoa autenticada consulte o total de acessos e os dados dos links que criou, sem permitir consulta dos links de outra pessoa.
- **FR-013**: O sistema MUST preservar contas, links, expirações e contagens após o encerramento e reinício da aplicação.
- **FR-014**: O sistema MUST apresentar mensagens de erro de validação em português, sem criar recursos para entradas inválidas.

### Key Entities *(include if feature involves data)*

- **Pessoa usuária**: Conta que autentica uma pessoa e identifica quais links ela criou.
- **Link curto**: Associação entre um código único, uma URL de destino e a pessoa criadora; pode ter expiração e registra sua contagem de acessos.
- **Acesso**: Requisição bem-sucedida a um link ativo que resulta em redirecionamento e incrementa a contagem daquele link.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em testes com URLs válidas, 100% dos links criados devolvem um código no formato escolhido: 7 caracteres alfanuméricos gerados ou 4 a 20 caracteres alfanuméricos personalizados.
- **SC-002**: Em testes de ponta a ponta, 100% dos links ativos abertos antes da expiração encaminham o visitante à URL cadastrada, sem exigir autenticação do visitante.
- **SC-003**: Em testes de contagem, cada redirecionamento bem-sucedido aumenta o total exatamente em uma unidade; acessos sem redirecionamento bem-sucedido não alteram o total.
- **SC-004**: Em testes de autorização, 100% das tentativas de consultar ou gerenciar links de outra pessoa são negadas.
- **SC-005**: Após reiniciar a aplicação em um teste, 100% das contas, links e contagens previamente confirmados continuam disponíveis.
- **SC-006**: Pelo menos 90% das pessoas em um teste de usabilidade conseguem criar e copiar um link curto válido em até 2 minutos, sem ajuda.
- **SC-007**: Pelo menos 90% das pessoas em um teste de usabilidade conseguem identificar o total de acessos de um link próprio em até 1 minuto.

## Assumptions

- O público inclui pessoas criadoras autenticadas e visitantes que recebem links compartilhados; visitantes não precisam de conta para redirecionamentos.
- O cadastro e o login usam e-mail e senha, e mensagens de autenticação não revelam se um e-mail está cadastrado.
- Códigos personalizados aceitam apenas letras e números; códigos não diferenciam maiúsculas de minúsculas para fins de unicidade.
- Datas e horas de expiração são interpretadas e comparadas em UTC. Um link é válido antes do instante definido e expirado a partir dele.
- Cada requisição bem-sucedida de redirecionamento conta como um acesso, sem deduplicação por pessoa, dispositivo ou intervalo de tempo.
- Links, contas e contagens são mantidos sem prazo de retenção definido nesta versão.
- A primeira versão não inclui edição do destino, exclusão de links, estatísticas detalhadas por período ou proteção contra uso malicioso além da validação de entradas descrita.
