# EcoEast API (.NET 8)

A clean API foundation mapped to the supplied **existing EcoApp SQL Server schema**.

## Important database safety
- This project does **not** create, drop, migrate, or seed database tables.
- Do not run `EnsureCreated`, `Migrate`, or the old destructive SQL script.
- The connection string targets `EcoApp` on `Okuhle\SQLEXPRESS01`; edit it if your SQL instance differs.
- Back up EcoApp before testing writes.
- Existing Identity users/roles are preserved. Resident registration requires a `Resident` role already present in `AspNetRoles`.

## Run
1. Open `EcoEast.API.csproj` in Visual Studio 2022.
2. Confirm the SQL Server connection string in `appsettings.json`.
3. Replace the JWT key. Prefer User Secrets:
   `dotnet user-secrets init`
   `dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS"`
4. Restore and build:
   `dotnet restore`
   `dotnet build`
5. Run with F5. Swagger is enabled in Development.
6. Test `GET /api/health` first. It should report `database: connected`.

## Included endpoints
- `GET /api/health`
- `GET /api/lookups/locations`
- `GET /api/lookups/service-areas`
- `GET /api/lookups/request-types`
- `GET /api/lookups/report-categories`
- `GET /api/lookups/statuses`
- `GET /api/schedules/areas?suburbId=`
- `GET /api/recycling-guide`
- `POST /api/auth/register-resident`
- `POST /api/auth/login`
- `GET/POST /api/resident/service-requests`
- `GET/POST /api/resident/reports`

## Authentication
Login returns a JWT. Send it as `Authorization: Bearer <token>`.
Only residents can use resident endpoints. Manager, field-worker and IT-admin endpoints are deliberately not exposed yet rather than shipping insecure placeholder actions.

## Schema-specific mapping notes
- `SERVICE_REQUESTS.WS_ID`, `REPORT_SUBMISSIONS.WS_ID`, and `WASTE_COLLECTION_SCHEDULES.WS_ID` reference `WASTESOLUTIONS_SUBURBS.WS_ID`, not `WASTE_SOLUTIONS.WASTE_SOL_ID`.
- `WASTE_COLLECTION_SCHEDULES` is the recurring area schedule table.
- `WASTE_SCHEDULES` is for per-request worker assignments; it is not used for area schedule queries.
- `AspNetUsers.PersonId` has no FK in the supplied schema, so code validates the corresponding `PERSON`/`RESIDENT` records itself.
- Initial status selection currently chooses the lowest `STATUS_ID`. Verify your actual status seed data before production; ideally configure explicit IDs/names for "Pending".
- Photo storage, notification persistence, audit logs, events, drop-off points, manager workflows and worker workflows are not included in this first stable API foundation.

## Before production
Add HTTPS-only deployment, restrictive CORS origins, rate limiting, robust logging, validation, image storage limits/scanning, account recovery, explicit status configuration, and full role-specific authorization. Never expose SQL exception details to public clients.
