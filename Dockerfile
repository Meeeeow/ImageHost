FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build-env
# The "environment" in docker-compose.yml only applied to runtime
# It need manual set during build process
ARG MYSQL_PWD
ENV ASPNETCORE_MYSQL_PWD=$MYSQL_PWD
ENV ASPNETCORE_ENVIRONMENT=production
WORKDIR /app
# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
# Temporary fix for https://github.com/aspnet/EntityFrameworkCore/issues/18977
RUN dotnet tool install --global dotnet-ef --version 3.0
RUN PATH="$PATH:/root/.dotnet/tools" dotnet ef database update

RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
RUN apt update && apt install -y libc6-dev libgdiplus
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "ImageHost.dll"]