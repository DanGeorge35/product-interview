FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/ProductService.API/ProductService.API.csproj", "backend/ProductService.API/"]
COPY ["backend/ProductService.Application/ProductService.Application.csproj", "backend/ProductService.Application/"]
COPY ["backend/ProductService.Domain/ProductService.Domain.csproj", "backend/ProductService.Domain/"]
COPY ["backend/ProductService.Infrastructure/ProductService.Infrastructure.csproj", "backend/ProductService.Infrastructure/"]

RUN dotnet restore "backend/ProductService.API/ProductService.API.csproj"

COPY . .
WORKDIR "/src/backend/ProductService.API"
RUN dotnet build "ProductService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.API.dll"]
