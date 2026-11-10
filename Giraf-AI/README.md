Database setup

```postgresql
CREATE TYPE id_type AS ENUM ('citizen_id', 'class_id', 'organization_id');

CREATE TABLE "imggen_queue" (
    queue_id     		SERIAL PRIMARY KEY NOT NULL,
    prompt         		VARCHAR(500) NOT NULL,
    translated_prompt 	VARCHAR(500),
    image_id    		VARCHAR(100) NOT NULL,
    map_id        		INTEGER NOT NULL,
    id_type        		id_type NOT NULL,
    title_da    		VARCHAR(100),
    title_en    		VARCHAR(100),
    nsfw        		BOOLEAN,
    img_data			VARCHAR(100000),
    received     		TIMESTAMP NOT NULL,
    finished   	 		TIMESTAMP
);

CREATE TABLE "tts_queue" (
    queue_id	SERIAL PRIMARY KEY NOT NULL,
    prompt      VARCHAR(500) NOT NULL,
    received    TIMESTAMP NOT NULL,
    finished    TIMESTAMP
); 
```