# syntax=docker/dockerfile:1.3
FROM ghcr.io/openfaas/of-watchdog:0.9.6 as watchdog

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim as builder
ARG RUN_TESTS="true"
ARG AZURE_DEVOPS_FEED_USERNAME
ARG AZURE_DEVOPS_FEED_ACCESSTOKEN

RUN apt update && apt install -y libxml2-utils

# suppress data collection
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# caches restore result by copying csproj files separately
WORKDIR /source/faas/function
COPY function/*.sln .
COPY function/src/function.csproj ./src/
COPY function/tests/integration/integration.csproj ./tests/integration/
COPY function/tests/unit/unit.csproj ./tests/unit/

# list files in WORKDIR
RUN ls

# restore packages
COPY add-nuget-config.sh /tmp/
RUN chmod u+x /tmp/add-nuget-config.sh
RUN --mount=type=secret,id=nuget.config /tmp/add-nuget-config.sh nuget.config \
    dotnet restore

# Copies the rest of the code
WORKDIR /source/faas
COPY . .

# Run the unit tests
RUN dotnet test function/tests/unit/unit.csproj -c release

# Run the integration tests
RUN if [ "$RUN_TESTS" != "false" ]; then dotnet test function/tests/integration/integration.csproj -c release; fi

# build and publish
RUN dotnet publish \
    -c release \
    -r linux-x64 \
    --no-self-contained \
    -p:PublishReadyToRun=true \
    -o published \
    function/src/function.csproj

# runner
FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim

# Install curl in the runtime container
RUN apt update && apt install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=watchdog /fwatchdog /usr/bin/

# Create a non-root user
RUN addgroup --system app \
    && adduser --system --ingroup app app

WORKDIR /home/app/
COPY --from=builder /source/faas/published .
RUN chown app:app -R /home/app

USER app

ENV fprocess="./function"
ENV cgi_headers="true"
ENV mode="http"
ENV upstream_url="http://localhost:9000"
ENV ASPNETCORE_URLS=http://+:9000

ENV exec_timeout="120s"
ENV write_timeout="120s"
ENV read_timeout="120s"

ENV prefix_logs="false"

EXPOSE 8080

HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1

CMD ["fwatchdog"]
