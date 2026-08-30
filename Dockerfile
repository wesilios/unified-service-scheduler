# syntax=docker/dockerfile:1

# --- Build stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `restore` is cached independently of source changes.
COPY UnifiedSeviceScheduler.sln .
COPY src/Scheduler.Domain/Scheduler.Domain.csproj src/Scheduler.Domain/
COPY src/Scheduler.Application/Scheduler.Application.csproj src/Scheduler.Application/
COPY src/Scheduler.Infrastructure/Scheduler.Infrastructure.csproj src/Scheduler.Infrastructure/
COPY src/Scheduler.Api/Scheduler.Api.csproj src/Scheduler.Api/
COPY tests/Scheduler.UnitTests/Scheduler.UnitTests.csproj tests/Scheduler.UnitTests/
COPY tests/Scheduler.IntegrationTests/Scheduler.IntegrationTests.csproj tests/Scheduler.IntegrationTests/

RUN dotnet restore src/Scheduler.Api/Scheduler.Api.csproj

COPY . .

RUN dotnet publish src/Scheduler.Api/Scheduler.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Non-root by default. /app/data is where the SQLite file for this assessment's demo
# lives — mount a volume there to persist it across container restarts. For production,
# override ConnectionStrings__SchedulerDb with a SQL Server connection string instead
# (see README.md's Deployment section) — no rebuild needed, just an env var.
RUN useradd --uid 5678 --user-group --no-create-home appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__SchedulerDb="Data Source=/app/data/scheduler.db"
EXPOSE 8080
VOLUME /app/data

ENTRYPOINT ["dotnet", "Scheduler.Api.dll"]
