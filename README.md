# ReconHub

Monitors integrations between enterprise systems (WMS, ERP, carriers). Systems report events to ReconHub; it checks
that every business transaction completed correctly in every system and raises exceptions for the support team.

## Prerequisites

- .NET SDK 10.0.300+ (pinned in `global.json`)
- Node.js 22+
- Docker (local SQL Server and the integration tests)

## Run locally

```bash
docker compose up -d                       # SQL Server 2022 on localhost:1433
dotnet tool restore                        # dotnet-ef (local tool)
dotnet ef database update --project src/ReconHub.Infrastructure --startup-project src/ReconHub.Api
dotnet run --project src/ReconHub.Api --launch-profile http     # http://localhost:5080
cd src/reconhub-web && npm install && npm run dev               # http://localhost:5173
```

In Development the API seeds a demo tenant (`0195f0c0-0000-7000-8000-000000000001`), WMS and ERP source systems,
and the flow "ShipConfirm -> DeliveryPosted within 15 minutes, quantity must match".

| Caller | Credential (Development) |
| --- | --- |
| WMS | `X-Api-Key: dev-wms-api-key` |
| ERP | `X-Api-Key: dev-erp-api-key` |
| Support operator | JWT from `dotnet user-jwts create --project src/ReconHub.Api --name support.agent --claim tenant_id=0195f0c0-0000-7000-8000-000000000001` |

OpenAPI document: <http://localhost:5080/openapi/v1.json> (Development only).

## Test

```bash
dotnet test --solution ReconHub.slnx                         # unit + integration (integration needs Docker)
cd src/reconhub-web && npm test && npm run lint && npm run typecheck
```

## API contract and frontend types

Building `ReconHub.Api` regenerates `contracts/openapi/reconhub-api.json`. After changing an endpoint, run
`npm run api:generate` in `src/reconhub-web` to refresh `src/api/schema.gen.ts`. The web app calls the API only through
`src/api/client.ts`; ESLint rejects direct `fetch` and `openapi-fetch` use elsewhere.

## Layout

| Path | Purpose |
| --- | --- |
| `src/ReconHub.Domain` | Entities, value objects, pure detection rules. No dependencies. |
| `src/ReconHub.Application` | Use cases, repository interfaces, DTOs, validation. |
| `src/ReconHub.Infrastructure` | EF Core (SQL Server), tenant isolation, repositories, detection worker, dev seeding. |
| `src/ReconHub.Api` | Minimal API endpoints, authentication, composition root. |
| `src/reconhub-web` | React + TypeScript (Vite) web app. |
| `tests/` | xUnit unit tests and Testcontainers-based integration tests. |
