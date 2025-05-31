# Use the official .NET SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY RentalService.csproj ./
RUN dotnet restore RentalService.csproj

# Copy the rest of the source code
COPY . .

# Build and publish the app
RUN dotnet publish RentalService.csproj -c Release -o /app/publish --no-restore

# Use the official ASP.NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Set the entrypoint
ENTRYPOINT ["dotnet", "RentalService.dll"]
