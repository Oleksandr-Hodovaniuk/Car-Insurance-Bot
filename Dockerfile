FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CarInsuranceBot/CarInsuranceBot.csproj", "src/CarInsuranceBot/"]
COPY ["src/CarInsuranceBot.Application/CarInsuranceBot.Application.csproj", "src/CarInsuranceBot.Application/"]
COPY ["src/CarInsuranceBot.Infrastructure/CarInsuranceBot.Infrastructure.csproj", "src/CarInsuranceBot.Infrastructure/"]
COPY ["src/CarInsuranceBot.Domain/CarInsuranceBot.Domain.csproj", "src/CarInsuranceBot.Domain/"]

RUN dotnet restore "src/CarInsuranceBot/CarInsuranceBot.csproj"

COPY . .

RUN dotnet publish "src/CarInsuranceBot/CarInsuranceBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]