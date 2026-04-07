# Stage 1: Preparation - Isolate project files for restoration caching
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS prep
WORKDIR /src
COPY . .
# Keep only files critical for 'dotnet restore' (solution, projects, and CPM config).
# This ensures that the restore layer is only invalidated when project structure or dependencies change.
RUN find . -type f \
    -not -name "*.sln" \
    -not -name "*.csproj" \
    -not -name "Directory.Packages.props" \
    -not -name "Directory.Build.props" \
    -not -name "global.json" \
    -delete

# Stage 2: Restore - Dependencies are restored using only isolated project files
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY --from=prep /src ./
RUN dotnet restore "hoppyfy.me.sln"

# Stage 3: Publish - Bring in full source and publish the app
# The 'COPY . .' here brings in all source files (respecting .dockerignore)
COPY . .
WORKDIR "/src/API.Web"
RUN dotnet publish "API.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final - Production runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Port 8080 is the default for ASP.NET 8.0+ in containers
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.Web.dll"]
