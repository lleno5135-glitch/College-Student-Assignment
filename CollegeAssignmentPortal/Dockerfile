FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CollegeAssignmentPortal.csproj", "./"]
RUN dotnet restore "CollegeAssignmentPortal.csproj"
COPY . .
RUN dotnet publish "CollegeAssignmentPortal.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CollegeAssignmentPortal.dll"]
