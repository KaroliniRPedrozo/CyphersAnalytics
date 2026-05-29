# <img src="./src/frontend/src/assets/valorant-white-logo.png" width="70" align="center"> Cypher's Analytics

> Uma plataforma web de alta performance para compilação, análise de dados e visualização de estatísticas do Valorant. Desenvolvida com foco em fornecer uma experiência de usuário (UX) fluida, arquitetura escalável e interfaces altamente intuitivas.

![Status do Projeto](https://img.shields.io/badge/Status-Em_Desenvolvimento-yellow?style=for-the-badge)
![React](https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB)
![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)

---

## 📋 Sobre o Projeto

O **Cypher's Analytics** é uma solução Full Stack robusta projetada para converter dados brutos do ecossistema competitivo de Valorant em insights visuais acionáveis. A aplicação permite que jogadores monitorem seu desempenho histórico, identifiquem suas melhores composições de agentes, analisem taxas de vitória por mapa e acompanhem a evolução de seus ranks.

A engenharia do projeto prioriza boas práticas de **Interação Humano-Computador (IHC)** e princípios de design moderno, garantindo acessibilidade, clareza gráfica na apresentação de dados complexos e tempos de resposta otimizados por meio de uma estratégia eficiente de cache local.

---

## ✨ Funcionalidades Principais

- 📊 **Dashboard de Performance Interativo:** Painel geral contendo métricas consolidadas como KDA Geral, Taxa de Vitória e Total de Partidas.
- 🧑‍🚀 **Módulo de Agentes Inteligente:** Análise aprofundada de desempenho com agrupamento automatizado para indicar o melhor agente do jogador baseado na média de KDA.
- 🗺️ **Estatísticas Geográficas (Mapas):** Cruzamento de dados de performance segmentados por mapas, evidenciando pontos fortes e fracos do usuário.
- ⚡ **Sincronização com Cache Otimizado:** Sistema inteligente que minimiza requisições a APIs externas, atualizando dados apenas quando necessário (intervalo de 15 minutos) e prevenindo gargalos de *rate limiting*.
- 📢 **Monitorização de Status:** Verificação em tempo real de modificações estruturais no jogo, como agentes desativados ou indisponíveis.
- 🔐 **Gestão Autenticada de Usuários:** Módulo completo de registro, login e atualização de perfil com persistência segura e criptografia avançada.

---

## 🚀 Arquitetura e Tecnologias

A aplicação é dividida em uma arquitetura desacoplada (Client-Server), garantindo independência de deploy e manutenibilidade:

### Frontend

- **React (Vite):** Framework ágil para componentização e renderização reativa da interface.
- **JavaScript (JSX):** Lógica estruturada para manipulação de estados globais e consumo de serviços assíncronos.
- **Figma:** Utilizado na fase de concepção e prototipagem de alta fidelidade das interfaces e componentes visuais (IHC).

### Backend & Segurança

- **C# / .NET:** Web API RESTful estruturada com injeção de dependência nativa e controllers assíncronos.
- **Entity Framework Core:** ORM utilizado para abstração de dados e gerenciamento de migrações estruturais.
- **BCrypt.Net:** Mecanismo de hashing de alta segurança para proteção contra ataques de força bruta no armazenamento de senhas.
- **Asp.Versioning:** Implementação de controle de versão estrito na API, garantindo retrocompatibilidade através do padrão `/api/v1/`.

### Banco de Dados

- **PostgreSQL:** Banco relacional encarregado do armazenamento de credenciais, dados de cache de jogadores e histórico imutável de partidas.

<<<<<<< HEAD
#### Diagrama de Entidades (ER)

```
┌─────────────────────────────────┐
│          USUARIOS               │
├─────────────────────────────────┤
│ • Id (PK)                       │
│ • Email                         │
│ • SenhaHash                     │
│ • GameName                      │
│ • TagLine                       │
│ • CriadoEm                      │
└─────────┬───────────────────────┘
          │
          │ 1:N (Relacionamento 1-para-Muitos)
          │
          ▼
┌─────────────────────────────────────────────────────────┐
│          PARTIDAS                                       │
├─────────────────────────────────────────────────────────┤
│ • Id (PK)                                               │
│ • MatchId                                               │
│ • Puuid / GameName / TagLine                            │
│ • Mapa / Modo / Agente                                  │
│ • Resultado / Kills / Deaths / Assists / KDA            │
│ • Score / DataPartida                                   │
│ • DeletedAt (Soft Delete)                               │
│ • UsuarioId (FK) ────────────────────────┐              │
└─────────┬───────────────────────────────────┼──────────┘
          │                                   │
          │ 1:1 (Relacionamento 1-para-1)     │
          │                       Muitos para 1
          ▼                        │
┌─────────────────────────────────┴──────────────────┐
│          PARTIDAESTATISTICA                        │
├─────────────────────────────────────────────────────┤
│ • Id (PK)                                           │
│ • PartidaId (FK, UNIQUE) ◄─────────────┐ 1:1      │
│ • AcsScore / EconScore                             │
│ • RoundasGanhas / RoundasPerdidas                   │
│ • HeadshotPercentual / BodyShotCount / LegShotCount│
│ • ArmaFavorita / DanosCausados / DanosRecebidos   │
│ • CriadoEm                                         │
└────────────────────────────────────────────────────┘
```

**Relacionamentos:**

- 1:N **Usuario → Partidas**: Cada usuário pode ter múltiplas partidas. Deletar usuário define `UsuarioId` como NULL (orphan).
- 1:1 **Partida ↔ PartidaEstatistica**: Cada partida tem exatamente uma estatística. Deletar partida deleta a estatística (Cascade).

=======
>>>>>>> 20192b2deae474d83a4a38641ceee79ef790e285
---

## 🛣️ Documentação da API (Endpoints)

A API do projeto está devidamente versionada sob o prefixo `/api/v1/`. Abaixo está a especificação completa das rotas disponíveis:

### 🔐 Autenticação (`/api/v1/Auth`)

| Método | Rota | Descrição | Estrutura do Payload (JSON) |
| :--- | :--- | :--- | :--- |
| `POST` | `/registrar` | Cria uma nova conta de usuário no sistema. | `{ "email", "senha", "gameName", "tagLine" }` |
| `POST` | `/login` | Autentica o usuário e concede acesso ao sistema. | `{ "email", "senha" }` |
| `PUT` | `/perfil` | Atualiza as informações do perfil, senha ou nick associado. | `{ "emailAtual", "senhaAtual", "novoEmail"?, "novaSenha"?, "gameName"?, "tagLine"? }` |

### 🎮 Jogadores (`/api/v1/Jogadores`)

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/temporada` | Retorna informações atualizadas sobre a Season e Act vigente no ecossistema do Valorant. |
| `GET` | `/{gameName}/{tagLine}` | Retorna o perfil resumido do jogador. Se a última consulta no banco for superior a 15 minutos, uma nova sincronização externa é feita automaticamente. Calcula dinamicamente o KDA, Taxa de Vitória, Melhor Mapa e Agente. |
| `PUT` | `/{gameName}/{tagLine}` | Força manualmente a reatualização do cache do jogador e importa novas partidas inéditas da API externa. |
| `GET` | `/{gameName}/{tagLine}/historico` | Retorna a lista paginada do histórico de partidas do jogador. Suporta Query Params: `page` (padrão 1) e `size` (padrão 10). |
| `GET` | `/{gameName}/{tagLine}/estatisticas` | Consolida e agrupa de forma decrescente toda a performance do jogador separada por tabelas de: Por Mapa, Por Agente e Por Modo de Jogo. |

### 📝 Partidas (`/api/v1/Partidas`)

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/` | Lista todas as partidas gerais persistidas no banco com suporte à paginação (`page`, `size`). |
| `GET` | `/{id}` | Recupera os detalhes completos de uma partida específica através do seu ID identificador. |
| `POST` | `/` | Registra manualmente uma nova partida no banco de dados. |
| `PUT` | `/{id}` | Atualiza propriedades de uma partida existente. O KDA é recalculado automaticamente no backend caso o número de mortes seja igual a zero. |
| `DELETE`| `/{id}` | Executa a deleção lógica da partida (*Soft Delete*), definindo a flag temporal `DeletedAt` sem expurgar o dado permanentemente. |

### 📢 Status do Jogo (`/api/v1/Status`)

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/agentes-desativados` | Consulta e retorna a lista de agentes que se encontram desativados ou indisponíveis no momento no Valorant. |

---

## 🗄️ Detalhes de Implementação Backend

### Mecanismo de Cache de Partidas

Para mitigar o excesso de requisições externas, o método de busca de partidas analisa a existência prévia de registros usando um `HashSet` baseado no campo `MatchId`. Dessa forma, somente partidas que ainda não constam no banco local são inseridas em lote (`AddRange`), otimizando o processamento:

```csharp
var matchIdsExist = _context.Partidas.Select(p => p.MatchId).ToHashSet();
var partidasNovas = partidas.Where(p => string.IsNullOrEmpty(p.MatchId) || !matchIdsExist.Contains(p.MatchId)).ToList();
```

## Soft Delete de Registros

A remoção de registros em `/api/v1/Partidas` não destrói a informação fisicamente. O sistema utiliza a abordagem de Soft Delete, permitindo auditorias e recuperação de dados futuras:

```
C#
partida.DeletedAt = DateTime.UtcNow; // ← Soft Delete
await _context.SaveChangesAsync();
```

## ⚙️ Como Executar o Projeto

Pré-requisitos
Certifique-se de possuir instalado em seu ambiente:

Git <https://git-scm.com/>

Node.js <https://nodejs.org/>

.NET SDK 8.0 <https://dotnet.microsoft.com/download>

PostgreSQL <https://www.postgresql.org/>

### 1. Clonando o Repositório

```
git clone [https://github.com/SEU-USUARIO/CyphersAnalytics.git](https://github.com/SEU-USUARIO/CyphersAnalytics.git)
cd CyphersAnalyticsFULL
```

### 2. Configurando e Rodando o Backend (.NET)

Certifique-se de configurar a sua Connection String do PostgreSQL no arquivo `src/api/appsettings.json` ou `appsettings.Development.json` antes de iniciar.

```

# Acesse a pasta do backend
cd src/api

# Restaure as dependências do NuGet
dotnet restore

# Execute o projeto
dotnet run
```

A API estará disponível por padrão nos endereços locais: `http://localhost:5000` ou `https://localhost:5001`.

### 3. Configurando e Rodando o Frontend (React + Vite)

```

# Abra um novo terminal, retorne à raiz e acesse o frontend
cd src/frontend

# Instale as dependências do ecossistema Node
npm install

# Inicie o servidor de desenvolvimento local
npm run dev
```

O servidor do frontend será inicializado e estará acessível em: `http://localhost:5173`.

## 📂 Estrutura Simplificada de Pastas

```
CyphersAnalytics/
├── src/
│   ├── api/                 # Código-fonte do Backend (.NET Web API)
│   │   ├── Controllers/     # Controladores da API (Auth, Jogadores, Partidas, Status)
│   │   ├── database/        # Contexto de dados do EF Core (AppDbContext)
│   │   ├── models/          # Entidades de Domínio (Usuario, Jogador, Partida)
│   │   └── services/        # Serviços e integrações externas (HenrikService, AuthService)
│   └── frontend/            # Código-fonte do Frontend (React + Vite)
│       ├── src/
│       │   ├── assets/      # Recursos visuais estáticos (Imagens, Ranks, Ícones)
│       │   ├── components/  # Componentes encapsulados reutilizáveis
│       │   ├── pages/       # Telas/Views principais da aplicação (Dashboard, Login)
│       │   └── services/    # Módulos de comunicação e requisições HTTP (Axios)
└── README.md                # Documentação do sistema
```

## ✍ Autoria

Desenvolvido com dedicação por Karolini R. Pedrozo.
