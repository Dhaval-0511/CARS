FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CARS.csproj ./
RUN dotnet restore

# Copy everything else and build the release
COPY . ./
RUN dotnet publish CARS.csproj -c Release -o /app/publish

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create directory for medical record uploads
RUN mkdir -p /app/wwwroot/uploads

# Configure the port the app runs on
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Start the application
ENTRYPOINT ["dotnet", "CARS.dll"]
