FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Web/Lab/GlobalPollenProject.Lab.Server/GlobalPollenProject.Lab.Server.fsproj /src/Web/Lab/GlobalPollenProject.Lab.Server/GlobalPollenProject.Lab.Server.fsproj
RUN dotnet restore --disable-parallel /src/Web/Lab/GlobalPollenProject.Lab.Server/GlobalPollenProject.Lab.Server.fsproj

# Copy everything else and build
COPY /src/Web/Lab /src/Web/Lab
COPY /src/Shared /src/Shared
COPY /src/Services/Core /src/Services/Core
WORKDIR /src/Web/Lab/GlobalPollenProject.Lab.Server
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Lab.Server.dll"]