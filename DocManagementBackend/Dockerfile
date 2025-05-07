# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["DocManagementBackend.csproj", "./"]
RUN dotnet restore "./DocManagementBackend.csproj"

# Copy the rest of the code and publish the app in Release mode
COPY . .
RUN dotnet publish "DocManagementBackend.csproj" -c Release -o /app/publish

# Stage 2: Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port that the app listens on (adjust if needed)
EXPOSE 80
EXPOSE 443

# Define the entry point
ENTRYPOINT ["dotnet", "DocManagementBackend.dll"]
