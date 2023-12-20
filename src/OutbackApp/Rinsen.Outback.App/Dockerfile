#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/OutbackApp/Rinsen.Outback.App/Rinsen.Outback.App.csproj", "src/OutbackApp/Rinsen.Outback.App/"]
COPY ["src/OutbackApp/Rinsen.IdentityProvider/Rinsen.IdentityProvider.csproj", "src/OutbackApp/Rinsen.IdentityProvider/"]
RUN dotnet restore "src/OutbackApp/Rinsen.Outback.App/Rinsen.Outback.App.csproj"
COPY . .
WORKDIR "/src/src/OutbackApp/Rinsen.Outback.App"
RUN dotnet build "Rinsen.Outback.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rinsen.Outback.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rinsen.Outback.App.dll"]