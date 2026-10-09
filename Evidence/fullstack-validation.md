# Full stack validation — 2026-10-09

This report describes the completed React frontend and API integration. The other evidence files record the earlier backend delivery.

| Check | Result |
| --- | --- |
| .NET 8 Release build | Passed, zero warnings and errors |
| Backend tests | 105 passed, 0 failed, 0 skipped |
| Pure backend unit tests | 46 passed |
| SQLite API/service integration tests | 56 passed |
| SQL Server 2022 concurrency/migration tests | 3 passed |
| Frontend unit/component tests | 15 passed |
| Frontend clean npm ci, type check and production build | Passed |
| npm audit | 0 reported vulnerabilities |
| Browser end-to-end suites against Vite/API/SQL Server | 2 passed |
| Browser end-to-end suites against Docker combined app/SQL Server | 2 passed |
| EF model alignment | No pending model changes |
| Combined application health | HTTP 200, database ready |
| SPA direct /requests route | HTTP 200, index document |
| Unknown /api route | HTTP 404 |
| Compose application restart | Preserved records and account access |

Backend validation used an isolated SQL Server container and unique automated databases. The user's database was not connected to, reset, or deleted. No migrations were changed for the frontend integration.

The first browser suite creates a department and employee, assigns the demo manager, checks audit availability and forbidden navigation, submits two working days, approves them as the manager, and verifies 19 annual days remain. It also checks mobile navigation and the profile. The second suite creates and edits a leave policy, cancels a request, rejects another with a reason, verifies all reserved days return, and deactivates the policy without deleting its history.

Frontend tests cover working days, JWT Identity claims, role restrictions, validation messages, refresh sharing across concurrent calls, rejected sessions, forbidden mutations, and logout during refresh.

The Dockerfile was built successfully and Compose started a separate persistent development SQL Server plus the compiled frontend/API application. This cloud uses an HTTPS inspection proxy: the successful build supplied its existing trusted CA through the optional proxy_ca BuildKit secret and resolved the proxy hostname with --add-host. Certificate verification remained enabled. Those environment-specific options are not needed on a normal Docker Desktop installation.

Reproduction commands are documented in README.md. Playwright tests create their own development records and require explicitly seeded, isolated development services. They do not clean up or delete database records.
