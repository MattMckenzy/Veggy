FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG TARGETARCH
ARG CONFIG
WORKDIR /src
COPY . ./  
RUN dotnet restore "Veggy.csproj" -a $TARGETARCH
RUN dotnet publish "Veggy.csproj" -a $TARGETARCH -c $CONFIG -o /app/publish

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG TARGETARCH
WORKDIR /app
EXPOSE 80

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Veggy.dll"]