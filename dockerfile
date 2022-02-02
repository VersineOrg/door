FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet add package Newtonsoft.Json
RUN dotnet add package DnsClient
RUN dotnet restore

# copy and publish app and libraries
COPY . .
RUN dotnet publish -c release -o /app --no-cache


# final stage/image
FROM alpine
WORKDIR /app
COPY --from=build /app .
RUN ls -lsa
#ENTRYPOINT ["./"]
#RUN mkdir ./Docker
#ENTRYPOINT ["./Docker"]


