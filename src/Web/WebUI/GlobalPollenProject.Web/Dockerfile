FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src

# set up node
ENV NODE_VERSION 14.5.0
ENV NODE_DOWNLOAD_URL https://nodejs.org/dist/v$NODE_VERSION/node-v$NODE_VERSION-linux-x64.tar.gz
ENV NODE_DOWNLOAD_SHA d5a05bbf5ef7f49752eca0d4fc946834dfda86088627248856795a61c81df1a2

RUN curl -SL "$NODE_DOWNLOAD_URL" --output nodejs.tar.gz \
    && echo "$NODE_DOWNLOAD_SHA nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs

# set up yarn
ENV YARN_VERSION 1.22.4

RUN set -ex \
  && wget -qO- https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --import \
  && curl -fSLO --compressed "https://yarnpkg.com/downloads/$YARN_VERSION/yarn-v$YARN_VERSION.tar.gz" \
  && curl -fSLO --compressed "https://yarnpkg.com/downloads/$YARN_VERSION/yarn-v$YARN_VERSION.tar.gz.asc" \
  && gpg --batch --verify yarn-v$YARN_VERSION.tar.gz.asc yarn-v$YARN_VERSION.tar.gz \
  && mkdir -p /opt/yarn \
  && tar -xzf yarn-v$YARN_VERSION.tar.gz -C /opt/yarn --strip-components=1 \
  && ln -s /opt/yarn/bin/yarn /usr/local/bin/yarn \
  && ln -s /opt/yarn/bin/yarn /usr/local/bin/yarnpkg \
  && rm yarn-v$YARN_VERSION.tar.gz.asc yarn-v$YARN_VERSION.tar.gz

# Copy project and restore as distinct layers
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