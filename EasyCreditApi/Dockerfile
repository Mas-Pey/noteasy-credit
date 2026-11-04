FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EasyCreditApi/EasyCreditApi.csproj", "EasyCreditApi/"]
RUN dotnet restore "EasyCreditApi/EasyCreditApi.csproj"

COPY EasyCreditApi/ EasyCreditApi/
WORKDIR "/src/EasyCreditApi"
RUN dotnet publish "EasyCreditApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5052

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EasyCreditApi.dll"]

