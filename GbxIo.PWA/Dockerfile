FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH=x64
ARG APPNAME=GbxIo.PWA
WORKDIR /src

RUN apk add --no-cache nodejs
# RUN dotnet workload install wasm-tools # bugged in alpine

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
COPY --from=build /app/wwwroot /usr/share/nginx/html