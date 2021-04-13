## Step to start

up containers.

```shell
docker-compose up -d
```

migrate & seed initial data.

```shell
dotnet ef database update
dotnet run -- Runner seed
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

## Step to stop

down all containers.
```
docker-compose down --volumes --remove-orphans
```

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
