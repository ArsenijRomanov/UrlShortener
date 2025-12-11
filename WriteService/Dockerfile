# ---------------------
# СТАДИЯ СБОРКИ (BUILD)
# ---------------------
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build 
    WORKDIR /src
    
    # 1. Копируем только файлы решения и WriteService
    COPY *.sln ./
    COPY WriteService/*.csproj ./WriteService/
    COPY Shared.Domain/*.csproj ./Shared.Domain/
    COPY Shared.Infrastructure/*.csproj ./Shared.Infrastructure/
    COPY Contracts/*.csproj ./Contracts/
    
    # 2. Восстанавливаем зависимости
    RUN dotnet restore WriteService/WriteService.csproj
    
    # 3. Копируем исходный код WriteService и общие библиотеки
    COPY WriteService/ ./WriteService/
    COPY Shared.Domain/ ./Shared.Domain/
    COPY Shared.Infrastructure/ ./Shared.Infrastructure/
    COPY Contracts/ ./Contracts/
    
    # 4. Собираем и публикуем ТОЛЬКО WriteService
    WORKDIR /src/WriteService
    RUN dotnet publish -c Release -o /app/publish
    
    # ---------------------
    # СТАДИЯ ЗАПУСКА (FINAL)
    # ---------------------
    FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
    WORKDIR /app
    COPY --from=build /app/publish .
    
    ENV ASPNETCORE_URLS=http://+:8080 
    EXPOSE 8080 
    ENTRYPOINT ["dotnet", "WriteService.dll"]