## Getting started

up containers.

```shell
docker-compose up -d
```

migrate & seed initial data.

```shell
dotnet run -- Runner migrate

or

dotnet ef database update
dotnet run -- Runner seed
```

add random test data for past 100sec.

```shell
dotnet run -- Runner random -count 100
```

keep insert random data.

```shell
dotnet run -- Runner keep
```

add ondemand test data.

```shell
dotnet run -- Runner test -location オフィス -temperature 20.1 -humidity 50.0
```

**clean up**

down all containers. (keep data)

```
docker-compose down --remove-orphans --volumes
```

down all containers. (initialize data)

```
docker-compose down --remove-orphans
```

## Access

**pgadmin**

http://localhost:8080

* user/pass: `admin@example.com`/`admin`

**grafana**

http://localhost:3000

* user/pass: `admin`/`admin`

**postgresql**

localhost:5432
or
timescaledb:5432 (from container)

* user/pass: `postres`/`password`


## TIPS

**Add own migrations**

If you change any Table change, add your migration.

```shell
dotnet ef migrations add <NAME_OF_MIGRATION>
dotnet ef database update
```

samples

create migrations

```shell
dotnet ef migrations add InitialCreate
```

**re-migrate from beginning**

drop database before migrations.

```shell
dotnet ef database drop --force
```
