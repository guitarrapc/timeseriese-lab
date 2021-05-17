#!/bin/sh -e

psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" <<-EOSQL
CREATE TABLE telemetries
(
    imei        TEXT                     NOT NULL,
    time        TIMESTAMPTZ              NOT NULL,
    latitude    DOUBLE PRECISION         NOT NULL,
    longitude   DOUBLE PRECISION         NOT NULL,
    geography   GEOGRAPHY(POINT, 4326)   NOT NULL,
    speed       SMALLINT                 NOT NULL,
    course      SMALLINT                 NOT NULL,
    CONSTRAINT telemetries_pkey PRIMARY KEY (imei, time)
);
SELECT * FROM create_distributed_hypertable(
    'telemetries', 'time', 'imei',
    number_partitions => 2, chunk_time_interval => INTERVAL '7 days', replication_factor => 1
);
--SELECT * FROM set_number_partitions('telemetries', 2, 'imei');
--SELECT * FROM set_chunk_time_interval('telemetries', INTERVAL '7 days');
--SELECT * FROM set_replication_factor('telemetries', 1);
EOSQL
