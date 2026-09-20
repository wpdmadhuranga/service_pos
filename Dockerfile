# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build image with SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY ["backend.sln", "./"]
COPY ["src/backend.API/backend.API.csproj", "src/backend.API/"]
COPY ["src/backend.Application/backend.Application.csproj", "src/backend.Application/"]
COPY ["src/backend.Domain/backend.Domain.csproj", "src/backend.Domain/"]
COPY ["src/backend.Infrastructure/backend.Infrastructure.csproj", "src/backend.Infrastructure/"]
COPY ["src/backend.Persistence/backend.Persistence.csproj", "src/backend.Persistence/"]
COPY ["src/backend.Shared/backend.Shared.csproj", "src/backend.Shared/"]

# Restore packages for API project and its dependencies
RUN dotnet restore "src/backend.API/backend.API.csproj"

# Copy source code and build
COPY src/ src/
WORKDIR "/src/src/backend.API"
RUN dotnet build "backend.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "backend.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final production runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "backend.API.dll"]
