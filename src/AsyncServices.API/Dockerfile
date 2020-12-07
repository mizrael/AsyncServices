#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
COPY ["AsyncServices.Common/AsyncServices.Common.csproj", "AsyncServices.Common/"]
COPY ["AsyncServices.API/AsyncServices.API.csproj", "AsyncServices.API/"]
RUN dotnet restore "AsyncServices.API/AsyncServices.API.csproj"
COPY . .
RUN dotnet build "AsyncServices.API/AsyncServices.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsyncServices.API/AsyncServices.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AsyncServices.API.dll"]
