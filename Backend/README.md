# Neura Backend

Neura is a cutting-edge, high-performance Learning Management System (LMS) and community platform built with **.NET 10**. It provides a robust foundation for e-learning with integrated real-time community engagement, automated proctored exams, and a modern micro-feature architecture.

---

## 🚀 Key Features

### 🎓 E-Learning Management
- **Course Lifecycle:** Create and manage courses with sections and lessons.
- **Content Delivery:** Support for various lesson types and rich-text content (sanitized via `HtmlSanitizer`).
- **Progress Tracking:** Automated tracking of lesson completions and course progress.
- **Prerequisites & Outcomes:** Define learning paths with mandatory prerequisites and clear outcomes.
- **Invitations:** Robust system for inviting users to courses with role-based assignments.

### 📝 Advanced Exam Engine
- **Proctored Exams:** Real-time monitoring of exam attempts with violation tracking (e.g., tab switching detection).
- **Automated Grading:** Instant results for objective questions (multiple choice, etc.) via `GradingService`.
- **Exam Analytics:** Detailed performance metrics for both students and instructors.
- **Timeout Management:** Background processing of expired exam attempts using Hangfire.

### 💬 Community & Engagement
- **Real-time Chat:** Persistent messaging channels powered by **SignalR** with Redis backplane.
- **Presence Tracking:** Live "Who's online" tracking within course communities.
- **Social Features:** Discussion posts, threaded comments, and likes.
- **Resource Sharing:** File uploads managed via **Cloudinary**.

### 🛡️ Enterprise-Grade Infrastructure
- **Security:** Granular Role-Based Access Control (RBAC) and course-specific permissions.
- **Authentication:** Identity-based auth with JWT support and OAuth (Google & GitHub).
- **Webhooks:** Extensible webhook system for external integrations (e.g., Stripe, custom automation).
- **Background Jobs:** Robust task scheduling with **Hangfire**.
- **Performance:** Optimized queries with EF Core Query Splitting and **HybridCache**.

---

## 🛠️ Tech Stack

- **Framework:** .NET 10 (ASP.NET Core Web API)
- **Database:** SQL Server (Entity Framework Core)
- **Real-time:** SignalR (with Redis Backplane)
- **Background Processing:** Hangfire
- **Caching:** HybridCache & Redis
- **File Storage:** Cloudinary
- **Messaging/Workflow:** MediatR (Vertical Slices / Features pattern)
- **Validation:** FluentValidation
- **API Documentation:** Scalar & Swagger (OpenAPI)
- **Logging:** Serilog

---

## 🏗️ Architecture

Neura follows a **Feature-based / Vertical Slice Architecture** to ensure high maintainability and scalability:

- **Neura.Api:** The entry point, containing controllers, minimal endpoints, and infrastructure configuration.
- **Neura.Core:** The heart of the system, housing Domain Entities, Abstractions (Result pattern), Enums, and Contracts.
- **Neura.Repository:** Persistence layer implementing the Data Context and Migrations.
- **Neura.Services:** Business logic implementation, background jobs, and hub definitions.

---

## 🚦 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Redis](https://redis.io/download) (for SignalR Backplane and Caching)
- [Cloudinary Account](https://cloudinary.com/) (for media management)

### Configuration
Create an `appsettings.json` (or use User Secrets) with the following structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=Neura;...",
    "HangfireConnection": "Server=...;Database=Neura_Hangfire;...",
    "Redis": "localhost:6379"
  },
  "JwtOptions": {
    "Key": "your-secret-key",
    "Issuer": "Neura",
    "Audience": "Neura-Clients"
  },
  "CloudinarySettings": {
    "CloudName": "...",
    "ApiKey": "...",
    "ApiSecret": "..."
  },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  }
}
```

### Setup & Run
1. **Clone the repository**
2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
3. **Apply Database Migrations:**
   ```bash
   dotnet ef database update --project Neura.Repository --startup-project Neura.Api
   ```
4. **Run the application:**
   ```bash
   dotnet run --project Neura.Api
   ```

---

## 📖 API Documentation

Once the application is running, you can access the interactive API documentation:

- **Scalar (Modern UI):** `https://localhost:port/scalar/v1`
- **Swagger (Legacy UI):** `https://localhost:port/swagger`
- **OpenAPI Spec:** `https://localhost:port/openapi/v1.json`

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
