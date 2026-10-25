CREATE TABLE telemetry_records (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tag_id integer NOT NULL CHECK (tag_id > 0),
    timestamp timestamptz NOT NULL,
    value double precision NOT NULL,
    quality integer NOT NULL CHECK (quality IN (0, 1))
);

CREATE INDEX idx_telemetry_tag_timestamp
ON telemetry_records (tag_id, timestamp DESC);