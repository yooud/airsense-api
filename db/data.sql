-- Function to insert sample data into "users"
CREATE OR REPLACE FUNCTION populate_users()
    RETURNS void AS $$
BEGIN
    INSERT INTO "users" ("name", "uid", "email", "notification_token")
    SELECT
        'User ' || i,
        gen_random_uuid()::varchar,
        'user' || i || '@example.com',
        'token_' || i
    FROM generate_series(1, 10) AS i;
END;
$$ LANGUAGE plpgsql;

-- Function to insert sample data into "sensor_types"
CREATE OR REPLACE FUNCTION populate_sensor_types()
    RETURNS void AS $$
BEGIN
    INSERT INTO "sensor_types" ("parameters")
    VALUES
        ('temperature'),
        ('humidity'),
        ('pressure'),
        ('temperature, humidity');
END;
$$ LANGUAGE plpgsql;

-- Function to insert sample data into "sensors"
CREATE OR REPLACE FUNCTION populate_sensors()
    RETURNS void AS $$
BEGIN
    INSERT INTO "sensors" ("serial_number", "room_id", "type_id")
    SELECT
        substr(md5(random()::text), 0, 21),
        (SELECT id FROM "rooms" ORDER BY RANDOM() LIMIT 1),
        type_id
    FROM generate_series(1, 5) AS i,
         (SELECT id AS type_id FROM "sensor_types") AS types;
END;
$$ LANGUAGE plpgsql;

-- Function to insert sample data into "devices"
CREATE OR REPLACE FUNCTION populate_devices()
    RETURNS void AS $$
BEGIN
    INSERT INTO "devices" ("serial_number", "room_id", "active_at")
    SELECT
        substr(md5(random()::text), 0, 21),
        null,
        CURRENT_TIMESTAMP - (i * INTERVAL '1 day')
    FROM generate_series(1, 25) AS i;
END;
$$ LANGUAGE plpgsql;

SELECT populate_users();
SELECT populate_sensor_types();
SELECT populate_sensors();
SELECT populate_devices();
