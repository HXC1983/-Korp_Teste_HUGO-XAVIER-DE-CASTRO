# Korp_Teste Técnico

Teste técnico desenvolvido para a KORP, com foco na implementação de processos de criação e faturamento de notas fiscais e aplicação de conceitos de desenvolvimento de software.

**Desenvolvedor**: Hugo Xavier de Castro

## Sobre o teste

A aplicação foi desenvolvida para atender aos requisitos propostos no teste técnico, contemplando funcionalidades relacionadas ao processo de criação e faturamento de notas fiscais.

O projeto foi estruturado utilizando uma arquitetura com front-end em Angular e back-end dividido em **dois microsserviços independentes** em C# com ASP.NET Core: o **EstoqueService**, responsável pelo controle de produtos e saldo, e o **FaturamentoService**, responsável pela gestão das notas fiscais. Os dois serviços se comunicam entre si via HTTP REST, com tratamento de falhas implementado através da biblioteca Polly, garantindo que o sistema se recupere de indisponibilidades temporárias e informe o usuário de forma clara quando isso ocorre.

Cada microsserviço possui seu próprio banco de dados, fisicamente separado, reforçando a independência entre os serviços.

## Funcionalidades

- Criação de produtos, com controle de saldo em estoque.
- Criação de notas fiscais, com numeração sequencial e múltiplos itens.
- Faturamento (impressão) de notas fiscais, com débito automático do estoque e bloqueio de reimpressão.
- Tratamento de falha de comunicação entre os microsserviços, com nova tentativa automática e aviso ao usuário em caso de indisponibilidade.

## Tecnologias utilizadas

**Front-end:**
- Angular
- Angular CLI
- TypeScript
- RxJS
- CSS3

**Back-end:**
- C#
- ASP.NET Core Web API
- Polly (tratamento de falhas entre microsserviços)

**Banco de dados:**
- SQL Server
- Entity Framework Core

**Ambiente e ferramentas:**
- Node.js
- npm
- Microsoft Visual Studio

## Estrutura do projeto

O projeto está organizado em três partes principais:

- **EstoqueService**: microsserviço em C#/ASP.NET Core responsável pelo controle de produtos e saldo, com banco de dados próprio.
- **FaturamentoService**: microsserviço em C#/ASP.NET Core responsável pela criação e impressão das notas fiscais, também com banco de dados próprio.
- **Frontend**: aplicação desenvolvida em Angular, que consome as APIs dos dois microsserviços.

## Como executar o projeto

É necessário rodar as três partes ao mesmo tempo, cada uma em um terminal:

```bash
# Terminal 1 - EstoqueService (porta 5001)
cd src/EstoqueService
dotnet restore
dotnet ef database update
dotnet run

# Terminal 2 - FaturamentoService (porta 5002)
cd src/FaturamentoService
dotnet restore
dotnet ef database update
dotnet run

# Terminal 3 - Frontend (porta 4200)
cd frontend
npm install
ng serve
```

Depois, é só acessar `http://localhost:4200` no navegador.

## Detalhamento técnico

- **Ciclos de vida do Angular utilizados:** `OnInit`, usado para carregar os dados assim que a tela é aberta.
- **Uso de RxJS:** sim — usado para recarregar listas após cadastro, filtrar produtos por busca em tempo real e tratar erros das chamadas HTTP.
- **Uso de LINQ:** sim — usado em consultas ao banco (filtros, ordenação, projeção de dados) e no cálculo da numeração sequencial das notas fiscais.
- **Tratamento de erros e exceções no backend:** feito de forma centralizada, com exceções próprias para cada tipo de erro (produto não encontrado, saldo insuficiente, serviço indisponível, etc.), convertidas em respostas HTTP padronizadas.

  ## Vídeo

  https://drive.google.com/file/d/1T6JZI7Q1_rzvoHGuj6zX2fjmX9QPSEMi/view?usp=sharing
