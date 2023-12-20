#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/OutbackApp/Rinsen.Outback.App.Installation/Rinsen.Outback.App.Installation.csproj", "src/OutbackApp/Rinsen.Outback.App.Installation/"]
COPY ["src/OutbackApp/Rinsen.IdentityProvider/Rinsen.IdentityProvider.csproj", "src/OutbackApp/Rinsen.IdentityProvider/"]
RUN dotnet restore "src/OutbackApp/Rinsen.Outback.App.Installation/Rinsen.Outback.App.Installation.csproj"
COPY . .
WORKDIR "/src/src/OutbackApp/Rinsen.Outback.App.Installation"
RUN dotnet build "Rinsen.Outback.App.Installation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rinsen.Outback.App.Installation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rinsen.Outback.App.Installation.dll"]