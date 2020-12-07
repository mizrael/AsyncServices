#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["AsyncServices.Worker/AsyncServices.Worker.csproj", "AsyncServices.Worker/"]
COPY ["AsyncServices.Common/AsyncServices.Common.csproj", "AsyncServices.Common/"]
RUN dotnet restore "AsyncServices.Worker/AsyncServices.Worker.csproj"
COPY . .
WORKDIR "/src/AsyncServices.Worker"
RUN dotnet build "AsyncServices.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsyncServices.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AsyncServices.Worker.dll"]
