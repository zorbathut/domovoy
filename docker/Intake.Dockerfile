FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Domovoy.Intake/Domovoy.Intake.csproj", "src/Domovoy.Intake/"]
COPY ["src/Domovoy.Database/Domovoy.Database.csproj", "src/Domovoy.Database/"]
COPY ["src/Domovoy.Shared/Domovoy.Shared.csproj", "src/Domovoy.Shared/"]
RUN dotnet restore "src/Domovoy.Intake/Domovoy.Intake.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Domovoy.Intake"
RUN dotnet build "Domovoy.Intake.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Domovoy.Intake.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 1973
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Domovoy.Intake.dll"]
