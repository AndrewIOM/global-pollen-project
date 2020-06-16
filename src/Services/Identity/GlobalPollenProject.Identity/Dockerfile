FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /src/Services/Identity/GlobalPollenProject.Identity/GlobalPollenProject.Identity.fsproj /src/Services/Identity/GlobalPollenProject.Identity/GlobalPollenProject.Identity.fsproj
RUN dotnet restore --disable-parallel /src/Services/Identity/GlobalPollenProject.Identity

COPY /src/Services/Identity /src/Services/Identity
COPY /src/Shared /src/Shared
WORKDIR /src/Services/Identity/GlobalPollenProject.Identity
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Identity.dll"]
