#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ELK.Play/ELK.Play.csproj", "ELK.Play/"]
RUN dotnet restore "ELK.Play/ELK.Play.csproj"
COPY . .
WORKDIR "/src/ELK.Play"
RUN dotnet build "ELK.Play.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ELK.Play.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ELK.Play.dll"]