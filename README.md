# testfinals-backend: LogiPulse Logistics Cloud API

Enterprise .NET 10 minimal Web API backend for the **LogiPulse Logistics Cloud**, managed with **AlphaCI Enterprise** CI/CD pipeline automation and deployed to **Render**.

## Features & Endpoints

- **Contract & Health Probes**:
  - `GET /health`: Health probe validated by AlphaCI production gate (HTTP 200 `{ "status": "ok", "service": "testfinals-backend" }`).
  - `GET /`: Service readiness check.
- **Authentication**:
  - `POST /api/v1/auth/login`: Authenticates dispatcher/admin/driver profiles with demo credentials.
  - `GET /api/v1/auth/me`: Retrieves current session user info from Bearer token.
- **Shipments & Waybill Telematics**:
  - `GET /api/v1/shipments`: Filterable by status and searchable by tracking number/route.
  - `GET /api/v1/shipments/{id}`: Detailed shipment dossier with full checkpoint event timeline.
  - `GET /api/v1/shipments/track/{trackingNumber}`: Public lookup endpoint.
  - `POST /api/v1/shipments`: Waybill issuance and tracking registration.
  - `PATCH /api/v1/shipments/{id}/status`: Checkpoint telemetry update.
  - `POST /api/v1/shipments/{id}/simulate`: Advances shipment state to next milestone.
- **Fleet & Vehicle Telematics**:
  - `GET /api/v1/fleet/vehicles`: Fleet status, battery/fuel reserve, capacity, GPS waypoint.
  - `GET /api/v1/fleet/drivers`: Driver roster, license classification, ratings.
  - `POST /api/v1/fleet/dispatch`: Assigns driver and vehicle to shipment route.
- **Warehouses & Inventory**:
  - `GET /api/v1/warehouses`: Multi-hub capacity and occupancy tracking (Chicago, Rotterdam, Dallas, Singapore, Frankfurt).
  - `GET /api/v1/inventory`: Warehouse SKU stock monitoring and reorder alerts.
- **Analytics & SLA**:
  - `GET /api/v1/analytics/overview`: Real-time KPI summary (on-time rate, active cargo, delayed alerts).

## Render Environment Variables

Configure these in **Render Dashboard** -> Service -> **Environment**:

| Variable | Recommended Value | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` (or `Staging` on UAT) | Runtime environment mode |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` | Required for Docker container listening on port 8080 |
| `PORT` | `8080` | Port configuration |
| `CORS_ORIGINS` | `https://*.vercel.app,https://testfinals-frontend.vercel.app,http://localhost:3000` | Allowed frontend origins for CORS |

## Branch Strategy & Promotion Workflow

Always create your working branch from `dev`:
```bash
git checkout dev
git pull origin dev
git checkout -b feat/your-feature
```

Open a pull request into `dev`. Once merged:
1. `dev`: AlphaCI runs quality checks and triggers `promote-to-uat`.
2. `uat`: Builds Docker container, deploys to Render UAT slot, and triggers post-deploy verification.
3. `main`: Once UAT is verified green, promotes to `main` and deploys to Production.

## Local Development & Testing

```bash
dotnet restore
dotnet format --verify-no-changes
dotnet build --configuration Release
dotnet test
```