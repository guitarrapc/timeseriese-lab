#!/bin/sh -e

psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -c "SHOW config_file"
# It is necessary to change the parameter max_prepared_transactions to a non-zero value ('150' is recommended).
# https://www.postgresql.org/docs/12/runtime-config-resource.html#max_prepared_transactions
sed -ri "s!^#?(max_prepared_transactions)\s*=.*!\1 = 150!" /var/lib/postgresql/data/postgresql.conf
grep "max_prepared_transactions" /var/lib/postgresql/data/postgresql.conf
# To achieve good query performance you need to enable partitionwise aggregation on the access node. This pushes down aggregation queries to the data nodes.
# https://docs.timescale.com/v2.0/using-timescaledb/distributed-hypertables#select
# https://www.postgresql.org/docs/12/runtime-config-query.html#enable_partitionwise_aggregate
sed -ri "s!^#?(enable_partitionwise_aggregate)\s*=.*!\1 = on!" /var/lib/postgresql/data/postgresql.conf
grep "enable_partitionwise_aggregate" /var/lib/postgresql/data/postgresql.conf
# JIT should be set to off on the access node as JIT currently doesn't work well with distributed queries.
# https://www.postgresql.org/docs/12/runtime-config-query.html#jit
sed -ri "s!^#?(jit)\s*=.*!\1 = off!" /var/lib/postgresql/data/postgresql.conf
grep "jit" /var/lib/postgresql/data/postgresql.conf

echo "Waiting for data nodes..."
until PGPASSWORD=$POSTGRES_PASSWORD psql -h timescaledb-dn01 -d "${POSTGRES_DB:-$POSTGRES_USER}" -U "$POSTGRES_USER" -c '\q'; do
    sleep 5s
done
until PGPASSWORD=$POSTGRES_PASSWORD psql -h timescaledb-dn02 -d "${POSTGRES_DB:-$POSTGRES_USER}" -U "$POSTGRES_USER" -c '\q'; do
    sleep 5s
done

echo "Connect data nodes to cluster and create distributed hypertable..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "${POSTGRES_DB:-$POSTGRES_USER}" <<-EOSQL
SELECT add_data_node('dn01', host => 'timescaledb-dn01', port => 5432, password => '$POSTGRES_PASSWORD', database => '${POSTGRES_DB:-$POSTGRES_USER}');
SELECT add_data_node('dn02', host => 'timescaledb-dn02', port => 5432, password => '$POSTGRES_PASSWORD', database => '${POSTGRES_DB:-$POSTGRES_USER}');
EOSQL
