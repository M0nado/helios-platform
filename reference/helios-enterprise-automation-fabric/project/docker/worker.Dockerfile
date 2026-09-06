FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY src/dotnet/HELIOS.Fabric.Contracts/HELIOS.Fabric.Contracts.csproj src/dotnet/HELIOS.Fabric.Contracts/
COPY src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj src/dotnet/HELIOS.Fabric.Worker/
RUN dotnet restore src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj
COPY src/dotnet src/dotnet
RUN dotnet publish src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
WORKDIR /app
COPY --from=build /out .
COPY config /app/config
USER app
ENTRYPOINT ["dotnet", "HELIOS.Fabric.Worker.dll"]

