# ── Stage 1: build React frontend ───────────────────────────────────────────
FROM node:22-alpine AS frontend
WORKDIR /app
COPY oamswlatifose.client/package*.json ./
RUN npm ci
COPY oamswlatifose.client/ ./
RUN npm run build

# ── Stage 2: build & publish .NET backend ───────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (layer-cached until csproj changes)
COPY oamswlatifose.Server/oamswlatifose.Server.csproj ./oamswlatifose.Server/
RUN dotnet restore ./oamswlatifose.Server/oamswlatifose.Server.csproj

# Copy source, then drop Vite output into wwwroot
COPY oamswlatifose.Server/ ./oamswlatifose.Server/
COPY --from=frontend /app/dist/ ./oamswlatifose.Server/wwwroot/

RUN dotnet publish ./oamswlatifose.Server/oamswlatifose.Server.csproj \
    -c Release -o /publish --no-restore

# ── Stage 3: runtime image ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .

# Railway injects $PORT; fall back to 8080 for local docker run
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=$PORT dotnet oamswlatifose.Server.dll"]
