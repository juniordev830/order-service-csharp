# Order Service C#

Protótipo em C# que modela pedidos em memória. O código atual contém um registro de pedido, um repositório simples baseado em lista e operações para adicionar e localizar pedidos.

> Apesar do nome, a versão atual é uma aplicação de console, não uma API HTTP nem um serviço distribuído.

## Tecnologias

- C#
- .NET SDK compatível com o arquivo `.csproj`

## Executar

```bash
dotnet build
dotnet run
```

## Comportamento atual

- Cria um pedido de demonstração.
- Valida se o identificador do pedido não está vazio.
- Armazena o pedido na memória do processo.
- Consulta o pedido pelo identificador e imprime o resultado.

Os dados são perdidos quando o programa termina.

## Estrutura

- `Program.cs`: modelos, repositório em memória e ponto de entrada.
- `OrderService.csproj`: configuração do projeto.
- `README.md`: documentação.

## Próximas melhorias

- Separar modelo, repositório e aplicação em arquivos distintos.
- Validar identificador, cliente e valor total.
- Adicionar testes unitários.
- Introduzir persistência se necessário.
- Só depois criar endpoints HTTP, autenticação e documentação de API.

Projeto educacional em evolução.
