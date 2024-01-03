FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0-jammy-chiseled-extra AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project and restore as distinct layers
COPY /.paket /.paket
COPY /.config /.config
COPY /paket.dependencies /paket.dependencies
COPY /paket.lock /paket.lock
RUN dotnet tool restore
COPY /src/Services/Identity/GlobalPollenProject.Identity/GlobalPollenProject.Identity.fsproj /src/Services/Identity/GlobalPollenProject.Identity/GlobalPollenProject.Identity.fsproj
RUN dotnet restore --disable-parallel /src/Services/Identity/GlobalPollenProject.Identity

COPY /src/Services/Identity /src/Services/Identity
COPY /src/Shared /src/Shared
WORKDIR /src/Services/Identity/GlobalPollenProject.Identity
RUN dotnet publish -c Release -r linux-x64 -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["./GlobalPollenProject.Identity"]
