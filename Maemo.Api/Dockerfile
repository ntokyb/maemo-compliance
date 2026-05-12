# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Maemo.Api/Maemo.Api.csproj", "Maemo.Api/"]
COPY ["domain/src/Maemo.Domain/Maemo.Domain.csproj", "domain/src/Maemo.Domain/"]
COPY ["application/src/Maemo.Shared/Maemo.Shared.csproj", "application/src/Maemo.Shared/"]
COPY ["application/src/Maemo.Application/Maemo.Application.csproj", "application/src/Maemo.Application/"]
COPY ["infrastructure/src/Maemo.Infrastructure/Maemo.Infrastructure.csproj", "infrastructure/src/Maemo.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Maemo.Api/Maemo.Api.csproj"

# Copy all source files
COPY Maemo.Api/ Maemo.Api/
COPY domain/src/Maemo.Domain/ domain/src/Maemo.Domain/
COPY application/src/Maemo.Shared/ application/src/Maemo.Shared/
COPY application/src/Maemo.Application/ application/src/Maemo.Application/
COPY infrastructure/src/Maemo.Infrastructure/ infrastructure/src/Maemo.Infrastructure/

# Build the application
WORKDIR /src/Maemo.Api
RUN dotnet build "Maemo.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Maemo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "Maemo.Api.dll"]

