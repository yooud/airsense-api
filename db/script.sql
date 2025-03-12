CREATE TABLE "users" (
                         "id" serial PRIMARY KEY,
                         "name" varchar NOT NULL,
                         "uid" varchar UNIQUE NOT NULL,
                         "email" varchar(100) UNIQUE NOT NULL,
                         "notification_token" varchar,
                         "created_at" timestamp NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

CREATE TABLE "environments" (
                                "id" serial PRIMARY KEY,
                                "name" varchar NOT NULL
);

CREATE TABLE "rooms" (
                         "id" serial PRIMARY KEY,
                         "name" varchar NOT NULL,
                         "environment_id" int NOT NULL
);

CREATE TABLE "settings" (
                            "room_id" int NOT NULL,
                            "parameter_id" int NOT NULL,
                            "curve" json NOT NULL,
                            PRIMARY KEY ("room_id", "parameter_id")
);

CREATE TABLE "sensors" (
                           "id" serial PRIMARY KEY,
                           "serial_number" varchar NOT NULL,
                           "room_id" int,
                           "type_id" int NOT NULL
);

CREATE TABLE "sensor_types" (
                                "id" serial PRIMARY KEY,
                                "name" varchar NOT NULL
);

CREATE TABLE "environment_members" (
                                       "member_id" int NOT NULL,
                                       "environment_id" int NOT NULL,
                                       "role" varchar,
                                       PRIMARY KEY ("member_id", "environment_id")
);

CREATE TABLE "devices" (
                           "id" serial PRIMARY KEY,
                           "serial_number" varchar NOT NULL,
                           "room_id" int
);

CREATE TABLE "device_data" (
                               "id" serial PRIMARY KEY,
                               "device_id" int NOT NULL,
                               "timestamp" timestamp NOT NULL DEFAULT (CURRENT_TIMESTAMP),
                               "value" real NOT NULL,
                               "applied" bool NOT NULL DEFAULT FALSE,
                               "applied_at" timestamp
);

CREATE TABLE "sensor_data" (
                               "id" serial PRIMARY KEY,
                               "sensor_id" int NOT NULL,
                               "timestamp" timestamp NOT NULL DEFAULT (CURRENT_TIMESTAMP),
                               "parameter_id" int NOT NULL,
                               "value" real NOT NULL,
                               "sent_at" timestamp NOT NULL
);

CREATE TABLE "parameters" (
                              "id" serial PRIMARY KEY,
                              "name" varchar NOT NULL,
                              "unit" varchar NOT NULL,
                              "min_value" real NOT NULL,
                              "max_value" real NOT NULL
);

CREATE TABLE "sensor_type_parameters" (
                                   "type_id" int NOT NULL,
                                   "parameter_id" int NOT NULL
);

ALTER TABLE "rooms" ADD FOREIGN KEY ("environment_id") REFERENCES "environments" ("id") ON DELETE CASCADE;

ALTER TABLE "settings" ADD FOREIGN KEY ("room_id") REFERENCES "rooms" ("id") ON DELETE CASCADE;

ALTER TABLE "settings" ADD FOREIGN KEY ("parameter_id") REFERENCES "parameters" ("id") ON DELETE CASCADE;

ALTER TABLE "sensors" ADD FOREIGN KEY ("room_id") REFERENCES "rooms" ("id") ON DELETE SET NULL;

ALTER TABLE "sensors" ADD FOREIGN KEY ("type_id") REFERENCES "sensor_types" ("id");

ALTER TABLE "environment_members" ADD FOREIGN KEY ("member_id") REFERENCES "users" ("id") ON DELETE CASCADE;

ALTER TABLE "environment_members" ADD FOREIGN KEY ("environment_id") REFERENCES "environments" ("id") ON DELETE CASCADE;

ALTER TABLE "devices" ADD FOREIGN KEY ("room_id") REFERENCES "rooms" ("id") ON DELETE SET NULL;

ALTER TABLE "device_data" ADD FOREIGN KEY ("device_id") REFERENCES "devices" ("id") ON DELETE CASCADE;

ALTER TABLE "sensor_data" ADD FOREIGN KEY ("sensor_id") REFERENCES "sensors" ("id") ON DELETE CASCADE;

ALTER TABLE "sensor_data" ADD FOREIGN KEY ("parameter_id") REFERENCES "parameters" ("id") ON DELETE CASCADE;

ALTER TABLE "sensor_type_parameters" ADD FOREIGN KEY ("type_id") REFERENCES "sensor_types" ("id") ON DELETE CASCADE;

ALTER TABLE "sensor_type_parameters" ADD FOREIGN KEY ("parameter_id") REFERENCES "parameters" ("id");
