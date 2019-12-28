FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["PTZ.PhotoOrder/PTZ.PhotoOrder.csproj", "PTZ.PhotoOrder/"]
RUN dotnet restore "PTZ.PhotoOrder/PTZ.PhotoOrder.csproj"
COPY . .
WORKDIR "/src/PTZ.PhotoOrder"
RUN dotnet build "PTZ.PhotoOrder.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "PTZ.PhotoOrder.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PTZ.PhotoOrder.dll"]