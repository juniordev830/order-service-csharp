# Guia de manutenção — Order Service C#

## Escopo confirmado

Biblioteca e demo em console .NET 8, com estados de pedidos em memória, sem API nem banco.

## Arquivos principais

`Order.cs`, `OrderRepository.cs`, `OrderFinancials.cs`, `OrderInsights.cs`, `Program.cs` e `tests/OrderService.Tests/`.

## Validar uma alteração

No diretório raiz, usar os comandos adequados ao ambiente: `dotnet restore`, `dotnet build --configuration Release`, `dotnet test` e `dotnet run`.

Registrar o resultado dos testes no PR e conferir se a execução do GitHub Actions corresponde ao commit revisado. Não presumir que a interface ou o serviço é de produção apenas porque o build passou.

## Revisão de casos críticos

Verificar transições inválidas, idempotência de mudanças de estado, overflow monetário, isolamento de snapshots e concorrência em inclusão/atualização.

## Limites e próxima entrega

Dados em memória desaparecem ao finalizar. Próxima etapa: contratos da camada de persistência e testes de recuperação de falhas antes de adicionar HTTP.

### Checklist para PR

- [ ] A mudança está associada a um comportamento identificável.
- [ ] Foram documentados os casos novos e os limites encontrados.
- [ ] Há teste automatizado quando o módulo comporta execução reproduzível.
- [ ] Exemplos não contêm credenciais ou dados reais.
- [ ] A branch pode ser revisada antes de qualquer merge na principal.
