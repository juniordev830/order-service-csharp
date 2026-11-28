# Engenharia reversa e estudo — Order Service C#

## Objetivo e fluxo

Pedidos em memória com transições de estado concorrentes. OrderRepository concentra operações no estado dos pedidos. OrderFinancials e OrderInsights produzem visões derivadas para relatórios e consultas.

**Tecnologias identificadas** · C#, .NET 8.

**Revisão de referência** · `0cd22eecbf69`.

## Mapa do código

| Caminho | Papel de leitura | Símbolos selecionados |
| --- | --- | --- |
| [Order.cs](../Order.cs) | Implementação | `OrderService`, `Order` |
| [OrderFinancials.cs](../OrderFinancials.cs) | Implementação | `OrderService`, `OrderFinancialSnapshot`, `OrderFinancials`, `Summarize` |
| [OrderInsights.cs](../OrderInsights.cs) | Implementação | `OrderService`, `OrderQueryResult`, `OrderInsights`, `Query` |
| [OrderRepository.cs](../OrderRepository.cs) | Implementação | `OrderService`, `OrderRepository`, `AddOrder`, `NormalizeStatus`, `UpdateOrderStatus`, `GetOrderById`, `GetAllOrders` |
| [Program.cs](../Program.cs) | Implementação | `OrderService`, `Program`, `Main` |

## Configuração e referências

Ambiente e dependências: [OrderService.csproj](../OrderService.csproj), [tests/OrderService.Tests/OrderService.Tests.csproj](../tests/OrderService.Tests/OrderService.Tests.csproj).

Referências existentes: [docs/GUIA_DE_VALIDACAO.md](GUIA_DE_VALIDACAO.md).

## Roteiro de estudo e desenvolvimento

1. Comece pelo fluxo descrito acima e localize seus pontos de entrada no mapa. Acompanhe uma entrada válida até a transformação ou a resposta.
2. Abra um teste relacionado ao fluxo e descreva qual regra ele protege. Compare o tratamento esperado para entradas válidas, inválidas e nos limites.
3. Ao adicionar uma regra, determine qual módulo controla o dado e cubra o caso nesse nível. Em seguida, confira o efeito no comando, na API ou na interface de entrada.

**Pergunta de entrevista**

Quais invariantes devem permanecer verdadeiras quando duas transições chegam ao mesmo tempo?

## Testes versionados

Arquivos para começar a leitura dos cenários de teste: [tests/OrderService.Tests/OrderFinancialsTests.cs](../tests/OrderService.Tests/OrderFinancialsTests.cs), [tests/OrderService.Tests/OrderInsightsTests.cs](../tests/OrderService.Tests/OrderInsightsTests.cs), [tests/OrderService.Tests/OrderLifecycleTests.cs](../tests/OrderService.Tests/OrderLifecycleTests.cs), [tests/OrderService.Tests/OrderRepositoryTests.cs](../tests/OrderService.Tests/OrderRepositoryTests.cs).

Os comandos e versões de runtime usados no CI ficam em [.github/workflows/ci.yml](../.github/workflows/ci.yml).

[Voltar ao README](../README.md).
