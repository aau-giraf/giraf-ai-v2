Database setup

```postgresql
DROP TABLE imggen_queue;
DROP TABLE tts_queue;

CREATE TYPE id_type AS ENUM ('citizen_id', 'class_id', 'organization_id');
CREATE TYPE language AS ENUM ('da', 'en');

CREATE TABLE "imggen_queue" (
    queue_id    SERIAL PRIMARY KEY NOT NULL,
    prompt      VARCHAR(500) NOT NULL,
    image_id    VARCHAR(100) NOT NULL,
    map_id      INTEGER NOT NULL,
    id_type     id_type NOT NULL,
    origin_app  VARCHAR(10) NOT NULL,
    received    TIMESTAMP NOT NULL
);

CREATE TABLE "tts_queue" (
     prompt      VARCHAR(500) NOT NULL,
     tts_id      INTEGER NOT NULL,
     language    language NOT NULL,
     received    TIMESTAMP NOT NULL
); 
```