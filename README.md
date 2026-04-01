<div align="center">

# 🎮 Cyphers Analytics Backend

### Valorant Dashboard — Análise Estatística de Partidas

![Status](https://img.shields.io/badge/status-em%20desenvolvimento-yellow?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![License](https://img.shields.io/badge/licen%C3%A7a-MIT-green?style=for-the-badge)

<br/>

> Dashboard de análise estatística para partidas de **Valorant**, desenvolvido como TCC do curso de **Tecnologias da Informação e Comunicação (TIC)** na UFSC.  
> A aplicação permite o registro de partidas e a visualização de métricas de desempenho dos jogadores.

</div>

---

## 📌 Índice

- [Sobre o Projeto](#-sobre-o-projeto)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Como Rodar o Projeto](#-como-rodar-o-projeto)
- [Endpoints Principais](#-endpoints-principais)
- [Contribuição](#-contribuição)
- [Licença](#-licença)
- [Autora](#-autora)

---

## 💡 Sobre o Projeto

O **Cyphers Analytics** nasceu da ideia de unir a paixão por jogos com o desenvolvimento de software. O projeto consome e organiza dados de partidas do Valorant, expondo uma API RESTful que permite:

- 📋 **Registrar** partidas jogadas com detalhes de desempenho
- 🔍 **Consultar** histórico de partidas por jogador
- 📈 **Visualizar** métricas de desempenho via dashboard
- 📄 **Explorar** todos os endpoints pela documentação interativa do Swagger

---

## 🚀 Tecnologias Utilizadas

| Camada | Tecnologia | Versão |
|---|---|---|
| Backend | ASP.NET Core Web API (C#) | .NET 8 |
| Banco de Dados | PostgreSQL | 16 |
| ORM | Entity Framework Core | — |
| Documentação | Swagger / OpenAPI | — |
| Editor | VS Code | — |
| Infra (opcional) | Docker + pgAdmin 4 | — |

---

## 📂 Estrutura do Projeto

```
CyphersAnalytics/
├── src/
│   ├── api/            # Controllers, configuração da API e Program.cs
│   ├── database/       # DbContext e configurações do EF Core
│   ├── models/         # Classes de entidade (Partida, Jogador, etc.)
│   └── services/       # Lógica de negócio (opcional)
├── .env                # Variáveis de ambiente (não versionar!)
├── .gitignore
└── README.md
```

> ⚠️ **Atenção:** nunca suba o arquivo `.env` com credenciais reais para o repositório. Adicione-o ao `.gitignore`.

---

## 🛠️ Como Rodar o Projeto

### Pré-requisitos

Certifique-se de ter instalado na sua máquina:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- Ferramenta `dotnet-ef` (instale via terminal):

```bash
dotnet tool install --global dotnet-ef
```

---

### Passo 1 — Clone o repositório

```bash
git clone https://github.com/seu-usuario/cyphers-analytics.git
cd cyphers-analytics
```

---

### Passo 2 — Configure o Banco de Dados

Certifique-se de que o PostgreSQL está rodando e crie um banco chamado `ValorantDB`.

No arquivo `src/api/appsettings.json`, configure sua string de conexão:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ValorantDB;Username=postgres;Password=SUA_SENHA"
  }
}
```

---

### Passo 3 — Rode as Migrations

Dentro da pasta `src/api`, execute:

```bash
dotnet ef database update
```

Isso criará automaticamente todas as tabelas no banco de dados.

---

### Passo 4 — Execute a API

Ainda na pasta `src/api`:

```bash
dotnet run
```

Acesse a documentação interativa do Swagger em:

```
https://localhost:PORTA/swagger
```

---

## 📊 Endpoints Principais

| Método | Endpoint | Descrição |
|---|---|---|
| `GET` | `/api/Partidas` | Lista todas as partidas registradas |
| `POST` | `/api/Partidas` | Registra uma nova partida de Valorant |
| `GET` | `/api/Partidas/{id}` | Retorna os detalhes de uma partida específica |

> 💡 A documentação completa com todos os parâmetros e exemplos de requisição/resposta está disponível no **Swagger** após rodar o projeto.

---

## 🤝 Contribuição

Contribuições são bem-vindas! Siga os passos abaixo:

1. Faça um **fork** do projeto
2. Crie uma branch para sua feature:
   ```bash
   git checkout -b feature/minha-feature
   ```
3. Faça o **commit** das suas alterações:
   ```bash
   git commit -m "feat: adiciona minha feature"
   ```
4. Faça o **push** para a branch:
   ```bash
   git push origin feature/minha-feature
   ```
5. Abra um **Pull Request** 🚀

---

## 📄 Licença

Este projeto está sob a licença **MIT**. Consulte o arquivo [LICENSE](./LICENSE) para mais detalhes.

---

## 👩‍💻 Autora

<div align="center">

**Karolini**  
Estudante de TIC @ UFSC · Graduação 2026

[![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/seu-usuario)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/karolini-ronzani-pedrozo-48124b268)

</div>
