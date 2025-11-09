# PersonaWatch

PersonaWatch is a modular intelligence and monitoring platform designed to collect, analyze, and report digital presence data across multiple public sources. The system follows Domain-Driven Design (DDD) and Clean Architecture principles to ensure maintainability and scalability.

---

## Purpose

PersonaWatch solves the need for centralized monitoring of individuals or entities across online platforms by providing:

- Unified scanning workflows (e.g., Instagram, YouTube, SERP, etc.)
- Normalized reporting and structured insights
- Pluggable provider architecture for adding new scanners without modifying core logic
- Video clip extraction tools for media-based evidence, monitoring, and report preparation

---

## Architecture Overview

PersonaWatch is structured according to Clean Architecture:

Presentation Layer (HTTP/API)
    ↓
Application Layer (Use Cases, Services, Interfaces, DTOs)
    ↓
Domain Layer (Entities, Value Objects, Core Business Rules)
    ↓
Infrastructure Layer (EF Core, External Providers, Repositories, API Clients)

Key Rule:
- Domain → depends on nothing.
- Application → depends only on Domain.
- Infrastructure → depends on both Domain and Application.
- API → depends on Application and Infrastructure.

---

## Project Structure

PersonaWatch.Api
  - Controllers and HTTP endpoints

PersonaWatch.Application
  - Service interfaces
  - DTOs
  - Use-case logic
  - Validators

PersonaWatch.Domain
  - Entities
  - Value Objects
  - Enums
  - Business invariants

PersonaWatch.Infrastructure
  - EF Core DbContext & Migrations
  - Repository implementations
  - External data providers (e.g., YouTube, SERP, Instagram)
  - Media Clip Service (yt-dlp + ffmpeg integration)

---

## Key Features

- Multi-source scanning pipeline
- Modular report generation logic
- YouTube video segment clipping (download or stream)
- Extendable provider system
- SQL Server persistence layer
- Auth-ready architecture

---

## Technology Stack

API: ASP.NET Core (NET 8)
Application: MediatR, Mapster, FluentValidation
Domain: Pure C# objects
Infrastructure: EF Core, SQL Server, YoutubeExplode, Selenium, ffmpeg / yt-dlp

---

## Media Clip API Usage

GET /api/tools/youtube/clip?videoId={id}&start={sec}&end={sec}

Example:
GET http://localhost:5099/api/tools/youtube/clip?videoId=dQw4w9WgXcQ&start=10&end=45

The service:
1) Attempts to clip using yt-dlp (segment-accurate)
2) Falls back to ffmpeg if needed
3) Streams the resulting MP4 back to the client

---

## Requirements

- .NET 8 SDK
- SQL Server (local or hosted)
- ffmpeg installed
- (optional but recommended) yt-dlp installed

macOS:
brew install ffmpeg
brew install yt-dlp

---

## Running the Project

dotnet build
dotnet watch run --project PersonaWatch.Api

Swagger UI:
http://localhost:5099/swagger

---

## Extending the System

To add a new scanner:

1. Define an interface in PersonaWatch.Application.Abstraction.Services
2. Implement it under PersonaWatch.Infrastructure.Providers.*
3. Register implementation in Infrastructure.DependencyInjection
4. Call through an Application service
5. Expose through API controller if needed

---

## Contribution

Contributions, feedback, and provider extensions are welcome.

---

## License

This project is currently private. All rights reserved unless explicit permission is granted.

