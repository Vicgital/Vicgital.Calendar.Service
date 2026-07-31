FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra

ARG source=.
WORKDIR /app
EXPOSE 50051
COPY $source .
ENTRYPOINT ["dotnet", "Vicgital.Calendar.Service.dll"]