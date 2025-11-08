# Etapa 1 — Build do projeto
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia tudo e faz o build
COPY . .
RUN dotnet publish -c Release -o out

# Etapa 2 — Runtime (executar a API)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HeroisCidadaniaWS.dll"]
