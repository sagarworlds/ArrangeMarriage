# Marriage Arranger (Matchmaker) Platform

A comprehensive digital platform designed for professional matchmakers to register candidates, manage profiles, calculate astrological/compatibility matches, coordinate meetings, track proposal workflows, and handle payment collections.

---

## 🏗 Project Architecture

The workspace is divided into three primary components:

1. **Backend (.NET 10 API)**
   * Implemented using **Clean Architecture** patterns (Domain, Application, Infrastructure, and API layers).
   * **Domain**: Core entities (`User`, `Profile`, `Proposal`, `Payment`, `Meeting`, `Message`).
   * **Infrastructure**: Database integration using **Entity Framework Core** and **PostgreSQL**, plus concrete implementations for PDF receipt generation (`PdfSharpCore`) and simulated third-party integrations (Stripe/Razorpay, SMS/Email dispatches).
   * **API / Hubs**: REST controllers and real-time **SignalR** connection hub (`ChatHub`) for instant chat communication.

2. **Website (Angular)**
   * Responsive member and admin dashboards built with Angular component frameworks.
   * Features real-time messaging, payments list and gateway emulation, interactive matchmaking grid displaying computed compatibility percentages and Guna horoscope scores.

3. **Mobile App (Flutter)**
   * Cross-platform client dashboard built for matches discovery and admin proposal monitoring.

---

## 🚀 Getting Started

### Prerequisites
* **.NET 10 SDK**
* **Node.js & npm**
* **Flutter SDK**
* **PostgreSQL Database**

### 1. Database Setup
1. Create a PostgreSQL database named `ArrangeMarriageDb`.
2. Execute the initialization script located in [database_schema.sql](file:///d:/Study/ArrangeMarriage/docs/database_schema.sql) to set up tables, indices, and enums.

### 2. Backend Startup
1. Navigate to the `backend/` directory.
2. Update the database connection credentials in `ArrangeMarriage.API/appsettings.json` if necessary.
3. Build the solution and restore dependencies:
   ```bash
   dotnet build
   ```
4. Run the API project:
   ```bash
   dotnet run --project ArrangeMarriage.API
   ```
5. Swagger documentation will be available at `http://localhost:5000/swagger`.

### 3. Website Startup
1. Navigate to the `website/` directory.
2. Install npm dependencies (including the SignalR client package):
   ```bash
   npm install
   ```
3. Run the development server:
   ```bash
   npm run start
   ```
4. Open your browser to `http://localhost:4200`.

---

## 🛠 Features Implemented
* **Real-time Messaging**: Multi-user chat powered by ASP.NET Core SignalR.
* **Smart Matchmaking**: In-memory partner compatibility scoring based on education, religion, age, caste, height, and income preferences.
* **Astrology matching**: Deterministic Guna compatibility score calculation (out of 36 points).
* **Payment Receipts**: Live receipt document generation to professional PDF files via `PdfSharpCore`.
* **Security**: JWT Authentication scheme protecting secure routes.
