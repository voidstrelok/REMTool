# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RemTool.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    fonts-dejavu-core \
    fontconfig \
    && fc-cache -fv \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Copiar todas las variantes de DejaVu Sans a LatoFont para que QuestPDF las descubra
RUN mkdir -p /app/LatoFont && cp /usr/share/fonts/truetype/dejavu/DejaVuSans*.ttf /app/LatoFont/

ENV ASPNETCORE_URLS=http://+:5198
EXPOSE 5198

ENTRYPOINT ["dotnet", "RemTool.dll"]
