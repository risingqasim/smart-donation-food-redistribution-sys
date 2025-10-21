# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["SmartDonationSystem.csproj", "SmartDonationSystem/"]
RUN dotnet restore "SmartDonationSystem/SmartDonationSystem.csproj"

# Copy all source code
COPY . SmartDonationSystem/
WORKDIR "/src/SmartDonationSystem"

# Build the application
RUN dotnet build "SmartDonationSystem.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SmartDonationSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create the final runtime image
FROM base AS final
WORKDIR /app

# Install necessary packages
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads && \
    chmod 755 /app/wwwroot/uploads

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "SmartDonationSystem.dll"]
