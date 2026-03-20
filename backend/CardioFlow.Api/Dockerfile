# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer cache optimization)
COPY backend/CardioFlow.Api/CardioFlow.Api.csproj backend/CardioFlow.Api/
RUN dotnet restore backend/CardioFlow.Api/CardioFlow.Api.csproj

# Copy full source and publish
COPY . .
WORKDIR /src/backend/CardioFlow.Api
RUN dotnet publish CardioFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "CardioFlow.Api.dll"]
