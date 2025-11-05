FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Wumpus.Web/Wumpus.Web.csproj", "src/Wumpus.Web/"]
COPY ["src/Wumpus.Database/Wumpus.Database.csproj", "src/Wumpus.Database/"]
COPY ["src/Wumpus.Shared/Wumpus.Shared.csproj", "src/Wumpus.Shared/"]
RUN dotnet restore "src/Wumpus.Web/Wumpus.Web.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Wumpus.Web"
RUN dotnet build "Wumpus.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Wumpus.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5001
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wumpus.Web.dll"]
