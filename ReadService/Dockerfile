FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build 
WORKDIR /src

COPY *.sln ./
COPY ReadService/*.csproj ./ReadService/
COPY Shared.Domain/*.csproj ./Shared.Domain/
COPY Shared.Infrastructure/*.csproj ./Shared.Infrastructure/
COPY Contracts/*.csproj ./Contracts/

RUN dotnet restore ReadService/ReadService.csproj

COPY ReadService/ ./ReadService/
COPY Shared.Domain/ ./Shared.Domain/
COPY Shared.Infrastructure/ ./Shared.Infrastructure/
COPY Contracts/ ./Contracts/

WORKDIR /src/ReadService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 
EXPOSE 8080 
ENTRYPOINT ["dotnet", "ReadService.dll"]