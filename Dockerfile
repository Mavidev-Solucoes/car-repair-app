# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to leverage layer caching for restore
COPY CarRepairShop.slnx ./
COPY src/CarRepairShop.Domain/CarRepairShop.Domain.csproj src/CarRepairShop.Domain/
COPY src/CarRepairShop.Repository/CarRepairShop.Repository.csproj src/CarRepairShop.Repository/
COPY src/CarRepairShop.Services/CarRepairShop.Services.csproj src/CarRepairShop.Services/
COPY src/CarRepairShop.Application/CarRepairShop.Application.csproj src/CarRepairShop.Application/
COPY src/CarRepairShop.API/CarRepairShop.API.csproj src/CarRepairShop.API/

RUN dotnet restore src/CarRepairShop.API/CarRepairShop.API.csproj

# Copy remaining source and publish
COPY src/ src/

RUN dotnet publish src/CarRepairShop.API/CarRepairShop.API.csproj \
    -c Release \
    -o /app/publish \
    --no-self-contained \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ARG NEW_RELIC_DOTNET_AGENT_VERSION=10.54.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates wget \
    && wget -O /tmp/newrelic-dotnet-agent.deb "https://download.newrelic.com/dot_net_agent/latest_release/newrelic-dotnet-agent_${NEW_RELIC_DOTNET_AGENT_VERSION}_amd64.deb" \
    && dpkg -i /tmp/newrelic-dotnet-agent.deb \
    && rm -f /tmp/newrelic-dotnet-agent.deb \
    && apt-get purge -y --auto-remove wget \
    && rm -rf /var/lib/apt/lists/*

# Run as non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "CarRepairShop.API.dll"]
