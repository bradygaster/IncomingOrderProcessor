# Dockerfile for .NET Framework 4.8.1 Windows Service
# Note: This application requires Windows containers due to .NET Framework and MSMQ dependencies

# Build stage
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY IncomingOrderProcessor/IncomingOrderProcessor.csproj IncomingOrderProcessor/
RUN nuget restore IncomingOrderProcessor/IncomingOrderProcessor.csproj

# Copy remaining source files
COPY IncomingOrderProcessor/ IncomingOrderProcessor/

# Build the application
WORKDIR /src/IncomingOrderProcessor
RUN msbuild IncomingOrderProcessor.csproj /p:Configuration=Release /p:OutputPath=/app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/framework/runtime:4.8 AS runtime
WORKDIR /app

# Enable MSMQ feature in Windows container
RUN powershell -Command \
    Enable-WindowsOptionalFeature -Online -FeatureName MSMQ-Server -All

# Copy build output from build stage
COPY --from=build /app/out .

# Set the entry point to run as console application
# Note: Windows Services need special handling in containers
# This runs the service in console mode for containerization
ENTRYPOINT ["IncomingOrderProcessor.exe"]
