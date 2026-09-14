# Channel server (Server.Login) of the emulator.
# The build context is the root of the repository: the server is built out of the solution,
# so every project it references is taken from the same tree.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY . .

RUN dotnet publish Servers/Server.Login/Server.Login.csproj \
    --configuration Release \
    --output /app

FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app
COPY --from=build /app .

# Everything that tells this channel where it lives comes from the environment, see compose.yml:
# the address it is registered under in TblParmSvr, the address it binds to and the connection
# strings. Nothing of that is baked into the image
ENTRYPOINT ["dotnet", "Server.Login.dll"]
