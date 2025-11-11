FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Domovoy.Web/Domovoy.Web.csproj", "src/Domovoy.Web/"]
COPY ["src/Domovoy.Database/Domovoy.Database.csproj", "src/Domovoy.Database/"]
COPY ["src/Domovoy.Shared/Domovoy.Shared.csproj", "src/Domovoy.Shared/"]
RUN dotnet restore "src/Domovoy.Web/Domovoy.Web.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Domovoy.Web"
RUN dotnet build "Domovoy.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Domovoy.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 1975
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Domovoy.Web.dll"]
