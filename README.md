# 🔍 LoanLens — Intelligent Financial Advisory & Loan Decisioning Engine

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4.svg?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120.svg?style=for-the-badge&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-4169e1.svg?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-Distributed%20Cache-dc382d.svg?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg?style=for-the-badge)](#-architecture--solution-design)
[![Tests](https://img.shields.io/badge/Unit%20Tests-41%2F41%20Passing-success.svg?style=for-the-badge)](#-testing--quality-assurance)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)

**LoanLens** is a high-performance, enterprise-grade financial engineering and advisory engine tailored for the Egyptian retail banking and consumer finance ecosystem. It empowers consumers, loan officers, and fintech platforms to assess loan affordability, simulate economic shocks, run stochastic default simulations, and generate personalized advisory narratives powered by LLMs (Google Gemini & Groq).

---

## 📑 Table of Contents

- [Executive Overview](#-executive-overview)
- [Architecture & Solution Design](#-architecture--solution-design)
- [Core Mathematical & Financial Engines](#-core-mathematical--financial-engines)
  - [1. Amortization & Effective APR Engine](#1-amortization--effective-apr-engine)
  - [2. Regulatory Affordability & DTI Compliance](#2-regulatory-affordability--dti-compliance)
  - [3. Health Insurance Multi-Engine Integration](#3-health-insurance-multi-engine-integration)
  - [4. Deterministic Stress Testing (Crisis Simulation)](#4-deterministic-stress-testing-crisis-simulation)
  - [5. Stochastic Monte Carlo Default Simulation](#5-stochastic-monte-carlo-default-simulation)
  - [6. 0–100 Composite Risk Scoring & Decision Engine](#6-0100-composite-risk-scoring--decision-engine)
  - [7. Borrowing Capacity & Retirement Ceiling Engine](#7-borrowing-capacity--retirement-ceiling-engine)
  - [8. Debt Consolidation & Buyout Engine](#8-debt-consolidation--buyout-engine)
  - [9. CBE Corridor Sensitivity Engine](#9-cbe-corridor-sensitivity-engine)
  - [10. Live Egyptian Banking Market Rates & CBE Corridor](#10-live-egyptian-banking-market-rates--cbe-corridor)
  - [11. AI Narrative Advisory Engine](#11-ai-narrative-advisory-engine)
  - [12. Document & Spreadsheet Generation](#12-document--spreadsheet-generation)
- [RESTful API Endpoints](#-restful-api-endpoints)
- [Security & Production Hardening](#-security--production-hardening)
- [Getting Started & Local Setup](#-getting-started--local-setup)
- [Docker & Container Deployment](#-docker--container-deployment)
- [Testing & Quality Assurance](#-testing--quality-assurance)
- [CI/CD Pipeline](#-cicd-pipeline)

---

## 🌟 Executive Overview

In volatile macroeconomic environments, conventional credit scoring fails to reflect real borrower resilience. LoanLens solves this by combining:
1. **Egyptian Central Bank (CBE) Regulations:** Strict compliance with the 50% legal Debt-To-Income (DTI) ceiling and 30%–35% safe recommended thresholds.
2. **Deterministic Stress Shocks:** Testing liquidity survival against unemployment, 25% salary slashes, double-digit inflation, and emergency hospital bills.
3. **Stochastic Monte Carlo Simulations:** Executing 5,000 to 50,000 multi-threaded iterations using Box-Muller transforms to model probability of default and cash-flow depletion.
4. **Live Egyptian Banking Rates Feed:** Continuous benchmark tracking of CBE corridor rates (Overnight Lending, Overnight Deposit, Main Operation) alongside NBE, Banque Misr, CIB, and QNB loan products.
5. **Resilient Redis Distributed Caching:** Sub-millisecond cached catalog queries with automatic in-memory fallback.
6. **Enterprise Identity & RBAC:** JWT authentication supporting isolated user workspaces and administrative oversight.
7. **Autonomous AI Advisory:** Synthesizing bilingual (Arabic & English) advisory briefings with concrete step-by-step financial roadmaps.

---

## 🏛 Architecture & Solution Design

The solution strictly adheres to **Clean Architecture** and follows the **Single-Type-Per-File** convention across all layers. Business rules remain independent of UI, databases, and third-party frameworks.

```
LoanLens/
├── src/
│   ├── LoanLens.Domain/                 # Core business domain (Zero external dependencies)
│   │   ├── Enums/                       # Strongly typed enums (UserRole, InterestCalculationMethod, etc.)
│   │   ├── ValueObjects/                # Immutable domain models & financial calculation results
│   │   ├── Services/                    # Pure domain engines (FinancialCalculator, StressTesting, etc.)
│   │   └── Localization/                # Language constants and automatic Arabic/English detection
│   │
│   ├── LoanLens.Application/            # Application orchestration layer
│   │   ├── DTOs/                        # Request and response contracts (1 file per record)
│   │   ├── Interfaces/                  # Abstractions for persistence, auth, cache, market rates, AI, PDF
│   │   ├── Services/                    # LoanAdvisorService (Core use case pipeline)
│   │   └── Validation/                  # Fluent domain validation with bilingual error messages
│   │
│   ├── LoanLens.Infrastructure/         # External integrations, caching & persistence
│   │   ├── Configuration/               # Options pattern bindings (JwtOptions, RedisOptions, AiAdvisorOptions)
│   │   ├── Entities/                    # EF Core entities (UserEntity, SavedScenarioEntity)
│   │   ├── Persistence/                 # LoanLensDbContext & PostgreSQL EF Core Migrations
│   │   ├── Repositories/                # ScenarioRepository implementation with user isolation
│   │   └── Services/                    # AuthService, JwtTokenGenerator, RedisCacheService, BankRatesService
│   │
│   └── LoanLens.Api/                    # Presentation layer
│       ├── Controllers/                 # RESTful Controllers (Loans, Auth, Admin, MarketRates, Scenarios)
│       ├── Middleware/                  # CorrelationIdMiddleware, Exception Handler, Rate Limiting
│       ├── Health/                      # PostgreSQL and Redis HealthChecks & readiness probes
│       └── Program.cs                   # Clean composition root with JWT, CORS & Scalar documentation
│
├── tests/
│   └── LoanLens.Domain.Tests/           # 41 xUnit unit tests verifying mathematical precision and security
├── .github/workflows/ci.yml             # GitHub Actions CI build, test, and coverage pipeline
├── Dockerfile                           # Multi-stage container build (.NET 10 SDK & ASP.NET runtime)
└── docker-compose.yml                   # Container orchestration (PostgreSQL + Redis + LoanLens.Api)
```

---

## ⚙️ Core Mathematical & Financial Engines

### 1. Amortization & Effective APR Engine
- **Reducing Balance Method:** Computed via the standard financial annuity formula:
  $$\text{PMT} = P \times \frac{r(1+r)^n}{(1+r)^n - 1}$$
  where $P$ is principal, $r$ is monthly interest rate ($\text{Annual Rate} / 12$), and $n$ is tenor in months.
- **Flat Rate Conversion:** Accurately converts misleading Flat interest rates into true Effective Annual Rates (EAR / APR) using Newton-Raphson approximation, revealing the true cost of credit to consumers.
- **Administrative Fees & Stamp Duties:** Deducts bank upfront processing charges (e.g., 1.5% - 2.0%) from net loan proceeds and amortizes them into true annual APR.

### 2. Regulatory Affordability & DTI Compliance
- **Debt-To-Income (DTI) Ratio:**
  $$\text{DTI} = \frac{\text{Current Monthly Installments} + \text{Proposed Installment}}{\text{Net Monthly Income}} \times 100$$
- **CBE 50% Legal Cap:** Hard ceiling enforced by the Central Bank of Egypt; applications exceeding 50% are automatically flagged as `Critical` / Non-Compliant.
- **Emergency Runway Calculation:**
  $$\text{Runway (Months)} = \frac{\text{Liquid Savings}}{\text{Fixed Living Expenses} + \text{Total Debt Obligations}}$$
  Flags vulnerable profiles when runway falls below 3.0 months.

### 3. Health Insurance Multi-Engine Integration
Health insurance coverage (`HasHealthInsurance`) directly impacts 5 engines:
- **Borrowing Capacity:** Safe DTI ceiling is dynamically set to **35%** for insured borrowers, but lowered to **30%** for uninsured borrowers to ensure cash is reserved for medical contingencies.
- **Stress Testing:** Medical emergency shock is modeled as an out-of-pocket co-pay (**7,500 – 10,000 EGP**) for insured clients, versus full private hospital bill (**25,000 – 38,000 EGP**) for uninsured clients.
- **Combined Crisis:** Shock outflow drops from 22,000 EGP (uninsured) to 8,000 EGP (insured).
- **Risk Scoring:** An uninsured borrower receives an automatic penalty of **+2 to +4 risk points** (scaled by number of dependents).
- **Monte Carlo Simulations:** Uninsured profiles experience higher stochastic shock probabilities and 3× higher outflow costs (up to 55,000 EGP).

### 4. Deterministic Stress Testing (Crisis Simulation)
Evaluates profile survivability across 7 macroeconomic shocks:
1. **Medical Emergency:** Co-pay for insured vs. full out-of-pocket for uninsured.
2. **Job Interruption (1 Month):** 100% loss of income for 30 days.
3. **Job Interruption (3 Months):** Extended unemployment crisis.
4. **Job Interruption (6 Months):** Severe structural unemployment.
5. **Income Reduction (-25%):** Permanent salary reduction.
6. **Inflation Surge (+20%) & Rate Surge (+300 bps):** Living cost explosion and corridor hike for variable-rate loans.
7. **Combined Crisis:** 20% income reduction + 15% inflation + unexpected emergency shock.

### 5. Stochastic Monte Carlo Default Simulation
- Implements the **Box-Muller Transform** to generate independent standard normal variates:
  $$Z = \sqrt{-2 \ln U_1} \cos(2\pi U_2)$$
- Simulates 5,000 to 50,000 monthly income and expenditure lifepaths in parallel using `Parallel.For`.
- Outputs **Default Probability %**, **Peak Risk Month**, and the **5th, 50th, and 95th Percentile Ending Savings**.

### 6. 0–100 Composite Risk Scoring & Decision Engine
Calculates an objective risk score from 0 (ultra-safe) to 100 (critical default risk):
- **Debt Burden (DTI) Component:** 0 to 30 points.
- **Emergency Runway Component:** 0 to 30 points.
- **Monte Carlo Default Risk Component:** 0 to 25 points.
- **Profile, Stability & Medical Risk:** 0 to 15 points.
- **Advisor Verdict:**
  - `0 - 35`: **Take Loan** (Sound, robust financial profile).
  - `36 - 65`: **Proceed with Caution** (Terms should be adjusted).
  - `66 - 100`: **Avoid Loan** (High vulnerability to default).

### 7. Borrowing Capacity & Retirement Ceiling Engine
- Calculates the maximum principal and installment allowed under both legal (50%) and recommended safe (30%–35%) ceilings.
- **Retirement Age Capping:** Automatically caps loan tenors if the applicant approaches statutory retirement age (60 years for salaried employees, 65 years for business owners).

### 8. Debt Consolidation & Buyout Engine
- Evaluates consolidating multiple high-interest debts (personal loans, credit card balances) into a single loan facility.
- Accounts for early settlement fees (typically 3%–5% in Egyptian banks) and computes total net interest saved and monthly disposable cashflow unlocked.

### 9. CBE Corridor Sensitivity Engine
- Evaluates the impact of benchmark interest rate shifts (+100 bps, +200 bps, +300 bps, -100 bps, -200 bps) on variable-rate loans, tracking the exact change in monthly installments.

### 10. Live Egyptian Banking Market Rates & CBE Corridor
- Tracks Central Bank of Egypt (CBE) corridor benchmarks:
  - **Overnight Lending Rate:** 28.25% (Corridor Ceiling)
  - **Overnight Deposit Rate:** 27.25% (Corridor Floor)
  - **Main Operation Rate:** 27.75%
- Catalogs retail lending schedules from top Egyptian commercial banks (NBE, Banque Misr, CIB, QNB Alahli, AAIB) across Personal Loans, Auto Loans, and Mortgage finance.
- Cached in Redis (`market:egyptian-bank-rates`) with 12-hour TTL and on-demand administrator cache busting.

### 11. AI Narrative Advisory Engine
- Integrates with **Google Gemini 3.5** and **Groq (OpenAI GPT-OSS / Llama / Allam)**.
- Features intelligent bilingual detection (auto-detects Arabic or English based on text inputs and headers).
- Synthesizes personalized executive summaries, key strengths, vulnerability warnings, and tailored action plans.

### 12. Document & Spreadsheet Generation
- **PDF Generation (`QuestPDF`):** Generates styled, print-ready PDF reports with color-coded risk gauges, stress testing breakdowns, and product comparisons.
- **Excel Generation (`ClosedXML`):** Exports comprehensive amortization schedules with summary KPIs, formatted currency columns, and principal/interest splits.

---

## 📡 RESTful API Endpoints

All endpoints support both `/api/v1/...` and `/api/...`.

### 1. Identity & Access Management (Authentication)
| Method | Endpoint | Access | Description |
|:---|:---|:---|:---|
| `POST` | `/api/v1/auth/register` | Public | Registers a new borrower user and issues a JWT token. First registered user is seeded as Admin. |
| `POST` | `/api/v1/auth/login` | Public | Authenticates via email & password and returns a JWT Bearer token with roles. |
| `GET`  | `/api/v1/auth/me` | Authenticated | Retrieves current authenticated user profile, role, and scenario statistics. |

### 2. Administrator Operations
| Method | Endpoint | Access | Description |
|:---|:---|:---|:---|
| `GET`  | `/api/v1/admin/dashboard` | Admin Only | Executive dashboard with platform analytics, active users, scenario volume, and CBE rates. |
| `GET`  | `/api/v1/admin/users` | Admin Only | Lists all registered users (supports `?search=&role=&isActive=`). |
| `GET`  | `/api/v1/admin/users/{id}` | Admin Only | Retrieves complete profile and scenario history for a specific user ID. |
| `PUT`  | `/api/v1/admin/users/{id}` | Admin Only | Updates user details, modifies roles (Admin/User), activates/deactivates, or resets passwords. |
| `DELETE`| `/api/v1/admin/users/{id}` | Admin Only | Deactivates or removes a user account and associated records. |
| `GET`  | `/api/v1/admin/scenarios` | Admin Only | Retrieves all financial scenarios created across all users in the system. |
| `DELETE`| `/api/v1/admin/scenarios/{id}` | Admin Only | Deletes any scenario across the system. |

### 3. Egyptian Banking Market Rates
| Method | Endpoint | Access | Description |
|:---|:---|:---|:---|
| `GET`  | `/api/v1/market-rates` | Public | Returns CBE corridor benchmarks, commercial bank interest rates, and loan category averages (Redis cached). |
| `POST` | `/api/v1/market-rates/refresh`| Admin Only | Force-refreshes the banking market rates feed and invalidates the Redis cache. |

### 4. Loan Calculations & Risk Advisory
| Method | Endpoint | Access | Description |
|:---|:---|:---|:---|
| `POST` | `/api/v1/loans/calculate` | Public | Calculates amortization schedule, monthly payment, and effective APR. |
| `POST` | `/api/v1/loans/affordability` | Public | Assesses DTI against CBE regulations and calculates emergency runway. |
| `POST` | `/api/v1/loans/stress-test` | Public | Simulates 7 deterministic macroeconomic stress scenarios. |
| `POST` | `/api/v1/loans/monte-carlo` | Public | Runs stochastic Monte Carlo default simulation (5,000+ paths). |
| `POST` | `/api/v1/loans/full-analysis` | Public | 360° diagnostic executing all engines, with optional AI narrative advice. |
| `GET`  | `/api/v1/loans/catalog` | Public | Retrieves catalog of Egyptian commercial banks and consumer finance products (Redis cached). |
| `POST` | `/api/v1/loans/borrowing-capacity`| Public | Calculates maximum borrowing capacity under legal and safe ceilings. |
| `POST` | `/api/v1/loans/debt-consolidation`| Public | Analyzes multi-loan buyout and debt consolidation opportunities. |
| `POST` | `/api/v1/loans/rate-sensitivity` | Public | Simulates CBE corridor rate hikes and cuts on variable rate loans. |
| `POST` | `/api/v1/loans/compare` | Public | Performs side-by-side comparative analysis between two arbitrary scenarios. |

### 5. Scenario Persistence & Management
| Method | Endpoint | Access | Description |
|:---|:---|:---|:---|
| `POST` | `/api/v1/loans/save-scenario` | Optional Auth | Evaluates and persists a loan scenario into PostgreSQL. Binds to user if authenticated. |
| `GET`  | `/api/v1/loans/scenarios` | Optional Auth | Lists saved scenarios. Regular users see only their own scenarios; Admins see all. |
| `GET`  | `/api/v1/loans/scenarios/{id}` | Optional Auth | Retrieves full diagnostic report for a saved scenario by GUID. |
| `DELETE`| `/api/v1/loans/scenarios/{id}` | Optional Auth | Deletes a scenario (enforces user ownership unless Admin). |
| `GET`  | `/api/v1/loans/scenarios/{id}/pdf` | Optional Auth | Downloads vector PDF report of a saved scenario. |
| `GET`  | `/api/v1/loans/scenarios/{id}/excel` | Optional Auth | Downloads Excel schedule (.xlsx) of a saved scenario. |
| `GET`  | `/api/v1/loans/scenarios/compare` | Optional Auth | Compares two saved scenarios by their IDs (`?id1=...&id2=...`). |
| `GET`  | `/api/v1/loans/scenarios/{id}/ai-advice` | Optional Auth | Generates on-demand AI advice or answers questions for a saved scenario. |

### 6. Reporting & Exports
| Method | Endpoint | Access | Description |
|:---|:---|:---|:---|
| `POST` | `/api/v1/loans/export-pdf` | Public | Generates and streams branded vector PDF report from request payload. |
| `POST` | `/api/v1/loans/export-excel` | Public | Generates and streams styled `.xlsx` amortization workbook. |
| `POST` | `/api/v1/loans/ai-advice` | Public | Generates standalone AI advisory narrative. |

### 7. Health & Diagnostics
| Method | Endpoint | Description |
|:---|:---|:---|
| `GET`  | `/healthz` | Comprehensive health check verifying PostgreSQL database and Redis cache connectivity. |
| `GET`  | `/ready` | Kubernetes/container readiness probe. |

---

## 🔒 Security & Production Hardening

- **Credential Isolation:** No plaintext API keys in configuration files. Secrets are managed via `.NET User Secrets` in development and Environment Variables in production.
- **Built-in Rate Limiting:** Enforces `FixedWindowRateLimiter` (100 requests / minute) to prevent Denial-of-Service and LLM quota exhaustion.
- **Correlation ID Tracking:** Every request is stamped with `X-Correlation-ID` for distributed tracing and structured log aggregation.
- **RFC 7807 ProblemDetails:** Uniform, machine-readable validation and error responses.
- **Configurable CORS:** Enforces whitelist origin filtering configurable in `appsettings.json`.

---

## 🚀 Getting Started & Local Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 16+](https://www.postgresql.org/) running locally on port `5432` (database: `loanlens_db`)
- [Redis 7+](https://redis.io/) running locally on port `6379`

### 1. Database & Cache Setup
```bash
# PostgreSQL database creation
psql -U postgres -c "CREATE DATABASE loanlens_db;"

# Verify Redis connection
redis-cli ping # Returns PONG
```

### 2. Clone & Build
```bash
git clone https://github.com/your-org/loanlens.git
cd loanlens
dotnet restore
dotnet build
```

### 3. Configure AI API Keys (Optional)
```bash
dotnet user-secrets set "AiAdvisor:GeminiApiKey" "YOUR_GEMINI_KEY" --project src/LoanLens.Api
dotnet user-secrets set "AiAdvisor:GroqApiKey" "YOUR_GROQ_KEY" --project src/LoanLens.Api
```

### 4. Run the Application
```bash
dotnet run --project src/LoanLens.Api
```
The API automatically applies pending EF Core PostgreSQL migrations and seeds the default administrator account (`admin@loanlens.com` / `Admin@123456`).

- **Interactive API Documentation (Scalar):** [http://localhost:5257/scalar/v1](http://localhost:5257/scalar/v1)
- **Health Check Endpoint:** [http://localhost:5257/healthz](http://localhost:5257/healthz)

---

## 🐳 Docker & Container Deployment

### Run with Docker Compose:
```bash
docker compose up -d --build
```
This builds and orchestrates the complete production-ready stack:
- **`loanlens_api`**: Multi-stage .NET 10 container running under an unprivileged `app` user.
- **`loanlens_postgres`**: PostgreSQL 16 database with persistent storage volume.
- **`loanlens_redis`**: Redis 7 cache with health probes.

---

## 🧪 Testing & Quality Assurance

The test suite contains **41 unit tests** verifying mathematical calculation precision, APR conversions, Monte Carlo convergence, CBE regulation caps, health insurance dynamics, JWT generation, password hashing, and Redis caching:

```bash
# Run test suite
dotnet test tests/LoanLens.Domain.Tests
```

---

## 🔄 CI/CD Pipeline

The project includes an automated GitHub Actions pipeline (`.github/workflows/ci.yml`) that executes on every push and pull request to `main` and `dev`:
- Restores dependencies
- Builds the solution in `Release` configuration
- Executes all 41 unit tests with XPlat code coverage collection

---

## 📄 License
This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
