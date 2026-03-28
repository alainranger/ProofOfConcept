# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore uniquement les csproj pour tirer parti du cache Docker
COPY ["src/DeadlockPolly.Core/DeadlockPolly.Core.csproj",  "src/DeadlockPolly.Core/"]
COPY ["src/DeadlockPolly.Demo/DeadlockPolly.Demo.csproj",  "src/DeadlockPolly.Demo/"]
RUN dotnet restore "src/DeadlockPolly.Demo/DeadlockPolly.Demo.csproj"

# Copier le reste du code source
COPY . .

# Stage 2: Publish
FROM build AS publish
WORKDIR /src/src/DeadlockPolly.Demo
RUN dotnet publish "DeadlockPolly.Demo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeadlockPolly.Demo.dll"]
