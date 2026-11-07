FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet restore backend/GestaoComercial.Api --locked-mode
RUN dotnet publish backend/GestaoComercial.Api -c Release --no-restore -o /out
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble
WORKDIR /app
COPY --from=build /out .
USER app
ENTRYPOINT ["dotnet", "GestaoComercial.Api.dll"]
