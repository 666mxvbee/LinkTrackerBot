FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY ["src/LinkTracker.Scrapper/LinkTracker.Scrapper.csproj", "src/LinkTracker.Scrapper/"]
COPY ["src/LinkTracker.Shared/LinkTracker.Shared.csproj", "src/LinkTracker.Shared/"]

RUN dotnet restore "src/LinkTracker.Scrapper/LinkTracker.Scrapper.csproj"

COPY . .

WORKDIR "/app/src/LinkTracker.Scrapper"
RUN dotnet publish "LinkTracker.Scrapper.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkTracker.Scrapper.dll"]