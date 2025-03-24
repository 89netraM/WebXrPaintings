FROM node:23-alpine AS ar-viewer-builder
WORKDIR /app
COPY ar-viewer/package.json ar-viewer/package-lock.json ./
RUN npm ci
COPY ar-viewer/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-builder
WORKDIR /app
COPY backend/WebXrPaintings.csproj backend/packages.lock.json ./
RUN dotnet restore --locked-mode
COPY backend/ ./
COPY --from=ar-viewer-builder /app/dist/index.html ./Pages/
RUN cat ./Pages/index.html >> ./Pages/Painting.cshtml && rm ./Pages/index.html
RUN dotnet publish --no-restore --output dist

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=ar-viewer-builder /app/dist/assets/ ./wwwroot/assets/
COPY --from=backend-builder /app/dist/ ./
ENTRYPOINT [ "dotnet", "WebXrPaintings.dll" ]
