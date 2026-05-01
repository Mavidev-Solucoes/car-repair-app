# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/CarRepairShop.API/CarRepairShop.API.csproj \
    -c Release \
    -o /app/publish \
    --no-self-contained

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CarRepairShop.API.dll"]
