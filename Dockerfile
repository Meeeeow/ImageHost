FROM microsoft/dotnet:sdk AS build-env
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
RUN dotnet ef database update
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
RUN apt update && apt install -y libc6-dev libgdiplus
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "ImageHost.dll"]