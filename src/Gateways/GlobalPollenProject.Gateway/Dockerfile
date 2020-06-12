FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src

ARG RUN=pwd
RUN ${RUN}

# Copy project and restore as distinct layers
COPY /src/Gateways/GlobalPollenProject.Gateway/GlobalPollenProject.Gateway.fsproj /src/Gateways/GlobalPollenProject.Gateway/GlobalPollenProject.Gateway.fsproj
RUN dotnet restore --disable-parallel /src/Gateways/GlobalPollenProject.Gateway

COPY /src/Gateways /src/Gateways
COPY /src/Shared /src/Shared
WORKDIR /src/Gateways/GlobalPollenProject.Gateway/
RUN dotnet restore --disable-parallel
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Gateway.dll"]