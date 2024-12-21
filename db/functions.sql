CREATE OR REPLACE FUNCTION delete_sensor_data_on_room_null()
    RETURNS TRIGGER AS $$
BEGIN
    DELETE FROM sensor_data
    WHERE sensor_id IN (
        SELECT id FROM sensors WHERE room_id IS NULL
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_delete_sensor_data_on_room_null
    AFTER UPDATE OF room_id ON sensors
    FOR EACH ROW
    WHEN (NEW.room_id IS NULL)
EXECUTE FUNCTION delete_sensor_data_on_room_null();

CREATE OR REPLACE FUNCTION delete_device_data_on_room_null()
    RETURNS TRIGGER AS $$
BEGIN
    DELETE FROM device_data
    WHERE device_id IN (
        SELECT id FROM devices WHERE room_id IS NULL
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_delete_device_data_on_room_null
    AFTER UPDATE OF room_id ON devices
    FOR EACH ROW
    WHEN (NEW.room_id IS NULL)
EXECUTE FUNCTION delete_device_data_on_room_null();
