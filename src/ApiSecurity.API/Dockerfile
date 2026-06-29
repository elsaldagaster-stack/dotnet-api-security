FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ApiSecurity.API/ApiSecurity.API.csproj", "src/ApiSecurity.API/"]
COPY ["src/ApiSecurity.Application/ApiSecurity.Application.csproj", "src/ApiSecurity.Application/"]
COPY ["src/ApiSecurity.Infrastructure/ApiSecurity.Infrastructure.csproj", "src/ApiSecurity.Infrastructure/"]
COPY ["src/ApiSecurity.Domain/ApiSecurity.Domain.csproj", "src/ApiSecurity.Domain/"]
RUN dotnet restore "src/ApiSecurity.API/ApiSecurity.API.csproj"
COPY . .
RUN dotnet build "src/ApiSecurity.API/ApiSecurity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/ApiSecurity.API/ApiSecurity.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiSecurity.API.dll"]
