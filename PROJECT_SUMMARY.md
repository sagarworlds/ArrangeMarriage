# Project Implementation Summary: Marriage Arranger (Matchmaker)

## 📌 Project Overview
The Marriage Arranger application is a comprehensive digital platform designed for professional matchmakers to register candidates, manage profiles, suggest suitable matches, track proposals, and handle fee collections from brides and grooms.

### 🎯 Core Objectives
- **Candidate Management**: End-to-end registration and profile verification.
- **Intelligent Matchmaking**: Preference-based search and proposal workflow.
- **Financial Tracking**: Flexible fee collection models (Groom, Bride, or both) with receipt generation.
- **Lifecycle Tracking**: From initial proposal to meeting scheduling and final marriage success.

---

## 🛠 Technical Architecture

### 1. Backend (.NET API)
Built using **Clean Architecture** to ensure separation of concerns and high maintainability.

- **Domain Layer**: Contains core business entities (`User`, `Profile`, `Proposal`, `Payment`, `Meeting`, `MarriageSuccess`) and domain-specific enums.
- **Application Layer**: Defines the business logic contracts via interfaces (`IUserService`, `IProfileService`, `IMatchmakingService`, etc.).
- **Infrastructure Layer**:
    - **Persistence**: Implemented with `ApplicationDbContext` using **Entity Framework Core** and **PostgreSQL**.
    - **Services**: Concrete implementations of business logic, including a matchmaking algorithm and PDF receipt generation.
- **API Layer**: RESTful endpoints with **Swagger/OpenAPI** integration for testing and documentation.

### 2. Website (Angular)
A responsive web application tailored for three primary user personas.

- **Public Interface**: Landing page featuring services, membership plans, and success stories.
- **Member Portal**: Personalized dashboard for candidates to manage profiles and interact with matches.
- **Admin Portal**: Full-featured management suite for the matchmaker to oversee all operations and revenue.
- **State Management**: Centralized `ApiService` for consistent communication with the backend.

### 3. Mobile App (Flutter)
A cross-platform mobile app ensuring accessibility for users on the go.

- **Candidate Experience**: Focuses on ease of use with a grid-based dashboard, a discovery screen for matches, and a profile management system.
- **Admin Experience**: A high-level control panel providing real-time KPIs and proposal tracking.

---

## 🗄 Database Design
The system utilizes a **PostgreSQL** database. The schema is designed for high performance with optimized indexing on critical search columns (Gender, Religion, Caste).

**Key Tables:**
- `users` & `profiles`: Core identity and personal information.
- `partner_preferences`: Detailed criteria for matching.
- `proposals`: Tracking the state of matches from 'Sent' to 'Finalized'.
- `payments`: Financial records including fee types and transaction IDs.
- `meetings` & `meeting_feedback`: Coordination and post-meeting reviews.
- `marriage_successes`: Final conversion tracking and success fee management.

---

## 🚀 Getting Started

### Prerequisites
- **.NET 10 SDK**
- **Node.js & Angular CLI**
- **Flutter SDK**
- **PostgreSQL**

### Setup Instructions

#### Backend
1. Navigate to `backend/`.
2. Update the connection string in `ArrangeMarriage.API/appsettings.json`.
3. Run the database schema script located in `docs/database_schema.sql`.
4. Execute `dotnet build` followed by `dotnet run --project ArrangeMarriage.API`.

#### Website
1. Navigate to `website/`.
2. Run `npm install`.
3. Launch the app using `ng serve`.

#### Mobile App
1. Navigate to `mobile/`.
2. Run `flutter pub get`.
3. Launch the app using `flutter run`.

---

## 🛣 Roadmap (Phase 2)
- [ ] **AI Recommendations**: Implement ML-based compatibility scoring.
- [ ] **WhatsApp Integration**: Automated notifications and alerts.
- [ ] **Horoscope Matching**: Integration with astrological API services.
- [ ] **Video Calling**: In-app verified video meetings.
- [ ] **Multi-language Support**: English, Hindi, and Marathi localization.
