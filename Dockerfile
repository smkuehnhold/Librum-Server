FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore && \
    cd src/Presentation && \
    dotnet publish -c Release -o build  --no-restore --verbosity m

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /var/lib/librum-server/

RUN groupadd -r -f librum-server
RUN useradd -r -g librum-server -d /var/lib/librum-server --shell /usr/sbin/nologin librum-server

COPY --from=build /app/src/Presentation/build /var/lib/librum-server/srv
COPY --from=build /app/appsettings.json /var/lib/librum-server/srv/
RUN chmod -R 660 /var/lib/librum-server/ && \
    chmod 770 /var/lib/librum-server && \
    chmod 770 /var/lib/librum-server/srv
COPY --from=build /app/self-hosting/run.sh .

RUN install run.sh -m770 /var/lib/librum-server/srv && \
    rm -f ./run.sh && \
    chown -R librum-server: /var/lib/librum-server/

ENV AdminEmail=admin@example.com
ENV AdminPassword=strongPassword123
ENV CleanUrl=http://0.0.0.0
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV LIBRUM_SELFHOSTED=true

EXPOSE 5000/tcp
EXPOSE 5001/tcp

WORKDIR /var/lib/librum-server/srv
USER librum-server

VOLUME /var/lib/librum-server/librum_storage

ENTRYPOINT /var/lib/librum-server/srv/run.sh
