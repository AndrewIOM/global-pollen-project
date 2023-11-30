FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Services/Core/GlobalPollenProject.Core/GlobalPollenProject.Core.fsproj /src/Services/Core/GlobalPollenProject.Core/GlobalPollenProject.Core.fsproj
COPY /src/Services/Core/GlobalPollenProject.App/GlobalPollenProject.App.fsproj /src/Services/Core/GlobalPollenProject.App/GlobalPollenProject.App.fsproj
COPY /src/Services/Core/GlobalPollenProject.Persistence/GlobalPollenProject.Persistence.fsproj /src/Services/Core/GlobalPollenProject.Persistence/GlobalPollenProject.Persistence.fsproj
RUN dotnet restore --disable-parallel /src/Services/Core/GlobalPollenProject.App/GlobalPollenProject.App.fsproj

# Copy everything else and build
COPY /src/Services/Core /src/Services/Core
COPY /src/Shared /src/Shared
WORKDIR /src/Services/Core/GlobalPollenProject.App
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 5000
WORKDIR /app
COPY --from=build-env /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.App.dll"]