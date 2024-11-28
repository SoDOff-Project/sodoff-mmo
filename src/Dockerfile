# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /src

# Copy the source code
COPY . .

# Explicitely Install Web SDK
RUN dotnet add-global package Microsoft.NET.Sdk.Web

# Restore Project
RUN dotnet build -c Release -o /app

# Create clean run environment (without source and sdk)
# FROM mcr.microsoft.com/dotnet/aspnet:6.0
# WORKDIR /app
# COPY --from=build /app .

# Run the application
WORKDIR /app
ENTRYPOINT ["./sodoffmmo"]
