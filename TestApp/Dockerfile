FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./

WORKDIR /app/SourceGenerator

# Restore as distinct layers
RUN dotnet restore
# Build and publish the source generator
RUN dotnet publish -c Release -o out 

WORKDIR /app/TestApp

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN ../SourceGenerator/out/SourceGenerator & dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/TestApp/out .
ENTRYPOINT ["dotnet", "TestApp.dll"]