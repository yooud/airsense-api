CREATE OR REPLACE FUNCTION delete_sensor_data_on_room_delete()
    RETURNS TRIGGER AS $$
BEGIN
    DELETE FROM sensor_data
    WHERE sensor_id IN (
        SELECT id FROM sensors WHERE room_id = OLD.id
    );
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_delete_sensor_data
    AFTER DELETE ON rooms
    FOR EACH ROW
EXECUTE FUNCTION delete_sensor_data_on_room_delete();

CREATE OR REPLACE FUNCTION delete_device_data_on_room_delete()
    RETURNS TRIGGER AS $$
BEGIN
    DELETE FROM device_data
    WHERE device_id IN (
        SELECT id FROM devices WHERE room_id = OLD.id
    );
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_delete_device_data
    AFTER DELETE ON rooms
    FOR EACH ROW
EXECUTE FUNCTION delete_device_data_on_room_delete();
