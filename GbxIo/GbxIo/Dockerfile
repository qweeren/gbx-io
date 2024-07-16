FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH=x64
ARG APPNAME=GbxIo
RUN apt-get update && apt-get install -y git python3
WORKDIR /src

COPY .git/ ./.git/

RUN dotnet workload install wasm-tools

# copy csproj and restore as distinct layers
COPY $APPNAME/$APPNAME/*.csproj $APPNAME/$APPNAME/
COPY $APPNAME/$APPNAME.Client/*.csproj $APPNAME/$APPNAME.Client/
COPY $APPNAME.Components/*.csproj $APPNAME.Components/
RUN dotnet restore $APPNAME/$APPNAME/$APPNAME.csproj -a $TARGETARCH

# copy and publish app and libraries
COPY $APPNAME/$APPNAME $APPNAME/$APPNAME
COPY $APPNAME/$APPNAME.Client $APPNAME/$APPNAME.Client
COPY $APPNAME.Components $APPNAME.Components
RUN dotnet publish $APPNAME/$APPNAME -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app --no-restore


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
EXPOSE 8081
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./GbxIo"]