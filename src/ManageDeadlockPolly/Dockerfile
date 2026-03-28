# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ManageDeadlockPolly.csproj", "."]
RUN dotnet restore "ManageDeadlockPolly.csproj"
COPY . .
RUN dotnet build "ManageDeadlockPolly.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "ManageDeadlockPolly.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ManageDeadlockPolly.dll"]
