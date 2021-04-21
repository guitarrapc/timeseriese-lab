## Getting started

up containers.

```shell
docker-compose up -d
```

migrate & seed initial data.

```shell
cd src/SampleConsole/
dotnet run -- Runner migrate

or

dotnet ef database update
dotnet run -- Runner seedcopy
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

```shell
docker-compose down --remove-orphans --volumes
```

down all containers. (initialize data)

```shell
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

### Add own migrations

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

### re-migrate from beginning

drop database before migrations.

```shell
dotnet ef database drop --force
```

### Disk Usage

**prepare**

```shell
dotnet run -- Runner migrate
dotnet run -- Runner sameseedcopy -count 1000000
dotnet run -- Runner sameseedcopy -count 9000000
dotnet run -- Runner sameseedcopy -count 90000000
```

**check volume size**

```shell
docker-compose exec timescaledb du -hs /var/lib/postgresql/data/
```

```sql
SELECT pg_size_pretty(pg_database_size('timeseriese'))
```

same data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 77 MB | 279.6M
10,000,000 | 678 MB | 1.7G
100,000,000 | 

random data records.

line of insert | sql | df
---- | ---- | ----
Initial | 9165 kB | 51.3M
10,000 | 10 MB | 52.4M
100,000 | 17 MB | 75.7M
1,000,000 | 90 MB | 228.0M
10,000,000 | 
100,000,000 | 

