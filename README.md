# giraf-ai-v2
New and Improved version of the giraf-ai - now written in C#

## Database configuration

The image-generation endpoint writes requests to `public.imggen_queue`. Configure
the database connection before starting the API:

```powershell
$env:ConnectionStrings__GirafAiDatabase = "Host=localhost;Port=5432;Database=GIRAF_AI_DB;Username=postgres;Password=SQLvmDBaccess"
```

The endpoint expects `Prompt`, `ImgID`, `MapID`, and `Type` (`CitizenID`,
`ClassID`, or `OrganizationID`) in the request body.
