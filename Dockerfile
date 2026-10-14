# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY GameWithApiASPTemplate/GameWithApiASPTemplate.csproj GameWithApiASPTemplate/
RUN --mount=type=cache,id=nuget-asp-game-template,target=/root/.nuget/packages \
    dotnet restore GameWithApiASPTemplate/GameWithApiASPTemplate.csproj

COPY . .
RUN --mount=type=cache,id=nuget-asp-game-template,target=/root/.nuget/packages \
    dotnet publish GameWithApiASPTemplate/GameWithApiASPTemplate.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

# ASP.NET Data Protection keys survive container recreation when this path is
# backed by the named volume from compose.yaml.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet

USER app
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080' || exit 1

ENTRYPOINT ["dotnet", "GameWithApiASPTemplate.dll"]
