FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Wumpus.DiscordBot/Wumpus.DiscordBot.csproj", "src/Wumpus.DiscordBot/"]
COPY ["src/Wumpus.NotificationClient/Wumpus.NotificationClient.csproj", "src/Wumpus.NotificationClient/"]
COPY ["src/Wumpus.Shared/Wumpus.Shared.csproj", "src/Wumpus.Shared/"]
RUN dotnet restore "src/Wumpus.DiscordBot/Wumpus.DiscordBot.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Wumpus.DiscordBot"
RUN dotnet build "Wumpus.DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Wumpus.DiscordBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wumpus.DiscordBot.dll"]
