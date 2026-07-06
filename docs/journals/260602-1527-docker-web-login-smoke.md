# Docker web login smoke

## Summary
- Rebuilt Docker web image with latest source and recreated `web`.
- Fixed existing Docker volumes where local `admin` password hash differed from documented `Admin@2026`.
- Fixed typed chatbot client constructor ambiguity that crashed the admin circuit after login when an API key was configured.
- Moved admin route protection to layout/NavGate so hard reload can restore browser session before redirect checks.

## Verification
- `dotnet build .\telemedicine-landing-page.sln -c Release` passed 0 warnings, 0 errors.
- `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` passed 192/192.
- `docker compose up --build -d web` rebuilt and started healthy web container.
- `GET http://localhost:8080/health` returned Healthy for `med-db` and `sqlserver`.
- Playwright smoke: login `admin` reaches `/admin`; reload stays `/admin`; no unhandled circuit exception in logs.

## Unresolved Questions
- None.
