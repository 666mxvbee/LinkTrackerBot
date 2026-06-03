# LinkTracker

[![.NET](https://github.com/666mxvbee/LinkTracker/actions/workflows/dotnet.yml/badge.svg)](https://github.com/666mxvbee/LinkTracker/actions/workflows/dotnet.yml)
[![Docker Image CI](https://github.com/666mxvbee/LinkTracker/actions/workflows/docker-image.yml/badge.svg)](https://github.com/666mxvbee/LinkTracker/actions/workflows/docker-image.yml)
[![License](https://img.shields.io/github/license/666mxvbee/LinkTracker)](LICENSE)
[![Release](https://img.shields.io/github/v/release/666mxvbee/LinkTracker?include_prereleases&sort=semver)](https://github.com/666mxvbee/LinkTracker/releases)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](docker-compose.yml)
[![Tests](https://img.shields.io/badge/tests-xUnit%20%2B%20Testcontainers-5A2D82)](tests/LinkTracker.Scrapper.Tests)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20Docker-lightgrey)](docker-compose.yml)

LinkTracker is a .NET 9 microservice application for tracking GitHub repositories and StackOverflow questions. The Bot service handles Telegram interaction, and the Scrapper service stores subscriptions, checks links on a schedule, and sends update notifications back to the Bot.

## Project Structure

```text
src/LinkTracker.Bot       Telegram bot HTTP service
src/LinkTracker.Scrapper  Subscription storage and scheduled update checker
src/LinkTracker.Shared    Shared DTOs
migrations/               SQL migrations applied by Scrapper on startup
```

## Prerequisites

- .NET 9 SDK
- Docker Desktop
- Telegram bot token from BotFather

## Configuration

Create `.env` in the repository root:

```env
TELEGRAM_BOT_TOKEN=your-telegram-bot-token

POSTGRES_DB=linktracker
POSTGRES_USER=linktracker
POSTGRES_PASSWORD=linktracker
```

`.env` is ignored by git. Use `.env.example` as the template.

Scrapper database settings are in `src/LinkTracker.Scrapper/appsettings.json`:

```json
"Database": {
  "AccessType": "SQL",
  "ConnectionString": "Host=localhost;Port=5433;Database=linktracker;Username=linktracker;Password=linktracker",
  "RunMigrations": true
}
```

`AccessType` can be:

```text
SQL  raw SQL repositories via Npgsql
ORM  EF Core repositories
```

Scheduler settings:

```json
"Scrapper": {
  "CheckIntervalSeconds": 30,
  "BatchSize": 100,
  "Parallelism": 4,
  "GitHubBaseUrl": "https://api.github.com/",
  "StackOverflowBaseUrl": "https://api.stackexchange.com/2.3/"
}
```

`BatchSize` is clamped by the application to `50..500`. `Parallelism` controls how many links are processed concurrently.

## Run With Docker Compose

Run all services:

```powershell
docker compose up --build
```

Endpoints:

```text
Bot API:       http://localhost:5100
Scrapper API: http://localhost:5000
PostgreSQL:   localhost:5433
```

Inside Docker, Scrapper connects to PostgreSQL by service name:

```text
Host=postgres;Port=5432
```

## Run From IDE

Start PostgreSQL first:

```powershell
docker compose up postgres -d
```

Then run the services from IDE or terminal:

```powershell
dotnet run --project src\LinkTracker.Scrapper
dotnet run --project src\LinkTracker.Bot
```

Scrapper applies SQL migrations from `migrations/` automatically when `Database:RunMigrations` is `true`.

## Useful Manual Checks

List database tables:

```powershell
docker exec -e PGPASSWORD=linktracker linktracker-postgres psql -U linktracker -d linktracker -c "\dt"
```

Expected domain tables:

```text
chats
links
chat_links
tags
chat_link_tags
```

DbUp also creates:

```text
schemaversions
```

Open Scrapper Swagger:

```text
http://localhost:5000/swagger
```

Basic API flow:

```text
POST   /tg-chat/{id}
POST   /links      with Tg-Chat-Id header
GET    /links      with Tg-Chat-Id header
DELETE /links      with Tg-Chat-Id header
GET    /tags
POST   /tags
PUT    /tags/{id}
DELETE /tags/{id}
```

## Update Checking

Scrapper uses Quartz to periodically process tracked links in batches.

For GitHub links, it detects new:

```text
Issue
Pull request
```

For StackOverflow links, it detects new:

```text
Answer
Question comment
Answer comment
```

Notifications include:

```text
type of update
title
user name
creation time
text preview limited to 200 characters
```

The notification sender is abstracted behind `IMessageSender`. The current implementation is HTTP from Scrapper to Bot.
