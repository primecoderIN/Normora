FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files for restoring
COPY ["Normora.Api/Normora.Api.csproj", "Normora.Api/"]
COPY ["Normora.Shared/Normora.Shared.csproj", "Normora.Shared/"]

COPY ["Modules/Normora.Modules.Auth/Normora.Modules.Auth.csproj", "Modules/Normora.Modules.Auth/"]
COPY ["Modules/Normora.Modules.Tenants/Normora.Modules.Tenants.csproj", "Modules/Normora.Modules.Tenants/"]
COPY ["Modules/Normora.Modules.Users/Normora.Modules.Users.csproj", "Modules/Normora.Modules.Users/"]
COPY ["Modules/Normora.Modules.Documents/Normora.Modules.Documents.csproj", "Modules/Normora.Modules.Documents/"]

RUN dotnet restore "Normora.Api/Normora.Api.csproj"

# Copy the rest of the files and build
COPY . .
WORKDIR "/src/Normora.Api"
RUN dotnet build "Normora.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Normora.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Normora.Api.dll"]
