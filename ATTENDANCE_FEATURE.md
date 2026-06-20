# Schedule-based, OTP-verified attendance

Adds a **schedule-driven, OTP-verified clock-in** with a Google-Cloud-Console-style
**Attendance Monitoring** console (matching the Realstate admin look).

## How it works
1. An employee taps **Time In**. The browser reads its **GPS**; the server checks it against
   the **branch geofences** (Haversine distance ≤ branch radius) → records **Office** (with the
   branch) or **Outside** (off-site). It captures the moment, generates a 6-digit code, stores
   it, and **emails it** (real SMTP) — no attendance is recorded yet.
2. The employee enters the code in the verify dialog. On success the attendance row is
   written using the **tap time** + the resolved location, and graded against their
   **work schedule**: on/before `StartTime + grace` → **Present**, otherwise **Late**
   (default 09:00 + 5 min when no schedule is set).
3. Managers/Admins **set the schedule time** and **define office branches** (centre + radius,
   "use my current location"), and watch the team monitoring table — which shows
   **Office vs Outside** per record.

### Location policy
- Every record is tagged **Office** / **Outside** / **Unknown** (no GPS) — so you can see who
  attended on-site vs off-site. Off-site is **allowed and tagged** by default.
- To make it **office-only** (block off-site clock-ins), set `AttendanceGeofence:RequireOnSite`
  to `true` in `appsettings.json`.
- With no branches defined, clock-ins are recorded as Outside/Unknown (the geofence is opt-in).

## What was added / changed
### Backend (`oamswlatifose.Server`)
- `Model/occurance/EMWorkSchedule.cs`, `EMAttendanceOtp.cs` (+ DbContext config + EF migration
  `*_AddWorkScheduleAndAttendanceOtp`).
- `Services/Schedule/*` (`IWorkScheduleService`) + `Controllers/ScheduleController.cs`
  (`GET /api/schedule/my`, `GET /api/schedule`, `POST /api/schedule`).
- `Services/Attendance/*AttendanceVerificationService*` + two endpoints on `AttendanceController`:
  `POST /api/attendance/clock-in/request-otp` (now takes optional `{latitude,longitude}`) and
  `POST /api/attendance/clock-in/verify`.
- **Branch geofences:** `Model/branches/EMBranch.cs` (centre + radius), `Services/Branch/*`
  (`IBranchService` — CRUD + Haversine `ResolveAsync` + `RequireOnSite`), `BranchController`
  (`GET/POST /api/branch`, `DELETE /api/branch/{id}`). `EMAttendance` + `EMAttendanceOtp` gained
  `WorkLocation` / `BranchId` / `Latitude` / `Longitude` (migration `*_AddBranchGeofenceAndLocation`).
  Config: `AttendanceGeofence:RequireOnSite` (default `false`).
- Fixes required for the flow to work end-to-end:
  - JWT now carries an **`employee_id`** claim (so attendance/schedule resolve the employee).
  - `IOptions<JwtSettings>` is now bound and `appsettings.JwtConfig` keys corrected
    (`AccessTokenExpirationMinutes`/`RefreshTokenExpirationDays`) — previously tokens expired instantly.
  - `EmailService` SMTP options are now bound from `SmtpSettings` / `EmailSettings:Sender`
    (`AddEmailOptions`) — previously no SMTP host was configured.

### Frontend (`oamswlatifose.client`)
- Clean `fetch`-based client (`src/lib/api.js`) through a Vite `/api` proxy (`vite.config.js`).
- GCP dark theme (`src/styles/gcp.css`) + the console (`src/features/attendance/*`):
  Today panel, schedule card, Time-In→OTP modal→Time-Out, metric cards, monitoring table
  (with an **Office/Outside Location** column), and a manager-only team table + schedule editor
  + **branch geofence editor** (`BranchEditor.jsx`). GPS is read via `src/lib/geo.js` at Time-In.

## Run it
```bash
# 1) Start the API on the **https** profile (https://localhost:7105).
#    In Development it auto-applies migrations AND seeds a demo account.
cd oamswlatifose.Server
dotnet run --launch-profile https

# 2) Start the client (proxies /api -> https://localhost:7105)
cd ../oamswlatifose.client
npm install
npm run dev          # http://localhost:5173
```

### Roles & demo logins (Development only)
There are three roles:

| Role  | Who | Capabilities |
|-------|-----|--------------|
| **Admin** | full access | everything below + admin |
| **HR**    | manages people | set schedules, manage branches, view team attendance |
| **User**  | the employee who **clocks in** | clock in/out, view own schedule + own attendance |

On first run, `DbSeeder` creates one login-able account per role, each linked to an employee
with a default schedule (08:00–17:00, 5-min grace), plus an example branch:

| Username | Password   | Role  |
|----------|------------|-------|
| `admin`  | `Demo@123` | Admin |
| `hr`     | `Demo@123` | HR    |
| `user`   | `Demo@123` | User  |

The **OTP email goes to the employee's address**, which defaults to your `SmtpSettings:UserName`
inbox so it's deliverable (each account uses a `+admin`/`+hr`/`+user` alias of that inbox, all
distinct addresses landing in the same mailbox). Override the base with `DevSeed:Email`.

### Console views (sidebar)
- **Monitoring** — today's status + Time-In/Out; Admin/HR also see the team table (Office vs
  Outside per person) and the branch geofence editor.
- **My attendance** — range chips, metric cards (Present/Late/Absent/on-time), and your history.
- **Schedule** — your schedule; Admin/HR also get the schedule editor + every employee's schedule.
- **Users** (Admin/HR only) — add an employee + login account (name, email, username, temporary
  password, role) and list existing accounts. Backed by `POST/GET /api/users` (+ `/api/users/roles`),
  gated on `edit_employees`/`view_employees` so both Admin and HR can add users; the basic User
  role gets 403. The branded logo (`public/logo.svg`) shows in the sidebar and on the login page.

### Notes for a real end-to-end test
- SMTP in `appsettings.json` (`SmtpSettings`, Gmail app password) must be reachable for the OTP email.
- Geofence: clock-ins are tagged **Office** only when inside a branch radius. Edit the seeded
  example branch (or add one with **Use my current location**) so your office matches.
- If your API runs on a different port, set `VITE_API_TARGET` before `npm run dev`.
