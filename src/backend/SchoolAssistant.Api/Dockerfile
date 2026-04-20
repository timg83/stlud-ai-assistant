FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SchoolAssistant.Api.csproj ./
RUN dotnet restore SchoolAssistant.Api.csproj

COPY . ./
RUN dotnet publish SchoolAssistant.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "SchoolAssistant.Api.dll"]