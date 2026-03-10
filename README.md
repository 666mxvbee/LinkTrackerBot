# LinkTracker Bot

LinkTracker is a microservice-based system that monitors GitHub repositories and StackOverflow questions, sending real-time update notifications via Telegram.

## Project Structure

    src/LinkTracker.Bot - ASP.NET Core Web API for Telegram interaction.

    src/LinkTracker.Scrapper - Quartz-based worker for resource monitoring.

    src/LinkTracker.Shared - Common models and DTOs shared between services.

## Quick Start (Docker)

The easiest way to run the entire infrastructure is using Docker Compose.

### 1. Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.
* A Telegram Bot Token from [@BotFather](https://t.me/botfather).

### 2. Configuration
Create a file named `.env` in the root directory of the project:

```env
TELEGRAM_BOT_TOKEN=your_token_here
```
Note: The .env file is ignored by git for security purposes.

### 3. Launch

Run the following command in the root folder:
Bash

docker-compose up --build

Once started:

    Bot API: http://localhost:5100

    Scrapper API: http://localhost:5000