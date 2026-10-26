FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/Telemetry.Api/Telemetry.Api.csproj src/Telemetry.Api/
COPY src/Telemetry.Core/Telemetry.Core.csproj src/Telemetry.Core/
COPY src/Telemetry.Infrastructure/Telemetry.Infrastructure.csproj src/Telemetry.Infrastructure/

RUN dotnet restore src/Telemetry.Api/Telemetry.Api.csproj

COPY src/ ./src/

RUN dotnet publish src/Telemetry.Api/Telemetry.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "Telemetry.Api.dll"]