FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Wumpus.Intake/Wumpus.Intake.csproj", "src/Wumpus.Intake/"]
COPY ["src/Wumpus.Database/Wumpus.Database.csproj", "src/Wumpus.Database/"]
COPY ["src/Wumpus.Shared/Wumpus.Shared.csproj", "src/Wumpus.Shared/"]
RUN dotnet restore "src/Wumpus.Intake/Wumpus.Intake.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Wumpus.Intake"
RUN dotnet build "Wumpus.Intake.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Wumpus.Intake.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 1973
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wumpus.Intake.dll"]
