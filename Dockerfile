FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Mandamentos.Web/Mandamentos.Web.csproj", "src/Mandamentos.Web/"]
RUN dotnet restore "src/Mandamentos.Web/Mandamentos.Web.csproj"

COPY src/Mandamentos.Web/ src/Mandamentos.Web/
WORKDIR /src/src/Mandamentos.Web
RUN dotnet publish "Mandamentos.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Mandamentos.Web.dll"]
