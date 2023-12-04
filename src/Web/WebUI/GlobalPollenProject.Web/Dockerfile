FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# set up node
ENV NODE_VERSION 20.10.0
ENV NODE_DOWNLOAD_URL https://nodejs.org/dist/v$NODE_VERSION/node-v$NODE_VERSION-linux-x64.tar.gz
ENV NODE_DOWNLOAD_SHA 132c61652c315a6f784167b97a53e9638bac45853f1544a84d4cbb90fe7a3bda

RUN curl -SL "$NODE_DOWNLOAD_URL" --output nodejs.tar.gz \
    && echo "$NODE_DOWNLOAD_SHA nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs

# Copy project and restore as distinct layers
COPY /.paket ./paket
COPY /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj
RUN dotnet restore --disable-parallel /src/Web/WebUI/GlobalPollenProject.Web/GlobalPollenProject.Web.fsproj

# Copy package.json and install
COPY /src/Web/WebUI/GlobalPollenProject.Web/package.json /src/Web/WebUI/GlobalPollenProject.Web/package.json
COPY /src/Web/WebUI/GlobalPollenProject.Web/yarn.lock /src/Web/WebUI/GlobalPollenProject.Web/yarn.lock
WORKDIR /src/Web/WebUI/GlobalPollenProject.Web
RUN yarn

# Copy everything else and build
COPY /src/Web/WebUI /src/Web/WebUI
COPY /src/Services/Core /src/Services/Core
COPY /src/Shared /src/Shared
RUN yarn run build:prod
RUN dotnet publish -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GlobalPollenProject.Web.dll"]