FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ContractManagement.API/*.csproj ContractManagement.API/
COPY ContractManagement.Infrastructure/*.csproj ContractManagement.Infrastructure/
RUN dotnet restore ContractManagement.API/ContractManagement.API.csproj

COPY . .
RUN dotnet publish ContractManagement.API/ContractManagement.API.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    libreoffice-writer && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5005
EXPOSE 5005
ENTRYPOINT ["dotnet", "ContractManagement.API.dll"]