# Order Service C#

Protótipo educacional em C# que modela pedidos em memória. A versão atual é uma aplicação de console com um repositório simples, validações de entrada e testes unitários. **Não é uma API HTTP, não usa banco de dados e não persiste pedidos entre execuções.**

## Tecnologias

- C# e .NET 8
- xUnit para testes automatizados
- GitHub Actions para build, testes e auditoria de pacotes NuGet

## Requisitos

- .NET SDK 8.0

## Executar

Na raiz do repositório:

```bash
dotnet restore
dotnet build --configuration Release
dotnet run
```

O programa cria um pedido demonstrativo, salva no repositório em memória, consulta pelo identificador e imprime o resultado.

## Comportamento implementado

O `OrderRepository` oferece:

- `AddOrder(Order)`: rejeita pedido nulo, identificador vazio, nome de cliente vazio, total menor ou igual a zero, status vazio e IDs duplicados.
- Normalização: remove espaços nas extremidades de identificador, nome do cliente e status antes de salvar.
- `GetOrderById(string)`: consulta por identificador; retorna `null` para uma chave vazia ou inexistente.
- `GetAllOrders()`: devolve uma cópia da coleção, para que alterações posteriores no repositório não modifiquem o snapshot já retornado.

O repositório é baseado em `List<Order>` e não é persistente nem thread-safe. Não deve ser usado como armazenamento de produção.

## Testes

Execute:

```bash
dotnet test tests/OrderService.Tests/OrderService.Tests.csproj --configuration Release
```

Os testes cobrem inclusão e consulta, snapshot da coleção, campos inválidos, IDs duplicados, normalização e consulta com identificadores vazios.

## CI e segurança de dependências

O workflow `.github/workflows/ci.yml` restaura os pacotes, verifica vulnerabilidades NuGet (incluindo dependências transitivas), compila em Release e executa os testes.

## Estrutura

```text
.
├── Order.cs
├── OrderRepository.cs
├── Program.cs
├── OrderService.csproj
├── tests/
│   └── OrderService.Tests/
│       ├── OrderRepositoryTests.cs
│       └── OrderService.Tests.csproj
└── .github/workflows/
    └── ci.yml
```

## Próximas etapas possíveis

- Introduzir persistência apenas quando houver requisito claro.
- Se o projeto evoluir para API, adicionar endpoints, autenticação, autorização, validação HTTP e testes de integração antes de expô-lo publicamente.

Projeto de portfólio em desenvolvimento.
