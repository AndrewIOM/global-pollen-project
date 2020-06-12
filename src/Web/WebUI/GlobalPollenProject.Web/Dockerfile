FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj
RUN dotnet restore --disable-parallel /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj

# Copy everything else and build
COPY /src/Web/WebUI /src/Web/WebUI
COPY /src/Shared /src/Shared
WORKDIR /src/Web/WebUI/GlobalPollenProject.Web
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Web.dll"]