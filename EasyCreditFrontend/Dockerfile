FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EasyCreditFrontend/EasyCreditFrontend.csproj", "EasyCreditFrontend/"]
RUN dotnet restore "EasyCreditFrontend/EasyCreditFrontend.csproj"

COPY EasyCreditFrontend/ EasyCreditFrontend/
WORKDIR "/src/EasyCreditFrontend"
RUN dotnet publish "EasyCreditFrontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5019

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EasyCreditFrontend.dll"]

