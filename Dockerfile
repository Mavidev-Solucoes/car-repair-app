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
ARG NEW_RELIC_DOTNET_AGENT_PACKAGE_URL_PREFIX=https://download.newrelic.com/dot_net_agent/archive

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates wget \
    && wget -O /tmp/newrelic-dotnet-agent.deb "${NEW_RELIC_DOTNET_AGENT_PACKAGE_URL_PREFIX}/${NEW_RELIC_DOTNET_AGENT_VERSION}/newrelic-dotnet-agent_${NEW_RELIC_DOTNET_AGENT_VERSION}_amd64.deb" \
    && dpkg -i /tmp/newrelic-dotnet-agent.deb \
    && rm -f /tmp/newrelic-dotnet-agent.deb \
    && apt-get purge -y --auto-remove wget \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
    NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true

# Run as non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "CarRepairShop.API.dll"]
