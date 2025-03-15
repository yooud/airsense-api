FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Airsense.API/Airsense.API.csproj", "Airsense.API/"]
RUN dotnet restore "Airsense.API/Airsense.API.csproj"
COPY . .
WORKDIR "/src/Airsense.API"
RUN dotnet build "Airsense.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS devtools
RUN apt-get update && \
    apt-get install --no-install-recommends -y unzip wget && \
    rm -rf /var/lib/apt/lists/*
RUN mkdir -p /opt/JetBrains/RiderDebuggerTools/ && \
    wget -O /tmp/rider-debug-tools.zip "https://data.services.jetbrains.com/products/download?code=RRD&platform=linux64" && \
    unzip /tmp/rider-debug-tools.zip -d /opt/JetBrains/RiderDebuggerTools/ && \
    rm /tmp/rider-debug-tools.zip

FROM build AS development
COPY --from=devtools /opt/JetBrains/RiderDebuggerTools/ /opt/JetBrains/RiderDebuggerTools/
CMD ["dotnet", "run", "--no-launch-profile"]

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Airsense.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Airsense.API.dll"]