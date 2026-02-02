# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY IncomingOrderProcessor/IncomingOrderProcessor.csproj IncomingOrderProcessor/
RUN dotnet restore IncomingOrderProcessor/IncomingOrderProcessor.csproj

# Copy source code and build
COPY IncomingOrderProcessor/ IncomingOrderProcessor/
WORKDIR /src/IncomingOrderProcessor
RUN dotnet build IncomingOrderProcessor.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish IncomingOrderProcessor.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "IncomingOrderProcessor.dll"]
