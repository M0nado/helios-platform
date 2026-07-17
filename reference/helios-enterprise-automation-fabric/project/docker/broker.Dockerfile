FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY src/dotnet/HELIOS.Fabric.Contracts/HELIOS.Fabric.Contracts.csproj src/dotnet/HELIOS.Fabric.Contracts/
COPY src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj src/dotnet/HELIOS.Fabric.Broker/
RUN dotnet restore src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj
COPY src/dotnet src/dotnet
RUN dotnet publish src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "HELIOS.Fabric.Broker.dll"]

