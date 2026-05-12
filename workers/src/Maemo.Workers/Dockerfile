# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["workers/src/Maemo.Workers/Maemo.Workers.csproj", "workers/src/Maemo.Workers/"]
COPY ["domain/src/Maemo.Domain/Maemo.Domain.csproj", "domain/src/Maemo.Domain/"]
COPY ["application/src/Maemo.Shared/Maemo.Shared.csproj", "application/src/Maemo.Shared/"]
COPY ["application/src/Maemo.Application/Maemo.Application.csproj", "application/src/Maemo.Application/"]
COPY ["infrastructure/src/Maemo.Infrastructure/Maemo.Infrastructure.csproj", "infrastructure/src/Maemo.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "workers/src/Maemo.Workers/Maemo.Workers.csproj"

# Copy all source files
COPY workers/src/Maemo.Workers/ workers/src/Maemo.Workers/
COPY domain/src/Maemo.Domain/ domain/src/Maemo.Domain/
COPY application/src/Maemo.Shared/ application/src/Maemo.Shared/
COPY application/src/Maemo.Application/ application/src/Maemo.Application/
COPY infrastructure/src/Maemo.Infrastructure/ infrastructure/src/Maemo.Infrastructure/

# Build the application
WORKDIR /src/workers/src/Maemo.Workers
RUN dotnet build "Maemo.Workers.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Maemo.Workers.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Maemo.Workers.dll"]

