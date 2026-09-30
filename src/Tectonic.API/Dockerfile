FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /build
COPY src/Tectonic.API/Tectonic.API.csproj ./
RUN dotnet restore Tectonic.API.csproj
COPY src/Tectonic.API/ ./
RUN dotnet publish Tectonic.API.csproj --configuration Release --no-restore \
    --output /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false \
    ConnectionStrings__Expenses="Data Source=/data/hackathon.db" \
    ConnectionStrings__Identity="Data Source=/data/auth.db"
RUN mkdir /data && chown "$APP_UID:$APP_UID" /data
COPY --from=publish /publish/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tectonic.API.dll"]
