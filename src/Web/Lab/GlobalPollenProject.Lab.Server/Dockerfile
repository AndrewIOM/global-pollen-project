FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Web/Lab/GlobalPollenProject.Lab.Server/GlobalPollenProject.Lab.Server.fsproj /src/Web/Lab/GlobalPollenProject.Lab.Server/GlobalPollenProject.Lab.Server.fsproj
RUN dotnet restore --disable-parallel /src/Web/Lab/GlobalPollenProject.Lab.Server/GlobalPollenProject.Lab.Server.fsproj

# Copy everything else and build
COPY /src/Web/Lab /src/Web/Lab
COPY /src/Shared /src/Shared
COPY /src/Services/Core /src/Services/Core
WORKDIR /src/Web/Lab/GlobalPollenProject.Lab.Server
RUN dotnet publish -c Release -r linux-x64 -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["./GlobalPollenProject.Lab.Server"]