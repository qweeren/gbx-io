FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH=x64
ARG APPNAME=GbxIo.PWA
WORKDIR /src

RUN apt-get update && apt-get install -y python3
RUN dotnet workload install wasm-tools --skip-manifest-update

RUN dotnet nuget add source https://nuget.gbx.tools/v3/index.json --name nuget.gbx.tools

# copy csproj and restore as distinct layers
COPY $APPNAME/*.csproj $APPNAME/
COPY GbxIo.Components/*.csproj GbxIo.Components/
RUN dotnet restore $APPNAME/$APPNAME.csproj -a $TARGETARCH

# copy and publish app and libraries
COPY $APPNAME/ $APPNAME/
COPY GbxIo.Components/ GbxIo.Components/
WORKDIR /src/$APPNAME
RUN dotnet publish -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app --no-restore


# final stage/image
FROM nginx:alpine
EXPOSE 8080
COPY GbxIo.PWA/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/wwwroot /usr/share/nginx/html