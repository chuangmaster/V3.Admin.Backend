# ===================================================
# 階段 1: 建置階段
# ===================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 複製 csproj 並還原相依性（利用 Docker 快取層）
COPY ["V3.Admin.Backend.csproj", "./"]
RUN dotnet restore "V3.Admin.Backend.csproj"

# 複製其餘檔案並建置專案
COPY . .
RUN dotnet build "V3.Admin.Backend.csproj" -c Release -o /app/build

# ===================================================
# 階段 2: 發佈階段
# ===================================================
FROM build AS publish
RUN dotnet publish "V3.Admin.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ===================================================
# 階段 3: 執行階段
# ===================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 建立非 root 使用者以提高安全性
RUN groupadd -r appuser && useradd -r -g appuser appuser

# 複製發佈的檔案
COPY --from=publish /app/publish .

# 設定日誌目錄權限
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# 切換到非 root 使用者
USER appuser

# 暴露應用程式埠
# 預設 ASP.NET Core 在容器中使用 8080 (HTTP) 和 8081 (HTTPS)
EXPOSE 8080
EXPOSE 8081

# 定義建置時參數（可在 docker build 時覆蓋）
ARG ASPNETCORE_ENVIRONMENT=Production
ARG ASPNETCORE_HTTP_PORTS=8080

# 設定環境變數（可在 docker run 時覆蓋）
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV ASPNETCORE_URLS=http://+:${ASPNETCORE_HTTP_PORTS}
ENV ASPNETCORE_HTTP_PORTS=${ASPNETCORE_HTTP_PORTS}

# 資料庫連線字串（執行時必須提供）
ENV ConnectionStrings__DefaultConnection=""

# JWT 設定（執行時必須提供）
ENV JwtSettings__SecretKey=""
ENV JwtSettings__Issuer="V3.Admin.Backend"
ENV JwtSettings__Audience="V3.Admin.Frontend"
ENV JwtSettings__ExpirationMinutes="60"

# Serilog 日誌層級
ENV Serilog__MinimumLevel__Default="Information"

# 健康檢查（可選）
# HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
#   CMD curl --fail http://localhost:${ASPNETCORE_HTTP_PORTS}/health || exit 1

# 啟動應用程式
ENTRYPOINT ["dotnet", "V3.Admin.Backend.dll"]
