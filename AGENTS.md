# Equipment Rental Agent Notes

## Current State

- This repository is documentation-only; `docs/EquipmentRental_PRD.md` is the sole source of requirements.
- No solution, application, manifest, lockfile, CI workflow, or executable build/test command exists yet. Do not claim verification beyond document review until those files are added.
- Treat structures and technologies described in the PRD as implementation requirements, not as existing code.

## Scope Guardrails

- Build a single-vendor, single-branch MVP without hardcoding company ownership; multi-vendor and multi-branch behavior are future scope.
- MVP payment is full cash payment only. Do not introduce online payments, partial payments, deposits, tax, or automatic cancellation fees.
- The PRD explicitly excludes automated test projects for the MVP; verification is manual through Swagger/Postman, the UI, PostgreSQL, and Serilog logs unless this requirement is changed.
- English is the MVP language, but user-facing UI strings and layouts must remain ready for Arabic and RTL.

## Required Architecture

- Backend target: .NET 10, ASP.NET Core Web API, EF Core 10, and PostgreSQL.
- Use five Clean Architecture projects named `Core`, `Services`, `infrastructure`, `Api`, and `Shared`; preserve dependency direction `Api -> Services -> Core` and `Api -> infrastructure -> Services/Core`.
- Use CQRS/MediatR, FluentValidation, Result errors, domain events, generic repositories/unit of work, optimistic concurrency, outbox processing, and manual mapping/LINQ projections. Do not add AutoMapper.
- Preserve MediatR pipeline order: unhandled exception, performance, validation, idempotency, caching, then handler.
- Use `TimeProvider` for business time and `DateTimeOffset` for persisted business timestamps; display in `Asia/Amman`. Do not use `DateTime.UtcNow` inside business rules.
- Frontend is one Next.js/TypeScript application for storefront, customer, operations, and admin surfaces, separated with route groups, authorization boundaries, and feature modules. Use Tailwind, shadcn/ui, TanStack Query, React Hook Form, and Zod.

## Domain Invariants

- Inventory supports both `Serialized` units and `QuantityBased` stock. Derive available bulk quantity from source records and overlapping periods; never persist it as an independently mutable total.
- Availability must include preparation/return buffers, active 12-hour holds, confirmed/active rentals, maintenance, unavailable stock/unit status, and every package component.
- Recheck availability inside the submission transaction. Contention must produce `409 Conflict` with affected items; duplicate-sensitive create/confirm operations require idempotency keys.
- Confirmation reserves serialized product quantity but does not assign physical units; assign eligible units only when the rental enters `Preparing`.
- Cart contents reserve nothing. A hold starts only when an authenticated, email-verified, non-suspended customer submits a request.
- Persist product, package-content, quantity, price, coupon, and delivery-address snapshots needed to keep historical rentals stable.
- Validate rental state transitions in `Core`; expected invalid transitions return explicit Result errors.
- A rental cannot become `Active` without full payment and checkout inspection, except for an Admin override with an audited reason.
- Payments are append-only: correct mistakes with reversal entries. Damage costs become charges only after Admin approval; damage/loss evidence requires photos.
- Rental days round every partial 24-hour period up. Late fees start only after the two-hour grace period and use item rate snapshots.
- A third No-show suspends new booking access. Suspended customers retain sign-in and read access to their existing records.

## API And Operations

- Enforce ownership and authorization in the API/application boundaries; frontend visibility is never a security boundary.
- Use the PRD response envelope `{ code, message, data }`; map validation to 400, authorization to 401/403, missing data to 404, conflicts to 409, and rate limits to 429.
- All list endpoints require server-side pagination; PostgreSQL remains the availability source of truth.
- Workers for hold expiry, outbox delivery, reminders, and overdue detection must be cancellation-aware, idempotent, observable, and retry-safe.
- Audit sensitive rental overrides, inventory/price changes, unit assignments, payment/reversal actions, damage decisions, account suspension, roles/settings, and review moderation; manual overrides require a reason.
- Never log passwords, OTP values, access/refresh tokens, or secrets.

## Source Of Truth

- Consult `docs/EquipmentRental_PRD.md` before modeling workflows or entities; its critical acceptance scenarios define manual verification priorities.
- Once executable configuration exists, trust manifests and scripts for commands/tool versions while retaining the PRD as the product-behavior authority.
