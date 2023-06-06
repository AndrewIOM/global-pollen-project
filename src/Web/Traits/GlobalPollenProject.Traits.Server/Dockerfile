FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Web/Traits/GlobalPollenProject.Traits.Server/GlobalPollenProject.Traits.Server.fsproj /src/Web/Traits/GlobalPollenProject.Traits.Server/GlobalPollenProject.Traits.Server.fsproj
RUN dotnet restore --disable-parallel /src/Web/Traits/GlobalPollenProject.Traits.Server/GlobalPollenProject.Traits.Server.fsproj

# Copy everything else and build
COPY /src/Web/Traits /src/Web/Traits
COPY /src/Shared /src/Shared
COPY /src/Services/Core /src/Services/Core
WORKDIR /src/Web/Traits/GlobalPollenProject.Traits.Server
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Traits.Server.dll"]