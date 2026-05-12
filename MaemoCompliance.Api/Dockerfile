# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MaemoCompliance.Api/MaemoCompliance.Api.csproj", "MaemoCompliance.Api/"]
COPY ["domain/src/MaemoCompliance.Domain/MaemoCompliance.Domain.csproj", "domain/src/MaemoCompliance.Domain/"]
COPY ["application/src/MaemoCompliance.Shared/MaemoCompliance.Shared.csproj", "application/src/MaemoCompliance.Shared/"]
COPY ["application/src/MaemoCompliance.Application/MaemoCompliance.Application.csproj", "application/src/MaemoCompliance.Application/"]
COPY ["infrastructure/src/MaemoCompliance.Infrastructure/MaemoCompliance.Infrastructure.csproj", "infrastructure/src/MaemoCompliance.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MaemoCompliance.Api/MaemoCompliance.Api.csproj"

# Copy all source files
COPY MaemoCompliance.Api/ MaemoCompliance.Api/
COPY domain/src/MaemoCompliance.Domain/ domain/src/MaemoCompliance.Domain/
COPY application/src/MaemoCompliance.Shared/ application/src/MaemoCompliance.Shared/
COPY application/src/MaemoCompliance.Application/ application/src/MaemoCompliance.Application/
COPY infrastructure/src/MaemoCompliance.Infrastructure/ infrastructure/src/MaemoCompliance.Infrastructure/

# Build the application
WORKDIR /src/MaemoCompliance.Api
RUN dotnet build "MaemoCompliance.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MaemoCompliance.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for healthchecks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Expose port 8080
EXPOSE 8080

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MaemoCompliance.Api.dll"]
