
up databases.

```shell
docker run -d --name timescaledb -p 5432:5432 -e POSTGRES_PASSWORD=password timescale/timescaledb:2.1.0-pg13
docker exec -it timescaledb psql -U postgres
```

drop database before migrations.

```shell
dotnet ef database drop --force
```

create migrations

```shell
dotnet ef migrations add InitialCreate
```

migrate & seed initial data.

```shell
dotnet ef database update
dotnet run -- Runner seed
```

## TIPS

**Add own migrations**

If you change any Table change, add your migration.

```shell
dotnet ef migrations add <NAME_OF_MIGRATION>
dotnet ef database update
```
