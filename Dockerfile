# ── Build stage ────────────────────────────────────────────────────────────────
# NOTE: The .esproj has ShouldRunBuildScript=false, so the React SPA is NOT
# built here. It is built and served by a separate Nginx container (see
# Dockerfile.client and docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /src

# Copy both projects — build context must be the Projects/ parent directory
# because ArbiScannerAdminPanel.API references ArbiScannerWebApp projects
COPY ArbiScannerAdminPannel/ ArbiScannerAdminPannel/
COPY ArbiScannerWebApp/ ArbiScannerWebApp/

WORKDIR /src/ArbiScannerAdminPannel

# Restore NuGet packages
RUN dotnet restore ArbiScannerAdminPanel.API/ArbiScannerAdminPanel.API.csproj

# Remove the esproj reference so the JS SDK does not trigger npm run build during publish.
# The React client is built in its own container (see Dockerfile.client).
RUN sed -i '/<ProjectReference.*\.esproj/,/<\/ProjectReference>/d' \
        "ArbiScannerAdminPanel.API/ArbiScannerAdminPanel.API.csproj"

# Publish the .NET API
RUN dotnet publish ArbiScannerAdminPanel.API/ArbiScannerAdminPanel.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ArbiScannerAdminPanel.API.dll"]
