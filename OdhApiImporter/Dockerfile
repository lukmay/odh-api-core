FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY DataModel/DataModel.csproj DataModel/
COPY RawQueryParser/RawQueryParser.fsproj RawQueryParser/
COPY Helper/Helper.csproj Helper/
COPY EBMS/EBMS.csproj EBMS/
COPY DSS/DSS.csproj DSS/
COPY MSS/MSS.csproj MSS/
COPY LCS/LCS.csproj LCS/
COPY SIAG/SIAG.csproj SIAG/
COPY CDB/CDB.csproj CDB/
COPY SuedtirolWein/SuedtirolWein.csproj SuedtirolWein/
COPY NINJA/NINJA.csproj NINJA/
COPY RAVEN/RAVEN.csproj RAVEN/
COPY STA/STA.csproj STA/
COPY OdhApiImporter/OdhApiImporter.csproj OdhApiImporter/
RUN dotnet restore OdhApiImporter/OdhApiImporter.csproj
COPY . .
WORKDIR /src/OdhApiImporter
RUN dotnet build OdhApiImporter.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish OdhApiImporter.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY OdhApiImporter/xml/Weather.xml xml/
COPY OdhApiImporter/xml/STACategories.xml xml/
COPY OdhApiImporter/xml/BezirkWeather.xml xml/
#COPY OdhApiImporter/xml/RequestInterceptor.xml xml/

ENTRYPOINT ["dotnet", "OdhApiImporter.dll"]
