# 🎮 Cyphers Analytics — Valorant Dashboard

> Dashboard de análise estatística para partidas de Valorant, desenvolvido como TCC do curso de Tecnologias da Informação e Comunicação (TIC) na UFSC.

A aplicação permite o registro de partidas e a visualização de métricas de desempenho dos jogadores.

---

## 🚀 Tecnologias Utilizadas

| Camada | Tecnologia |
|---|---|
| Backend | .NET 8 (C#) com ASP.NET Core Web API |
| Banco de Dados | PostgreSQL 16 |
| ORM | Entity Framework Core (EF Core) |
| Documentação | Swagger / OpenAPI |
| Ferramentas | VS Code, Docker (opcional), pgAdmin 4 |

---

## 📂 Estrutura do Projeto

```
CyphersAnalytics/
├── src/
│   ├── api/            # Configuração da API e Controllers
│   ├── database/       # Contexto do Banco de Dados (EF Core)
│   ├── models/         # Classes de Entidade (Partida, Jogador)
│   └── services/       # Lógica de negócio (opcional)
├── .env                # Variáveis de ambiente
└── README.md
```

---

## 🛠️ Como Rodar o Projeto

### 1. Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- Ferramenta `dotnet-ef`:

```powershell
dotnet tool install --global dotnet-ef
```

### 2. Configuração do Banco de Dados

Certifique-se de que o PostgreSQL está rodando e crie um banco de dados chamado `ValorantDB`.

No arquivo `src/api/appsettings.json`, ajuste a string de conexão:

```json
"DefaultConnection": "Host=localhost;Database=ValorantDB;Username=postgres;Password=SUA_SENHA"
```

### 3. Rodar as Migrations (Criar as Tabelas)

No terminal, dentro da pasta `src/api`, execute:

```powershell
dotnet ef database update
```

### 4. Executar a API

Ainda na pasta `src/api`:

```powershell
dotnet run
```

Acesse a documentação interativa em:

```
https://localhost:PORTA/swagger
```

---

## 📊 Endpoints Principais

| Método | Endpoint | Descrição |
|---|---|---|
| `GET` | `/api/Partidas` | Lista todas as partidas gravadas. |
| `POST` | `/api/Partidas` | Registra uma nova partida de Valorant. |
| `GET` | `/api/Partidas/{id}` | Busca detalhes de uma partida específica. |

---
**Karolini** — Estudante de TIC @ UFSC · Graduação 2026
