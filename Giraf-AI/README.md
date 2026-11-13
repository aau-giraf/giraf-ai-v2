Database setup

```postgresql
CREATE TYPE id_type AS ENUM ('citizen_id', 'class_id', 'organization_id');
CREATE TYPE origin_type AS ENUM ('vta', 'fp');

CREATE TABLE "imggen_queue" (
    queue_id    SERIAL PRIMARY KEY NOT NULL,
    prompt      VARCHAR(500) NOT NULL,
    image_id    VARCHAR(100) NOT NULL,
    map_id      INTEGER NOT NULL,
    id_type     id_type NOT NULL,
    received    TIMESTAMP NOT NULL
);

CREATE TABLE "tts_queue" (
    queue_id	SERIAL PRIMARY KEY NOT NULL,
    prompt      VARCHAR(500) NOT NULL,
    tts_id      INTEGER NOT NULL,
    origin      origin_type NOT NULL,
    received    TIMESTAMP NOT NULL
); 
```