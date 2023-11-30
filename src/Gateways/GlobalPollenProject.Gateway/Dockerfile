FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Gateways/GlobalPollenProject.Gateway/GlobalPollenProject.Gateway.fsproj /src/Gateways/GlobalPollenProject.Gateway/GlobalPollenProject.Gateway.fsproj
RUN dotnet restore --disable-parallel /src/Gateways/GlobalPollenProject.Gateway

COPY /src/Gateways /src/Gateways
COPY /src/Shared /src/Shared
WORKDIR /src/Gateways/GlobalPollenProject.Gateway/
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Gateway.dll"]