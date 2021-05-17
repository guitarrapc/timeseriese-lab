## Getting started

up containers.

```shell
# single node timescaledb
run

# if you want launch as multi-node timescaledb use below.
multi
```

migrate & seed initial data.

```shell
cd src/SampleConsole/
dotnet run -- Runner migrate
dotnet run -- Runner seedsensordata

or

dotnet ef database update
dotnet run -- Runner seedsensordata
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
down
```

down all containers. (initialize data)

```shell
init
```

## Access

**pgadmin**

http://localhost:8080

* user/pass: `admin@example.com`/`admin`

**grafana**

http://localhost:3000

* user/pass: `admin`/`admin`

**timescaledb**

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

random data.

```shell
dotnet run -- Runner migrate
dotnet run -- Runner seedcopy -count 1000000
dotnet run -- Runner seedcopy -count 9000000
dotnet run -- Runner seedcopy -count 90000000
```

double base same data.

```shell
dotnet run -- Runner migrate
dotnet run -- Runner sameseedcopy -count 1000000
dotnet run -- Runner sameseedcopy -count 9000000
dotnet run -- Runner sameseedcopy -count 90000000
```

float base same data.

```shell
dotnet run -- Runner migrate
dotnet run -- Runner smallsameseedcopy -count 1000000
dotnet run -- Runner smallsameseedcopy -count 9000000
dotnet run -- Runner smallsameseedcopy -count 90000000
```


**check volume size**

```shell
docker-compose exec timescaledb du -hs /var/lib/postgresql/data/
```

```sql
SELECT pg_size_pretty(pg_database_size('timeseries'))
```

**hypertable disabled**

double base random data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 74 MB | 164.6M
10,000,000 | 660 MB | 1.1G
100,000,000 | 6521 MB | 7.4G

double base same data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 67 MB | 157.1M
10,000,000 | 584 MB | 1.0G
100,000,000 | 5755 MB | 6.7G

float base same data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 59 MB | 133.3M
10,000,000 | 507 MB | 837.2M
100,000,000 | 4987 MB | 5.9G


**hypertable enabled**

double base random data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 114 MB | 332.1M
10,000,000 | 2.2G | 1008 MB
100,000,000 | 9335 MB | 10.2G

double base same data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 77 MB | 279.6M
10,000,000 | 678 MB | 1.7G
100,000,000 | 6693 MB | 7.6G 

float base same data records.

line of insert | sql | df
---- | ---- | ----
1,000,000 | 68 MB | 254.6M
10,000,000 | 600 MB | 1.6G
100,000,000 | 5918 MB | 6.8G



