FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先複製 csproj 讓 Docker cache restore layer，後續只改程式碼時可以加快 Render build。
COPY TicketFlow.Api.csproj ./
RUN dotnet restore TicketFlow.Api.csproj

COPY . ./
RUN dotnet publish TicketFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# Render 會在 runtime 提供 PORT；用 shell 展開後交給 ASP.NET Core 綁定正確連接埠。
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet TicketFlow.Api.dll"]
