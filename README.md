# Order Service C#

Projeto educacional de Engenharia de Software em **C# e .NET 8** que modela pedidos com regras de negócio, transições de estado atômicas e testes de concorrência. É uma aplicação de console que usa dados em memória: **não é uma API HTTP, não usa banco de dados e não persiste pedidos entre execuções**.

## Tecnologias

- C# / .NET 8 e records imutáveis
- xUnit para testes automatizados
- GitHub Actions para build, testes e auditoria de pacotes NuGet

## Requisitos e execução

Instale o .NET SDK 8. Na raiz do repositório:

```bash
dotnet restore
dotnet build --configuration Release
dotnet run
```

A demonstração cria um pedido e percorre o fluxo `PENDING → CONFIRMED → SHIPPED → DELIVERED`. Nenhuma transação financeira real é realizada.

## Regras de negócio

O `OrderRepository` oferece:

- `AddOrder(Order)`: valida ID, cliente, total maior que zero e status conhecido; normaliza espaços e o status para maiúsculas; rejeita IDs duplicados.
- `GetOrderById(string)`: busca por ID; retorna `null` para ID ausente ou vazio.
- `GetAllOrders()`: devolve um snapshot da coleção.
- `UpdateOrderStatus(string orderId, string nextStatus)`: altera o status atomicamente ou rejeita transições inválidas. Aceita diferenças de caixa e espaços nas extremidades; repetir o status atual é idempotente.

### Máquina de estados do pedido

| Status atual | Próximos estados permitidos |
| --- | --- |
| `PENDING` | `CONFIRMED`, `CANCELLED` |
| `CONFIRMED` | `SHIPPED`, `CANCELLED` |
| `SHIPPED` | `DELIVERED` |
| `DELIVERED` | Nenhum (terminal) |
| `CANCELLED` | Nenhum (terminal) |

`AddOrder` aceita os cinco status conhecidos para permitir carregar pedidos existentes. `UpdateOrderStatus` bloqueia retrocessos e cancelamentos após o envio.

**Exemplo de integração:**

```csharp
var repository = new OrderRepository();
repository.AddOrder(new Order("ORD-42", "Cliente Exemplo", 49.90m, "PENDING"));

var confirmado = repository.UpdateOrderStatus("ORD-42", "CONFIRMED");
var enviado = repository.UpdateOrderStatus("ORD-42", "SHIPPED");
Console.WriteLine(enviado.Status); // SHIPPED
```

Status desconhecidos e IDs vazios causam `ArgumentException`. IDs inexistentes em atualizações causam `KeyNotFoundException`; transições proibidas causam `InvalidOperationException`. Em todos esses casos, o pedido permanece inalterado.

## Consistência e limites

O repositório mantém uma lista em memória protegida por um `lock` por instância. Leitura, criação e atualização usam o mesmo lock, evitando estados intermediários observáveis dentro da instância. Uma transição cria um novo record `Order`, preservando os snapshots anteriores.

**Não há** banco, autenticação, eventos de domínio, auditoria, transações distribuídas ou coordenação entre múltiplas instâncias. O mecanismo não é pronto para produção e o exemplo não realiza cobranças, remessas ou entregas reais.

## Testes

```bash
dotnet test tests/OrderService.Tests/OrderService.Tests.csproj --configuration Release
```

A suíte verifica inclusão, leitura, validação, IDs duplicados, normalização, concorrência de criação, transições permitidas e proibidas, estados terminais, idempotência, atomicidade de atualizações concorrentes e snapshots imutáveis.

O workflow `.github/workflows/ci.yml` executa build e testes em pull requests. Também verifica vulnerabilidades de dependências NuGet.

## Próximas etapas

- Separar regras de negócio em uma camada de serviço e repositório persistente.
- Adicionar histórico/auditoria de status e eventos de domínio.
- Expor uma API somente após autenticação, autorização, validação HTTP e testes de integração.
- Adicionar observabilidade, operações idempotentes persistentes e tratamento de concorrência entre instâncias.

Projeto de portfólio mantido na conta [juniordev830](https://github.com/juniordev830).


## Relatórios de pedidos e paginação

O novo módulo `OrderInsights.cs` calcula um **relatório somente de leitura**, a partir de uma fotografia dos pedidos em memória. O relatório informa quantidade total, soma dos valores e contagem por status. A consulta aceita filtro por status, página a partir de zero e tamanho de página entre 1 e 100.

```csharp
var repository = new OrderRepository();
repository.AddOrder(new Order("ORD-001", "Cliente demonstrativo", 25m, "PENDING"));
var report = OrderInsights.Query(repository, status: "PENDING", page: 0, pageSize: 10);
Console.WriteLine(report.TotalOrders); // 1
Console.WriteLine(report.TotalMatching); // 1
Console.WriteLine(report.CountsByStatus["PENDING"]); // 1
```

A ordenação usa o ID de pedido para que a paginação tenha resultado determinístico. Um relatório já gerado não muda quando outros pedidos são adicionados ou atualizados posteriormente. Pedidos inexistentes não são inventados, e páginas além do fim retornam lista vazia.

**Limitações:** os totais incluem todos os pedidos, inclusive cancelados, porque refletem o valor registrado nos dados do protótipo; **não representam faturamento recebido**. A aplicação continua sem banco, API, pagamentos ou integração de vendas reais.

## Indicadores financeiros por status de pedido

O novo `OrderFinancials.Summarize(repository)` fornece valores registrados por status, separando os grupos **em andamento** (`PENDING`, `CONFIRMED`, `SHIPPED`), **entregues** e **cancelados**, além da média por pedido e do total registrado. Todas as métricas são calculadas a partir de um único snapshot e devolvidas como estrutura de leitura, com proteção contra overflow de `decimal`. Os testes cobrem pedidos vazios, alteração posterior de status, valores máximos e operações concorrentes.

**Atenção:** pedidos entregues não significam pagamentos liquidados. Estes indicadores são exclusivamente valores do protótipo em memória e não constituem relatório de faturamento real.
