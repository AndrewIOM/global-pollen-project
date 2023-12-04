FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Web/WebApi/GlobalPollenProject.Web.API/GlobalPollenProject.Web.API.fsproj /src/Web/WebApi/GlobalPollenProject.Web.API/GlobalPollenProject.Web.API.fsproj
RUN dotnet restore --disable-parallel /src/Web/WebApi/GlobalPollenProject.Web.API/GlobalPollenProject.Web.API.fsproj

# Copy everything else and build
COPY /src/Web/WebApi /src/Web/WebApi
COPY /src/Shared /src/Shared
COPY /src/Services/Core /src/Services/Core
WORKDIR /src/Web/WebApi/GlobalPollenProject.Web.API
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Web.API.dll"]