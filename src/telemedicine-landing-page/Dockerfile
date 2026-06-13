# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY telemedicine-landing-page.sln ./
COPY src/telemedicine-landing-page/telemedicine-landing-page.csproj src/telemedicine-landing-page/
COPY tests/telemedicine-landing-page.tests/telemedicine-landing-page.tests.csproj tests/telemedicine-landing-page.tests/
RUN dotnet restore telemedicine-landing-page.sln

COPY . .
RUN dotnet publish src/telemedicine-landing-page/telemedicine-landing-page.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && (getent group app >/dev/null || groupadd --system app) \
    && (id -u app >/dev/null 2>&1 || useradd --system --gid app --home /app app) \
    && mkdir -p /app/App_Data/procedure-attachments \
    && chown -R app:app /app/App_Data
COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "TelemedicineLandingPage.dll"]
