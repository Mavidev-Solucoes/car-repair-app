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

# Run as non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CarRepairShop.API.dll"]
