FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# set up node
ENV NODE_VERSION 20.10.0
ENV NODE_DOWNLOAD_URL https://nodejs.org/dist/v$NODE_VERSION/node-v$NODE_VERSION-linux-x64.tar.gz
ENV NODE_DOWNLOAD_SHA d3f0908a9d9190a8525c5b9a716ed91bb57e908555841b0c47f75b2a001ff91b

RUN curl -SL "$NODE_DOWNLOAD_URL" --output nodejs.tar.gz \
    && echo "$NODE_DOWNLOAD_SHA nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs

# Copy project and restore as distinct layers
COPY /.paket /.paket
COPY /.config /.config
COPY /paket.dependencies /paket.dependencies
COPY /paket.lock /paket.lock
RUN dotnet tool restore
COPY /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj
RUN dotnet restore --disable-parallel /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj

# Copy everything else and build
COPY /src/Web/WebUI /src/Web/WebUI
COPY /src/Services/Core /src/Services/Core
COPY /src/Shared /src/Shared
WORKDIR /src/Web/WebUI/GlobalPollenProject.Web
RUN corepack enable
RUN dotnet publish -c Release -r linux-x64 -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["./GlobalPollenProject.Web"]