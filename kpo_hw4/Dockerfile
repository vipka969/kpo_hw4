FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["kpo_hw4/kpo_hw4.csproj", "kpo_hw4/"]
RUN dotnet restore "kpo_hw4/kpo_hw4.csproj"
COPY . .
WORKDIR "/src/kpo_hw4"
RUN dotnet build "kpo_hw4.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "kpo_hw4.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kpo_hw4.dll"]
