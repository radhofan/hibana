# Project 3: High-Performance IoT Telemetry Hub

.NET 10 Clean Architecture backend + React frontend for ingesting and visualizing IoT device telemetry in real-time, with multi-agent AI orchestration and a custom CLI tool.

## Architecture

```
Frontend (React/Vite :5173)
    └─> REST API  (ASP.NET Core :5000)
    └─> SignalR   (/hubs/telemetry) — live push
    
IoT Devices ─> gRPC (:5001, HTTP/2) — high-throughput ingest

Backend layers:
  IoTHub.Api          → gRPC service, SignalR hub, REST controllers
  IoTHub.Application  → CQRS handlers (MediatR), interfaces
  IoTHub.Infrastructure → SQL Server (EF Core), Redis, RabbitMQ
  IoTHub.Domain       → Entities (Device, TelemetryReading, Alert)

AI Orchestration:
  Ollama (local LLM) + Microsoft Semantic Kernel
    ├─> Planner Agent  — detects failing IoT nodes, triggers review
    └─> Reviewer Agent — critiques failure logs for performance bottlenecks

Infra:
  SQL Server :1433  — write-side (telemetry, devices, alerts)
  Redis :6379       — read-side cache (device list, aggregations)
  RabbitMQ :5672    — alert-queue (threshold breach processing)
  Ollama :11434     — local LLM inference (multi-agent orchestration)
  Prometheus :9090  — metrics scraping
  Grafana :3001     — dashboards (admin/admin)
```

## Data Flow

```
gRPC IngestReading
  → IngestTelemetryCommand (MediatR)
  → SQL Server write (TelemetryReading)
  → Device status updated
  → SignalR broadcast (TelemetryReceived, DeviceStatusChanged)
  → If value >= threshold → RabbitMQ "alert-queue"
                              → AlertProcessorWorker (BackgroundService)
                              → Alert persisted to SQL Server
                              → SignalR broadcast (AlertTriggered)
                              → AI Planner Agent evaluates node health
                                    → Reviewer Agent critiques failure logs
```

## Quick Start

### 1. Start infrastructure

```bash
docker compose up -d
```

Starts: SQL Server, Redis, RabbitMQ, Ollama, Prometheus, Grafana.

### 2. Pull local LLM model

```bash
docker exec -it ollama ollama pull llama3
```

### 3. Start backend

```bash
cd backend
dotnet restore
dotnet run --project src/IoTHub.Api
```

- REST API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- gRPC:    https://localhost:5001
- Metrics: http://localhost:5000/metrics

### 4. Start frontend

```bash
npm install
npm run dev
```

Frontend: http://localhost:5173

### 5. Use the CLI tool

```bash
cd cli
dotnet run -- diff --summarize <path-to-diff-file>
```

The `iotdiff` CLI uses a local Ollama model to analyze and summarize complex system diffs.

## API Reference

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/devices` | List all devices |
| POST | `/api/devices` | Register new device |
| PUT | `/api/devices/{id}/threshold` | Update alert threshold |
| GET | `/api/devices/{id}/telemetry?hours=24` | Device telemetry history |
| POST | `/api/telemetry/ingest` | REST telemetry ingest (gRPC preferred) |
| GET | `/api/alerts?page=0&size=20` | Paged alert list |
| POST | `/api/alerts/{id}/acknowledge` | Acknowledge alert |

### Register device example
```json
POST /api/devices
{
  "hardwareId": "METER-001",
  "name": "Smart Meter NYC",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "alertThreshold": 85.0,
  "unit": "°C"
}
```

### Ingest telemetry (REST)
```json
POST /api/telemetry/ingest
{
  "hardwareId": "METER-001",
  "value": 72.5,
  "unit": "°C"
}
```

## gRPC Ingest (proto)

```protobuf
service TelemetryService {
  rpc IngestReading (TelemetryRequest) returns (TelemetryResponse);
  rpc IngestStream (stream TelemetryRequest) returns (IngestStreamResponse);
}
```

Use `IngestStream` for high-throughput device simulation (thousands of pings/sec).

## SignalR Events (frontend subscribes)

| Event | Payload |
|-------|---------|
| `TelemetryReceived` | `{ deviceId, deviceName, value, unit, timestamp }` |
| `AlertTriggered` | `{ deviceId, deviceName, message, severity, triggerValue, threshold }` |
| `DeviceStatusChanged` | `{ deviceId, status, lastSeen }` |

## Multi-Agent AI Orchestration

Built with **Microsoft Semantic Kernel** and a local **Ollama** instance (100% free, no external API calls):

- **Planner Agent**: Monitors node health metrics in real-time. When a node's failure rate or latency exceeds thresholds, the planner agent flags it and assembles a diagnostic context.
- **Reviewer Agent**: Receives the failure context from the planner and systematically critiques the failure logs, identifying performance bottlenecks and suggesting remediation steps.

Agent activity is surfaced in the Alert Feed UI and persisted as structured audit entries.

## CLI Tool — `iotdiff`

A custom .NET CLI tool that uses a local Ollama model to automatically analyze and summarize complex system diffs:

```bash
# Summarize a git diff
iotdiff diff --summarize ./changes.patch

# Analyze a deployment diff between two config snapshots
iotdiff diff --compare config-before.json config-after.json
```

The tool streams the LLM summary to stdout, making it easy to pipe into CI reports or deployment logs.

## UI Pages

- **Live Map** — Leaflet map, colored markers per device status, live updates
- **Analytics** — Device selector, real-time AreaChart (Recharts), threshold line overlay
- **Devices** — Register devices, inline threshold editing
- **Alerts** — Real-time feed with severity badges, acknowledge button, pagination, agent analysis panel

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core, MediatR, EF Core 9
- **Architecture**: Clean Architecture with MediatR (CQRS)
- **Databases**: SQL Server 2022 (write), Redis 7 (cache)
- **Messaging**: RabbitMQ 3.13
- **Real-time**: ASP.NET Core SignalR
- **gRPC**: Grpc.AspNetCore
- **AI Orchestration**: Local Ollama instance (100% free) + Microsoft Semantic Kernel (multi-agent planner/reviewer)
- **CLI Tool**: .NET 10 CLI with Ollama integration (AI-powered diff summarization)
- **Frontend**: React 18, Vite, Tailwind CSS, React-Leaflet, Recharts, @microsoft/signalr
- **Maps**: OpenStreetMap via Leaflet (free) or Mapbox (Free Tier)
- **Error Tracking**: Sentry (Developer Free Plan) or GlitchTip
- **Observability**: Prometheus + Grafana, prometheus-net
- **Infrastructure**: Docker (SQL Server on Linux, RabbitMQ, Ollama)
