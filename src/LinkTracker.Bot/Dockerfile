FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY ["src/LinkTracker.Bot/LinkTracker.Bot.csproj", "src/LinkTracker.Bot/"]
COPY ["src/LinkTracker.Shared/LinkTracker.Shared.csproj", "src/LinkTracker.Shared/"]

RUN dotnet restore "src/LinkTracker.Bot/LinkTracker.Bot.csproj"

COPY . .

WORKDIR "/app/src/LinkTracker.Bot"
RUN dotnet publish "LinkTracker.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkTracker.Bot.dll"]