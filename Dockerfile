﻿# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Synapse/Synapse.csproj", "src/Synapse/"]
RUN dotnet restore "src/Synapse/Synapse.csproj"

# Copy remaining source code and build application
COPY . .
WORKDIR "/src/Synapse"
RUN dotnet build "Synapse.csproj" -c Release -o /app/build
RUN dotnet publish "Synapse.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password \
    --home /app \
    --gecos '' dotnetuser && chown -R dotnetuser /app

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R dotnetuser /app/logs

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=UTC

# Copy configuration files
COPY ["src/Synapse/appsettings.json", "appsettings.json"]
COPY ["src/Synapse/appsettings.Production.json", "appsettings.Production.json"]
COPY ["src/Synapse/nlog.config", "nlog.config"]

# Switch to non-root user
USER dotnetuser

# Set entrypoint
ENTRYPOINT ["dotnet", "Synapse.dll"]
