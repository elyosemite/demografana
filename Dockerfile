FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore demografana.Core/demografana.Core.csproj
RUN dotnet restore demografana.Api/demografana.Api.csproj
RUN dotnet restore demografana.Relay/demografana.Relay.csproj
RUN dotnet restore demografana.Worker/demografana.Worker.csproj
RUN dotnet publish demografana.Api/demografana.Api.csproj -c Release -o /app/api --no-restore
RUN dotnet publish demografana.Relay/demografana.Relay.csproj -c Release -o /app/relay --no-restore
RUN dotnet publish demografana.Worker/demografana.Worker.csproj -c Release -o /app/worker --no-restore

FROM base AS migrator
COPY --from=build /app/api .
ENTRYPOINT ["dotnet", "demografana.Api.dll", "--migrate-only"]

FROM base AS api
COPY --from=build /app/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "demografana.Api.dll"]

FROM base AS relay
COPY --from=build /app/relay .
ENTRYPOINT ["dotnet", "demografana.Relay.dll"]

FROM base AS worker
COPY --from=build /app/worker .
ENTRYPOINT ["dotnet", "demografana.Worker.dll"]
