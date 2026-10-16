# Multi-stage Dockerfile for LoanLens API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching layer restore
COPY ["src/LoanLens.Domain/LoanLens.Domain.csproj", "src/LoanLens.Domain/"]
COPY ["src/LoanLens.Application/LoanLens.Application.csproj", "src/LoanLens.Application/"]
COPY ["src/LoanLens.Infrastructure/LoanLens.Infrastructure.csproj", "src/LoanLens.Infrastructure/"]
COPY ["src/LoanLens.Api/LoanLens.Api.csproj", "src/LoanLens.Api/"]
COPY ["tests/LoanLens.Domain.Tests/LoanLens.Domain.Tests.csproj", "tests/LoanLens.Domain.Tests/"]

# Restore dependencies
RUN dotnet restore "src/LoanLens.Api/LoanLens.Api.csproj"

# Copy all source files
COPY . .

# Run unit tests during container build to guarantee release quality
WORKDIR /src/tests/LoanLens.Domain.Tests
RUN dotnet test -c Release --no-restore

# Publish API
WORKDIR /src/src/LoanLens.Api
RUN dotnet publish "LoanLens.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5257

ENV ASPNETCORE_URLS=http://+:5257
ENV DOTNET_gcServer=0

# Create data directory for SQLite database persistence
RUN mkdir -p /app/data && chown -R app:app /app

USER app
COPY --from=build --chown=app:app /app/publish .

ENTRYPOINT ["dotnet", "LoanLens.Api.dll"]
