# GIRAF AI

GIRAF AI provides HTTP endpoints for image generation and text-to-speech (TTS).

## Requirements

- .NET 10 SDK
- A reachable PostgreSQL server and credentials with permission to create the configured database and its schema
- Node.js, only if you want to run the local mock callback server

## Run the application

Set the `ConnectionStrings:DefaultConnection` connection string in `DataBase/appsettings.json` to point to a PostgreSQL server the application can access.

On startup, the application creates the configured database and full schema if they do not already exist. No manual database or schema setup is needed. This is automatic creation, not a schema migration: it will not update an existing database whose schema is outdated.

From this directory, start the API:

```bash
dotnet run
```

The API listens at <http://localhost:8100>.

### Optional: reset an outdated database schema

Use this only when an existing application database has an outdated schema. **These commands permanently delete the queue tables and their data.** Connect to the configured database, run the SQL below, then restart the application so it can recreate the schema. Back up any data you need first.

```sql
DROP TABLE IF EXISTS imggen_queue;
DROP TABLE IF EXISTS tts_queue;

DROP TYPE IF EXISTS id_type;
DROP TYPE IF EXISTS language;
DROP TYPE IF EXISTS origin_app;
```

This reset assumes no other database objects depend on these types.

## Local mock callback server

The mock server accepts generated media callbacks and saves the callback JSON and media files under `~/Documents/generations/`. Start it from this directory in a separate terminal if you need to test or capture those callbacks:

```bash
node MockServer/server.js
```

It listens on port `4500`.

## API

Send JSON `POST` requests to:

| Purpose | Endpoint |
| --- | --- |
| Image generation | `http://localhost:8100/api/image-generation` |
| Text to speech | `http://localhost:8100/api/tts` |

### Image generation request

```json
{
  "prompt": "madpakke med en sandwich",
  "MapID": 4202,
  "idType": "CitizenId",
  "imgID": "57201",
  "OriginApp": "fp"
}
```

| Property | Type | Required | Allowed values / constraints |
| --- | --- | --- | --- |
| `Prompt` | String | Yes | Up to 500 characters |
| `MapID` | Integer | Yes | Positive integer |
| `IdType` | String | Yes | `CitizenID`, `ClassID`, or `OrganizationID` (case-insensitive) |
| `ImgID` | String | Yes | GUID |
| `OriginApp` | String | Yes | `fp` (Foodplanner) or `vta` (case-insensitive) |

### Text-to-speech request

```json
{
  "text": "En melon der er skåret ud i både",
  "ttsID": "12",
  "lang": "da",
  "originApp": "fp"
}
```

| Property | Type | Required | Allowed values / constraints |
| --- | --- | --- | --- |
| `Text` | String | Yes | Up to 500 characters |
| `TtsID` | String | Yes | GUID |
| `Lang` | String | No | `da` (Danish, default when omitted or blank) or `en` (English) |
| `OriginApp` | String | Yes | `fp` or `vta` |
