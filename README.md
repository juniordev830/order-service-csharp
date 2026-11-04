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
- `GetOrderHistory(string orderId)`: devolve uma lista de eventos **imutáveis** de criação e mudanças de status, ordenada por sequência. Uma atualização repetida e sem mudança não gera evento duplicado. Eventos incluem `Sequence`, `PreviousStatus`, `Status` e timestamp UTC.

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

Histórico de auditoria em memória:

```csharp
var events = repository.GetOrderHistory("ORD-42");
foreach (var change in events)
    Console.WriteLine($"{change.Sequence}: {change.PreviousStatus} -> {change.Status}");
```

O evento inicial tem `PreviousStatus = null`. Se uma mudança falhar, o histórico não é alterado. A ordem garantida vem de `Sequence` dentro de cada pedido; timestamps são horários do relógio UTC do processo, que podem sofrer ajustes.

Status desconhecidos e IDs vazios causam `ArgumentException`. IDs inexistentes em atualizações causam `KeyNotFoundException`; transições proibidas causam `InvalidOperationException`. Em todos esses casos, o pedido permanece inalterado.

## Consistência e limites

O repositório mantém uma lista em memória protegida por um `lock` por instância. Leitura, criação e atualização usam o mesmo lock, evitando estados intermediários observáveis dentro da instância. Uma transição cria um novo record `Order`, preservando os snapshots anteriores.

**Não há** banco, autenticação, trilha de auditoria persistente, transações distribuídas ou coordenação entre múltiplas instâncias. Os eventos de auditoria se perdem ao reiniciar o processo. O mecanismo não é pronto para produção e o exemplo não realiza cobranças, remessas ou entregas reais.

## Testes

```bash
dotnet test tests/OrderService.Tests/OrderService.Tests.csproj --configuration Release
```

A suíte verifica inclusão, leitura, validação, IDs duplicados, normalização, concorrência de criação, transições permitidas e proibidas, estados terminais, idempotência, atomicidade de atualizações concorrentes, histórico de eventos de status e snapshots imutáveis.

O workflow `.github/workflows/ci.yml` executa build e testes em pull requests. Também verifica vulnerabilidades de dependências NuGet.

## Próximas etapas

- Separar regras de negócio em uma camada de serviço e repositório persistente.
- [x] Adicionar histórico de status em memória com eventos de auditoria imutáveis.
- [ ] Persistir os eventos com garantias transacionais, retenção e controle de acesso.
- Expor uma API somente após autenticação, autorização, validação HTTP e testes de integração.
- Adicionar observabilidade, operações idempotentes persistentes e tratamento de concorrência entre instâncias.

Projeto de portfólio mantido na conta [juniordev830](https://github.com/juniordev830).
