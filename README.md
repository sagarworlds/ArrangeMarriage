# Marriage Arranger (Matchmaker) Platform

[![.NET](https://img.shields.io/badge/.NET-10-informational?logo=.net&logoColor=white&color=512bd4)](https://dotnet.microsoft.com/)
[![Flutter](https://img.shields.io/badge/Flutter-⸱-02569B?logo=flutter)](https://flutter.dev/)
[![Angular](https://img.shields.io/badge/Angular-⸱-DD0031?logo=angular&logoColor=white)](https://angular.io/)
[![Dart](https://img.shields.io/badge/Dart-⸱-0175C2?logo=dart&logoColor=white)](https://dart.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-⸱-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-⸱-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework-Core-512bd4)](https://docs.microsoft.com/ef/)
[![SignalR](https://img.shields.io/badge/SignalR-RealTime-1f425f)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![PdfSharpCore](https://img.shields.io/badge/PdfSharpCore-PDF-lightgrey)](https://github.com/ststeiger/PdfSharpCore)

A comprehensive digital platform for professional matchmakers to register candidates, manage profiles, calculate astrological/compatibility matches, coordinate meetings, and track proposals.

---

## Table of Contents

- [Project Architecture](#-project-architecture)
- [Features](#-features-implemented)
- [Getting Started](#-getting-started)
  - [Prerequisites](#-prerequisites)
  - [Database Setup](#-database-setup)
  - [Backend Startup](#-backend-startup)
  - [Website Startup](#-website-startup)
  - [Mobile App Startup](#-mobile-app-startup)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🏗 Project Architecture

The workspace is divided into three primary components:

1. **Backend (.NET 10 API)**
   - Implemented using Clean Architecture patterns (Domain, Application, Infrastructure, API).
   - Domain entities: `User`, `Profile`, `Proposal`, `Payment`, `Meeting`, `Message`.
   - Infrastructure: Entity Framework Core with PostgreSQL, PDF receipt generation via PdfSharpCore, and simulated third-party services for payments and notifications.
   - API / Hubs: REST controllers and real-time SignalR hub (`ChatHub`) for instant chat communication.

2. **Website (Angular)**
   - Responsive member and admin dashboards built with Angular.
   - Real-time messaging (SignalR client), payments list & gateway emulation, interactive matchmaking grid showing computed compatibility scores and Guna horoscope results.

3. **Mobile App (Flutter)**
   - Cross-platform client dashboard for match discovery and admin proposal monitoring.

---

## 🛠 Features Implemented

- Real-time messaging powered by ASP.NET Core SignalR.
- Smart matchmaking: in-memory compatibility scoring based on education, religion, age, caste, height, and income preferences.
- Astrology matching: deterministic Guna compatibility score calculation (out of 36 points).
- Payment receipts: PDF generation with PdfSharpCore.
- Security: JWT authentication protecting secure routes.

---

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js & npm
- Flutter SDK
- PostgreSQL

### Database Setup

1. Create a PostgreSQL database named `ArrangeMarriageDb`.
2. Execute the SQL initialization script to create tables and enums. (If you keep the `docs/database_schema.sql` file locally, run that script. If not, use EF Core migrations.)

Example (psql):

```bash
createdb ArrangeMarriageDb
psql -d ArrangeMarriageDb -f ./docs/database_schema.sql
```

> Tip: If you prefer migrations, run migrations from the backend project using `dotnet ef database update` after configuring the connection string.

### Backend Startup

1. Navigate to the `backend/` directory.
2. Update the connection string in `ArrangeMarriage.API/appsettings.json` if needed.
3. Restore and build:

```bash
dotnet restore
dotnet build
```

4. Run the API:

```bash
dotnet run --project ArrangeMarriage.API
```

5. Swagger docs: `http://localhost:5000/swagger` (or the URL shown in console).

### Website Startup

1. Navigate to the `website/` directory.
2. Install dependencies and start dev server:

```bash
npm install
npm run start
```

3. Open: `http://localhost:4200`.

### Mobile App Startup

1. Open the `mobile/` (or `app/`) Flutter project directory.
2. Get packages and run:

```bash
flutter pub get
flutter run
```

---

## Contributing

Contributions are welcome. Please open issues for bugs or feature requests and submit PRs for proposed changes. Add unit/integration tests where appropriate and follow the existing code structure.

---

## License

Add your license here (e.g., MIT). If this repository already has a LICENSE file, update this section accordingly.
