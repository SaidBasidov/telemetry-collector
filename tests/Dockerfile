FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/Telemetry.Core/ src/Telemetry.Core/
COPY src/Telemetry.Infrastructure/ src/Telemetry.Infrastructure/
COPY tests/Telemetry.IntegrationChecks/ tests/Telemetry.IntegrationChecks/
COPY sql/init.sql sql/init.sql
RUN dotnet publish tests/Telemetry.IntegrationChecks/Telemetry.IntegrationChecks.csproj \
    -c Release -o /app/checks /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/checks .
USER $APP_UID
ENTRYPOINT ["dotnet", "Telemetry.IntegrationChecks.dll"]
