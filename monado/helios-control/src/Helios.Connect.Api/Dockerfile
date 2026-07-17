FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Helios.Connect.Contracts/Helios.Connect.Contracts.csproj src/Helios.Connect.Contracts/
COPY src/Helios.Connect.Api/Helios.Connect.Api.csproj src/Helios.Connect.Api/
RUN dotnet restore src/Helios.Connect.Api/Helios.Connect.Api.csproj
COPY . .
RUN dotnet publish src/Helios.Connect.Api/Helios.Connect.Api.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Helios.Connect.Api.dll"]
