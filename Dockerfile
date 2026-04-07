# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["LMS.API/LMS.API.csproj", "LMS.API/"]
COPY ["LMS.Application/LMS.Application.csproj", "LMS.Application/"]
COPY ["LMS.Domain/LMS.Domain.csproj", "LMS.Domain/"]
COPY ["LMS.Infrastructure/LMS.Infrastructure.csproj", "LMS.Infrastructure/"]

RUN dotnet restore "LMS.API/LMS.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the application
WORKDIR "/src/LMS.API"
RUN dotnet publish "LMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the lighter aspnet image for the final runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "LMS.API.dll"]
