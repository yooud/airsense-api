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

CREATE OR REPLACE FUNCTION populate_parameters()
    RETURNS void AS $$
BEGIN
    INSERT INTO "parameters" ("name", "unit", "min_value", "max_value")
    VALUES
        ('temperature', '°C', -50, 50),
        ('humidity', '%', 0, 100),
        ('pressure', 'hPa', 300, 1100);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION populate_sensor_types()
    RETURNS void AS $$
BEGIN
    INSERT INTO "sensor_types" ("name")
    VALUES
        ('Temperature Sensor'),
        ('Humidity Sensor'),
        ('Pressure Sensor'),
        ('Temperature and Humidity Sensor');
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION populate_sensor_type_parameters()
    RETURNS void AS $$
BEGIN
    INSERT INTO "sensor_type_parameters" ("type_id", "parameter_id")
    VALUES
        (1,1),
        (2,2),
        (3,3),
        (4, 1),
        (4, 2);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION populate_sensors()
    RETURNS void AS $$
BEGIN
    INSERT INTO "sensors" ("serial_number", "type_id", "secret")
    SELECT s.serial,
           s.type_id,
           md5(s.serial || s.serial)
    FROM (
         SELECT
             substr(md5(random()::text), 0, 21) AS serial,
             type_id
         FROM generate_series(1, 5) AS i,
              (SELECT id AS type_id FROM "sensor_types") AS types
    ) s;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION populate_devices()
    RETURNS void AS $$
BEGIN
    INSERT INTO "devices" ("serial_number", "secret")
    SELECT s.serial,
           md5(s.serial || s.serial)
    FROM (
         SELECT
             substr(md5(random()::text), 0, 21) AS serial
         FROM generate_series(1, 25) AS i
    ) s;
END;
$$ LANGUAGE plpgsql;

SELECT populate_users();
SELECT populate_parameters();
SELECT populate_sensor_types();
SELECT populate_sensor_type_parameters();
SELECT populate_sensors();
SELECT populate_devices();
