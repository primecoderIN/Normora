FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files for restoring
COPY ["Normora.Api/Normora.Api.csproj", "Normora.Api/"]
COPY ["Normora.Shared/Normora.Shared.csproj", "Normora.Shared/"]

COPY ["Modules/Auth/Auth.csproj", "Modules/Auth/"]
COPY ["Modules/Conversations/Conversations.csproj", "Modules/Conversations/"]
COPY ["Modules/Documents/Documents.csproj", "Modules/Documents/"]
COPY ["Modules/Tenants/Tenants.csproj", "Modules/Tenants/"]
COPY ["Modules/Users/Users.csproj", "Modules/Users/"]

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
