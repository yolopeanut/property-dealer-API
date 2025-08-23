# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Stage 1: Build the application
# Use the official .NET SDK image on Linux for building. It's more portable and standard.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file(s) and restore dependencies first.
# This leverages Docker layer caching. The restore layer is only invalidated
# when the .csproj file changes, not on every code change.
COPY ["property-dealer-API/property-dealer-API.csproj", "property-dealer-API/"]
RUN dotnet restore "property-dealer-API/property-dealer-API.csproj"

# Copy the rest of the source code.
# Use a .dockerignore file to avoid copying bin/obj/etc.
COPY . .
WORKDIR "/src/property-dealer-API"

# Publish the application. The 'publish' command also builds the project.
# The output is placed in /app/publish.
RUN dotnet publish "property-dealer-API.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# Stage 2: Create the final, small runtime image
# Use the lightweight ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create a non-root user for security
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Set the entrypoint to run the application
ENTRYPOINT ["dotnet", "property-dealer-API.dll"]