FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["DesafioByCoders.Api/DesafioByCoders.Api.csproj", "DesafioByCoders.Api/"]
COPY ["DesafioByCoders.ServiceDefaults/DesafioByCoders.ServiceDefaults.csproj", "DesafioByCoders.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "DesafioByCoders.Api/DesafioByCoders.Api.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/DesafioByCoders.Api"
RUN dotnet build "./DesafioByCoders.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DesafioByCoders.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DesafioByCoders.Api.dll"]
