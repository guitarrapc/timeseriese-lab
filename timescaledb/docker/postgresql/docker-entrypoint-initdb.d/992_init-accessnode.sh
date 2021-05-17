#!/bin/sh -e

psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -c "SHOW config_file"
# To achieve good query performance you need to enable partitionwise aggregation on the access node. This pushes down aggregation queries to the data nodes.
# https://docs.timescale.com/v2.0/using-timescaledb/distributed-hypertables#select
# https://www.postgresql.org/docs/12/runtime-config-query.html#enable_partitionwise_aggregate
sed -ri "s!^#?(enable_partitionwise_aggregate)\s*=.*!\1 = on!" /var/lib/postgresql/data/postgresql.conf
grep "enable_partitionwise_aggregate" /var/lib/postgresql/data/postgresql.conf

echo "Waiting for data nodes..."
until PGPASSWORD=$POSTGRES_PASSWORD psql -h timescaledb-dn01 -U "$POSTGRES_USER" -c '\q'; do
    sleep 5s
done
until PGPASSWORD=$POSTGRES_PASSWORD psql -h timescaledb-dn02 -U "$POSTGRES_USER" -c '\q'; do
    sleep 5s
done

echo "Connect data nodes to cluster and create distributed hypertable..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" <<-EOSQL
SELECT add_data_node('dn01', host => 'timescaledb-dn01', port => 5432, password => '$POSTGRES_PASSWORD', database => '$POSTGRES_DB');
SELECT add_data_node('dn02', host => 'timescaledb-dn02', port => 5432, password => '$POSTGRES_PASSWORD', database => '$POSTGRES_DB');
EOSQL
