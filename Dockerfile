FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Projetar.Web/Projetar.Web.csproj", "src/Projetar.Web/"]
RUN dotnet restore "src/Projetar.Web/Projetar.Web.csproj"

COPY src/Projetar.Web/ src/Projetar.Web/
WORKDIR /src/src/Projetar.Web
RUN dotnet publish "Projetar.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Projetar.Web.dll"]
