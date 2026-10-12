FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Digital Krushi Officer.csproj", "./"]
RUN dotnet restore "Digital Krushi Officer.csproj"
COPY . .
RUN dotnet publish "Digital Krushi Officer.csproj" -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Digital Krushi Officer.dll"]
