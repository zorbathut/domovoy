FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Domovoy.DiscordBot/Domovoy.DiscordBot.csproj", "src/Domovoy.DiscordBot/"]
COPY ["src/Domovoy.NotificationClient/Domovoy.NotificationClient.csproj", "src/Domovoy.NotificationClient/"]
COPY ["src/Domovoy.Shared/Domovoy.Shared.csproj", "src/Domovoy.Shared/"]
RUN dotnet restore "src/Domovoy.DiscordBot/Domovoy.DiscordBot.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Domovoy.DiscordBot"
RUN dotnet build "Domovoy.DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Domovoy.DiscordBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Domovoy.DiscordBot.dll"]
