

To run the application you need to set up a PostgreSQL database.

Then you need to run the following command inside the terminal to run the application:

```dotnet run```

To receive the results and save them on your local maching you need to run the mock server in a seperate terminal:

```node MockServer/server.js```

This will save both the requests and the attached files in your /%USER%/documents/generations/ folder

**Database setup**

You need to name the database ```GIRAF_AI_DB``` with the password ```SQLvmDBaccess```

To set up the database, run the following SQL commands:

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
     origin_app  VARCHAR(10) NOT NULL,
     received    TIMESTAMP NOT NULL
); 
```

To send post requests to the application you can use Postman or any other tool that can send HTTP requests. The endpoints are as follows:

**Image generation endpoint**

```POST localhost:8100/api/image-generation```

**Text to speech endpoint**

```POST localhost:8100/api/tts```

**image generation request body example**

```json
{
    "Prompt": "madpakke med en sandwich",
    "MapID": 4202,
    "IdType": "CitizenID",
    "ImgID": "57201",
    "OriginApp": "fp"
}
```

**text to speech request body example**

```json
{
    "text": "En melon der er skåret ud i både",
    "ttsID": 12,
    "lang": "da",
    "originApp": "fp"
}
```