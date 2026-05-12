# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["workers/src/MaemoCompliance.Workers/MaemoCompliance.Workers.csproj", "workers/src/MaemoCompliance.Workers/"]
COPY ["domain/src/MaemoCompliance.Domain/MaemoCompliance.Domain.csproj", "domain/src/MaemoCompliance.Domain/"]
COPY ["application/src/MaemoCompliance.Shared/MaemoCompliance.Shared.csproj", "application/src/MaemoCompliance.Shared/"]
COPY ["application/src/MaemoCompliance.Application/MaemoCompliance.Application.csproj", "application/src/MaemoCompliance.Application/"]
COPY ["infrastructure/src/MaemoCompliance.Infrastructure/MaemoCompliance.Infrastructure.csproj", "infrastructure/src/MaemoCompliance.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "workers/src/MaemoCompliance.Workers/MaemoCompliance.Workers.csproj"

# Copy all source files
COPY workers/src/MaemoCompliance.Workers/ workers/src/MaemoCompliance.Workers/
COPY domain/src/MaemoCompliance.Domain/ domain/src/MaemoCompliance.Domain/
COPY application/src/MaemoCompliance.Shared/ application/src/MaemoCompliance.Shared/
COPY application/src/MaemoCompliance.Application/ application/src/MaemoCompliance.Application/
COPY infrastructure/src/MaemoCompliance.Infrastructure/ infrastructure/src/MaemoCompliance.Infrastructure/

# Build the application
WORKDIR /src/workers/src/MaemoCompliance.Workers
RUN dotnet build "MaemoCompliance.Workers.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MaemoCompliance.Workers.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MaemoCompliance.Workers.dll"]
