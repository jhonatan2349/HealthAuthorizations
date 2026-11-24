# syntax=docker/dockerfile:1.7
#
# Imagen de la API y del migrador de base de datos, desde un único Dockerfile multi-stage.
#
#   docker build --target api      -t healthauthorizations-api .
#   docker build --target migrator -t healthauthorizations-migrator .
#
# - La compilación corre en la arquitectura del host ($BUILDPLATFORM) y produce binarios para la
#   arquitectura destino ($TARGETARCH): sirve para amd64 (servidores) y arm64 (Apple Silicon, Graviton).
# - Las imágenes finales son "chiseled" (Ubuntu sin shell, sin gestor de paquetes y con usuario no root
#   por defecto). La variante "extra" incluye ICU y tzdata, necesarias para culturas y zonas horarias.

ARG DOTNET_VERSION=10.0

# ---------------------------------------------------------------------------------------------------
# build: restaura y publica. Las capas de restore solo se invalidan si cambia un .csproj o los props.
# ---------------------------------------------------------------------------------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG TARGETARCH
ARG CONFIGURATION=Release
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json .editorconfig ./
COPY src/HealthAuthorizations.Domain/HealthAuthorizations.Domain.csproj                 src/HealthAuthorizations.Domain/
COPY src/HealthAuthorizations.Application/HealthAuthorizations.Application.csproj       src/HealthAuthorizations.Application/
COPY src/HealthAuthorizations.Infrastructure/HealthAuthorizations.Infrastructure.csproj src/HealthAuthorizations.Infrastructure/
COPY src/HealthAuthorizations.Api/HealthAuthorizations.Api.csproj                       src/HealthAuthorizations.Api/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet tool restore \
 && dotnet restore src/HealthAuthorizations.Api/HealthAuthorizations.Api.csproj -a "$TARGETARCH"

COPY src/ src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/HealthAuthorizations.Api/HealthAuthorizations.Api.csproj \
        -c "$CONFIGURATION" -a "$TARGETARCH" --no-restore \
        -o /out/api -p:UseAppHost=false

# ---------------------------------------------------------------------------------------------------
# bundle: ejecutable autocontenido con las migraciones de EF Core (no requiere SDK ni dotnet-ef al correr).
# ---------------------------------------------------------------------------------------------------
FROM build AS bundle
ARG TARGETARCH
# dotnet-ef construye el host de la API para descubrir el DbContext y este exige una cadena de conexión.
# Al generar el bundle no se abre ninguna conexión: el valor es un marcador. La real llega al ejecutarlo.
ENV ConnectionStrings__Default="Server=marcador-de-build;Database=HealthAuthorizations"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-arm64 || echo linux-x64) \
 && dotnet ef migrations bundle \
        -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api \
        -r "$RID" --self-contained -o /out/migrator/efbundle

# ---------------------------------------------------------------------------------------------------
# migrator: aplica las migraciones pendientes y termina (job de Kubernetes / servicio de compose).
#   Requiere ConnectionStrings__Default con un usuario con permisos DDL.
# ---------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION}-noble-chiseled-extra AS migrator
WORKDIR /app
COPY --from=bundle /out/migrator/efbundle ./efbundle
COPY --from=build  /out/api/appsettings.json ./appsettings.json
# El bundle es un single-file que se extrae al arrancar: a /tmp para funcionar con el rootfs de solo lectura.
ENV DOTNET_ENVIRONMENT=Production \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---------------------------------------------------------------------------------------------------
# api: imagen final de la aplicación.
# ---------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra AS api
ARG VERSION=0.0.0-local
ARG REVISION=unknown
LABEL org.opencontainers.image.title="HealthAuthorizations API" \
      org.opencontainers.image.description="API de autorizaciones de servicios de salud" \
      org.opencontainers.image.version=$VERSION \
      org.opencontainers.image.revision=$REVISION

WORKDIR /app
COPY --from=build /out/api ./

# Kestrel escucha HTTP en 8080 (puerto no privilegiado). TLS termina en el ingress / proxy inverso.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Https__RedireccionHabilitada=false \
    DOTNET_gcServer=1
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "HealthAuthorizations.Api.dll"]
