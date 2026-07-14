# Equipment Rental MVP Milestones

Source: `docs/EquipmentRental_PRD.md`

This roadmap is ordered by dependency and delivery risk. Every milestone must produce a manually demonstrable vertical slice across the relevant API, database, workers, and UI. The MVP excludes automated test projects; verification uses Swagger/Postman, the application UI, PostgreSQL inspection, and Serilog logs.

## Manual Testing Evidence

- [ ] Store milestone evidence under `docs/manual-testing/`, including `milestone-00a-project-foundation.md`, `milestone-00b-application-foundations.md`, `milestone-01-auth.md`, `milestone-02-catalog.md`, `milestone-03-availability.md`, subsequent milestone files using the same naming pattern, and `release-acceptance.md`.
- [ ] Give every test case these fields: `ID`, `Preconditions`, `Steps`, `Expected Result`, `Actual Result`, `Status`, `Evidence`, and `Tested At`.
- [ ] Update the relevant manual-testing document and attach reproducible evidence before marking a milestone's Exit Criteria complete.
- [ ] Do not create automated unit, integration, architecture, or end-to-end test projects unless the approved MVP scope changes.

## Milestone 0A: Project Foundation

**Outcome:** The required backend, frontend, database, and local dependencies start successfully with the approved project boundaries.

- [x] Create the .NET 10 solution with `Core`, `Services`, `infrastructure`, `Api`, and `Shared` projects.
- [x] Enforce dependency direction `Api -> Services -> Core` and `Api -> infrastructure -> Services/Core`, with `Shared` limited to cross-layer DTOs and contracts.
- [x] Initialize one Next.js/TypeScript application with route groups and authorization-ready layouts for storefront, customer, operations, and admin surfaces.
- [x] Configure PostgreSQL, Npgsql, EF Core 10, design-time migration support, and the initial migration.
- [x] Configure Docker-based local dependencies and environment-specific settings without committing secrets.
- [x] Add Swagger/OpenAPI, Serilog console logging, Health Checks, Response Compression, and a basic Development CORS policy.
- [x] Establish basic frontend routing, root layouts, responsive shell, Tailwind, and shadcn/ui design-system setup.
- [x] Document prerequisites, environment variables, startup order, database migration commands, and shutdown commands in the repository README.
- [x] Create `docs/manual-testing/milestone-00a-project-foundation.md` and record foundation smoke-test evidence.

**Verification commands**

```bash
docker compose up -d
dotnet restore
dotnet build
dotnet ef database update --project infrastructure --startup-project Api
dotnet run --project Api
npm install
npm run dev
```

The generated solution and frontend manifests become the command source of truth. Update these commands if scaffolding establishes different verified paths or scripts.

**Exit criteria**

- [x] `dotnet restore` and `dotnet build` complete successfully for the five-project solution.
- [x] Docker starts PostgreSQL, the initial migration applies cleanly, and database connectivity is visible through Health Checks and Serilog.
- [x] The API starts, Swagger loads, and a health request succeeds under the Development CORS policy.
- [x] The Next.js development server starts and each top-level route-group layout renders on desktop and mobile.
- [x] No secret is committed, and a new developer can reproduce startup using only documented commands.
- [x] `docs/manual-testing/milestone-00a-project-foundation.md` contains completed evidence for all Exit Criteria.

## Milestone 0B: Application Foundations

**Depends on:** Milestone 0A

**Outcome:** Cross-cutting backend and frontend conventions are operational before business features depend on them.

- [ ] Implement the Result Pattern and the standard `{ code, message, data }` API response envelope, including field-specific actionable validation errors.
- [ ] Map validation to 400, authentication/authorization to 401/403, missing data to 404, conflicts to 409, rate limits to 429, and unexpected failures to 500.
- [ ] Add global exception handling, request correlation IDs, structured request logging, and safe unexpected-error responses.
- [ ] Establish CQRS command/query and handler conventions with MediatR and FluentValidation; order pipeline behaviors: unhandled exception, performance, validation, idempotency, caching, handler.
- [ ] Establish generic Repository and Unit of Work abstractions without bypassing aggregate boundaries or transaction ownership.
- [ ] Add `TimeProvider`, persisted `DateTimeOffset`, `Asia/Amman` display conversion, and decimal JOD money conventions; business rules must not call `DateTime.UtcNow`.
- [ ] Establish Domain Event dispatch, Outbox persistence, idempotent request persistence, and optimistic concurrency conventions.
- [ ] Establish manual mapping and LINQ projection conventions; do not add AutoMapper.
- [ ] Implement the `AuditLog` entity and persistence with acting user, action, target type/ID, timestamp, relevant old/new values, IP address when available, and mandatory reason support for manual overrides.
- [ ] Configure ASP.NET Core Rate Limiting middleware and endpoint-policy conventions with standard 429 envelope behavior.
- [ ] Add caching abstractions with optional Redis-backed HybridCache; Development must run without Redis, and cache must never be the availability source of truth.
- [ ] Configure TanStack Query, React Hook Form, and Zod in the frontend application.
- [ ] Add shared loading, empty, error, confirmation, and notification patterns with accessible interaction behavior.
- [ ] Establish localization-ready user-facing strings and RTL-safe layout/design tokens while keeping the MVP UI English.
- [ ] Create `docs/data-model.md` before Catalog or Availability implementation. Document entities/relationships, aggregate boundaries, ownership, delete behavior, unique constraints, concurrency tokens, snapshot tables, the three rental status dimensions, monetary fields, date/time conventions, and indexes for availability overlaps, active filters, catalog search, customer history, Outbox processing, hold expiration, and operational dashboards.
- [ ] Add a transition matrix to `docs/data-model.md` for `RentalOperationalStatus`, `RentalFinancialStatus`, and `DamageResolutionStatus`, including allowed transitions, triggering events, authorized actors, validation guards, terminal states, and availability effects.
- [ ] Treat `docs/data-model.md` as a design contract that can evolve by review; do not require all documented tables to be implemented in this milestone.
- [ ] Create `docs/manual-testing/milestone-00b-application-foundations.md` and record application-foundation evidence.

**Exit criteria**

- [ ] A manually exercised command passes through the approved MediatR pipeline and returns the standard success or error envelope.
- [ ] Validation, 404, conflict, rate-limit, and unexpected-error paths demonstrate correct status mapping and correlation IDs without leaking sensitive details.
- [ ] Domain Event, Outbox, idempotency, concurrency, and Audit Log records can be persisted and inspected in PostgreSQL.
- [ ] The application starts and performs its non-cached paths in Development with Redis unavailable.
- [ ] Shared frontend states are demonstrated with keyboard access, visible focus, and RTL-safe layout behavior.
- [ ] `docs/data-model.md` is reviewed before Milestone 2 starts and covers every required design topic, including the complete three-dimension transition matrix.
- [ ] `docs/manual-testing/milestone-00b-application-foundations.md` contains completed evidence for all Exit Criteria.

## Milestone 1: Identity, Accounts, and Authorization

**Depends on:** Milestone 0B

**Outcome:** Visitors can register/sign in, authenticated users receive account email, and customers, operations employees, and admins access only permitted data and actions.

- [ ] Implement organization, user, customer profile, employee profile, refresh token, OTP, role, and account/booking status data according to `docs/data-model.md`.
- [ ] Implement customer registration with Jordanian phone validation, terms acceptance, BCrypt password hashing, and email-verification eligibility rules.
- [ ] Add MailKit email infrastructure, safe configuration, retry-aware sending, and templates for verification and password recovery.
- [ ] Implement email OTP generation, verification, expiry, resend, and rate limits without logging OTP values.
- [ ] Implement login, short-lived JWT access tokens, refresh-token rotation, logout/revocation, forgot password, OTP verification, and password reset.
- [ ] Revoke relevant refresh tokens after password changes and enforce configurable login lockout plus authentication/OTP rate limits.
- [ ] Implement persisted Notification records and email/in-app notification contracts before lifecycle features begin producing notifications.
- [ ] Implement the basic cancellation-aware, idempotent, observable Outbox processor and use it for account email delivery.
- [ ] Implement role-based authorization and policies for Customer, OperationsEmployee, and Admin actions.
- [ ] Enforce ownership in API/application handlers so customers can access only their own records.
- [ ] Implement Admin customer, employee, role, account-status, and booking-status management plus the authorized OperationsEmployee action to mark a phone confirmed after direct contact.
- [ ] Build login, registration, verification, password recovery, profile, and security UI while preserving the intended return route and local cart across login.
- [ ] Seed or document secure creation of the initial organization, Admin, and OperationsEmployee accounts.
- [ ] Add audit events for sensitive account-status, employee, role, and security administration actions.
- [ ] Create `docs/manual-testing/milestone-01-auth.md` and record authentication, email, authorization, ownership, rate-limit, and audit evidence.

**Exit criteria**

- [ ] Registration through email OTP verification, sign-in, token refresh/rotation, logout, and password reset work end to end.
- [ ] Account email is delivered through the Outbox processor, and retrying processing does not duplicate the logical notification.
- [ ] Unverified and suspended customers cannot submit bookings, while suspended customers can still sign in and read existing records.
- [ ] Manual cross-account and cross-role requests return the required 401/403 responses without exposing protected data.
- [ ] Passwords, OTP values, access/refresh tokens, and secrets do not appear in logs or audit values.
- [ ] `docs/manual-testing/milestone-01-auth.md` contains completed evidence for all Exit Criteria.

## Milestone 2: Catalog, Packages, and Inventory

**Depends on:** Milestone 1 and approved `docs/data-model.md`

**Outcome:** Admins can manage rentable offerings and early inventory blocks, while guests can browse accurate serialized and bulk inventory definitions.

- [ ] Implement hierarchical categories and active/inactive ordering.
- [ ] Implement equipment products with unique slugs, decimal JOD daily rates, tracking type, eligibility flags, product-specific buffers, structured specifications, and organization ownership reference.
- [ ] Implement ordered product images, alt text, upload validation, local Development storage, and a production object-storage abstraction.
- [ ] Implement serialized equipment units with unique serial/internal codes, condition, operational status, and history relationships.
- [ ] Implement quantity-based stock source records for total, disabled, damaged, and rental movement quantities without a mutable available-total field.
- [ ] Implement fixed packages with component requirements, independent package rates, minimum days, and fulfillment eligibility.
- [ ] Implement a minimal `EquipmentUnavailabilityBlock` with product or serialized-unit reference, start/end `DateTimeOffset`, reason, active status, and availability-exclusion semantics.
- [ ] Build Admin management for categories, products, packages, images, quantities, equipment units, and equipment unavailability blocks.
- [ ] Add audit events for product, package, price, inventory, unit-status, image, and unavailability-block changes.
- [ ] Build paginated storefront catalog, category, search, filter, sort, product detail, package list, and package detail pages.
- [ ] Create `docs/manual-testing/milestone-02-catalog.md` and record catalog, inventory, upload, unavailability-block, pagination, UI, and audit evidence.

**Exit criteria**

- [ ] Admin can publish serialized products, quantity-based products, and fixed packages with valid images and specifications.
- [ ] Admin can create, deactivate, and inspect product/unit unavailability blocks with valid time ranges and reasons.
- [ ] Guests can browse only active offerings through server-paginated endpoints and responsive, accessible pages.
- [ ] Sensitive changes create complete audit records, and invalid or unsafe uploads are rejected.
- [ ] `docs/manual-testing/milestone-02-catalog.md` contains completed evidence for all Exit Criteria.

## Milestone 3: Pricing, Availability, and Cart

**Depends on:** Milestone 2

**Outcome:** Guests and customers can build a correctly priced cart for one rental period and receive PostgreSQL-backed availability results.

- [ ] Implement a one-day minimum, charge durations under 24 hours as one day, and round every additional partial 24-hour period up.
- [ ] Implement product and package pricing with decimal JOD calculations and no tax, deposit, advance, or partial-payment concepts.
- [ ] Implement PostgreSQL overlap queries using configurable four-hour preparation and four-hour return defaults with per-product overrides.
- [ ] Calculate serialized availability from eligible units/conflicts and bulk availability from source quantities and overlapping demand.
- [ ] Exclude active `EquipmentUnavailabilityBlock` periods for affected products and serialized units.
- [ ] Include active holds, every reservation-bearing operational status, unavailable unit/stock states, and every package component using that component product's effective buffered interval; release occupancy only through an explicit release event or required return inspection.
- [ ] Add the availability indexes designed in `docs/data-model.md` and inspect representative PostgreSQL query plans.
- [ ] Keep PostgreSQL as the availability source of truth; cached catalog/reference data must not determine reservation availability.
- [ ] Implement guest local cart and authenticated cart merge; require one shared rental period before adding the first item.
- [ ] Recheck the full cart when dates or quantities change and make clear that cart contents reserve nothing.
- [ ] Display `From X JOD/day` before period selection and build availability-aware catalog filters/cart UI with actionable unavailable-item and recoverable-error states.
- [ ] Create `docs/manual-testing/milestone-03-availability.md` and record pricing, overlap, buffer, unavailability-block, package, cart, and query evidence.

**Exit criteria**

- [ ] Serialized, bulk, and package availability agree with PostgreSQL source records across overlapping and buffered periods.
- [ ] An active product/unit unavailability block removes affected inventory, while inactive or non-overlapping blocks do not.
- [ ] A package is unavailable whenever any component is insufficient or blocked.
- [ ] Pricing demonstrates the one-day minimum and partial-day rounding, and changing the period triggers a complete recheck.
- [ ] Login merges the guest cart without creating a hold or reservation.
- [ ] `docs/manual-testing/milestone-03-availability.md` contains completed evidence for all Exit Criteria.

## Milestone 4: Fulfillment, Coupons, Submission, and Holds

**Depends on:** Milestones 1 and 3

**Outcome:** An eligible customer can submit a pickup or delivery request that atomically creates snapshots, a 12-hour hold, and required notifications.

- [ ] Implement saved customer addresses and immutable delivery-address snapshots.
- [ ] Implement single-branch data, working hours, pickup appointments, manual delivery zones, fixed fees, minimum subtotals, and eligibility checks.
- [ ] Implement delivery and separately selected collection slots with capacity, active state, date/time windows, optional zone restrictions, and validation that delivery occurs before event start.
- [ ] Implement percentage/fixed coupons, validity dates, subtotal rules, limits, one-coupon policy, and submission/confirmation revalidation.
- [ ] Enforce coupon active dates, minimum subtotal, maximum percentage discount, total/per-customer usage limits, rental-subtotal-only application, and exclusion of delivery and additional charges.
- [ ] Enforce submission eligibility for authenticated, email-verified, non-suspended customers without blocking suspended users' read access.
- [ ] Revalidate dates, quantities, packages, coupon, delivery, price, and availability inside one submission transaction.
- [ ] Persist product, package-content, quantity, rate, coupon, discount, fulfillment, and address snapshots.
- [ ] Create rental/items, temporary hold, original 12-hour expiration, Outbox messages, and idempotency result atomically.
- [ ] Initialize independent `RentalOperationalStatus`, `RentalFinancialStatus`, and `DamageResolutionStatus` values when the rental is created.
- [ ] Handle optimistic concurrency for inventory, slot capacity, and coupon usage limits so contention returns an actionable `409 Conflict` and creates no partial booking.
- [ ] Implement the cancellation-aware, idempotent, observable, retry-safe hold-expiry worker.
- [ ] Deliver customer/staff submission notifications and customer hold-expiration/expired-request notifications through the Outbox.
- [ ] Build address, fulfillment, slot, coupon, checkout/review, submission-success, and pending-rental UI.
- [ ] Add audit events for delivery configuration, slot capacity, coupon administration, submission conflicts where appropriate, and hold-expiry actions.
- [ ] Create `docs/manual-testing/milestone-04-submission-and-holds.md` and record fulfillment, coupon, transaction, concurrency, idempotency, worker, notification, and UI evidence.

**Exit criteria**

- [ ] Pickup and delivery submissions create correct snapshots and active 12-hour holds; unsupported zones and full/inactive slots are rejected.
- [ ] Two concurrent customers cannot both reserve unavailable overlapping quantity; the loser receives an actionable 409 response.
- [ ] Retrying submission with the same idempotency key does not create duplicate rentals, holds, or logical notifications.
- [ ] Expiring a pending request releases availability exactly once and delivers the required customer/staff notification.
- [ ] `docs/manual-testing/milestone-04-submission-and-holds.md` contains completed evidence for all Exit Criteria.

## Milestone 5: Review, Confirmation, Changes, and Reliability Policies

**Depends on:** Milestone 4

**Outcome:** Staff and customers can safely change pre-operation requests while operational, financial, and damage states remain independent and reservations stay consistent.

- [ ] Complete domain-owned transitions for `RentalOperationalStatus`, `RentalFinancialStatus`, and `DamageResolutionStatus` with explicit Result errors; never infer financial or damage resolution solely from operational status, and update `docs/data-model.md` as the transition design evolves.
- [ ] Build operations queues and rental details for pending-request review.
- [ ] Implement confirmation as one idempotent, optimistic-concurrency-protected transaction that revalidates coupon, fulfillment, and availability; converts the hold; changes operational status; records coupon usage; and persists Outbox/audit/idempotency effects without assigning serialized units.
- [ ] Implement rejection, customer cancellation, Admin cancellation, expiration, and their availability-release rules.
- [ ] Record cancellation actor, reason, timestamp, notes, and late-cancellation flag for confirmed rentals cancelled within 48 hours; do not create automatic cancellation fees.
- [ ] Implement pending-request modification as one idempotent, optimistic-concurrency-protected transaction.
- [ ] Validate proposed dates, products, quantities, packages, fulfillment method, address, and coupon before mutating persisted state.
- [ ] Recheck availability while excluding only the request's current hold.
- [ ] Recalculate pricing, fulfillment, coupon, and all affected snapshots.
- [ ] Replace the previous hold with the new hold in the same transaction while preserving the original 12-hour expiration timestamp without resetting or extending it.
- [ ] Roll back completely and preserve the original request, snapshots, and hold if validation, availability, concurrency, or persistence fails.
- [ ] Return the prior successful result for an idempotent retry and return an actionable 409 for a competing update.
- [ ] Implement post-confirmation modification and active-rental extension requests with staff approval, conflict checks, approved added cost, and rejection preserving the original rental/return time; shortening duration does not automatically reduce charges and requires a documented Admin adjustment.
- [ ] Implement No-show handling, inventory release, warning/review progression, automatic suspension on the third No-show, and audited Admin reactivation.
- [ ] Deliver confirmation, rejection, customer/Admin cancellation, expiration, modification-decision, extension-decision, and No-show notifications through the Outbox.
- [ ] Add audit events for confirmation/rejection, cancellations, modifications/extensions, status transitions, No-shows, suspension/reactivation, and manual overrides.
- [ ] Build customer tracking, cancellation, pending modification, post-confirmation modification, and extension UI plus staff review flows.
- [ ] Create `docs/manual-testing/milestone-05-rental-review-and-changes.md` and record transitions, atomic hold replacement, rollback, concurrency, idempotency, status separation, notifications, and audit evidence.

**Exit criteria**

- [ ] Confirmation converts rather than duplicates held availability and does not assign physical serialized units.
- [ ] A successful pending modification atomically replaces request data/snapshots and its hold while retaining the exact original expiration time.
- [ ] A failed or competing pending modification leaves the original request, snapshots, hold, and expiration unchanged; retries do not duplicate holds or effects.
- [ ] Invalid transitions in any status dimension are rejected without partial updates.
- [ ] The persistence/domain model permits independent operational, financial, and damage-resolution values, and changing one dimension does not implicitly change either of the others.
- [ ] Rejection, cancellation, expiration, and No-show release inventory correctly, and a third No-show blocks new submissions while preserving read access.
- [ ] Each lifecycle event delivers its required notification and creates its required audit record exactly once logically.
- [ ] `docs/manual-testing/milestone-05-rental-review-and-changes.md` contains completed evidence for all Exit Criteria.

## Milestone 6: Preparation, Scheduling, Payment, and Handover

**Depends on:** Milestone 5

**Outcome:** Operations can prepare equipment, collect full cash payment, inspect it, and hand it over or dispatch it while financial status remains independent.

- [ ] Build operational calendar, starting/due-today views, preparation queues, and pickup/delivery/collection schedules.
- [ ] Implement transition to `Preparing` and assign only eligible serialized units outside rental and unavailability conflicts.
- [ ] Record prepared and handed-over bulk quantities and visibly flag preparation shortages.
- [ ] Implement checkout inspections for condition, accessories, quantities, existing damage, employee confirmation, and customer acknowledgment name/timestamp.
- [ ] Implement append-only full cash rental payments due at pickup/delivery with amount, `Cash` payment type, receiver, receipt number, timestamp, notes, and idempotency protection.
- [ ] Update `RentalFinancialStatus` from financial source records without changing `RentalOperationalStatus` or `DamageResolutionStatus`.
- [ ] Persist payment/reversal, financial-status update, receipt, Outbox/audit records, and idempotency result atomically; never edit or delete recorded payments.
- [ ] Generate provisional/final rental invoices and cash receipts with QuestPDF from immutable snapshots as applicable.
- [ ] Enforce that transition to `Active` requires full rental payment and checkout inspection, except for an Admin override with mandatory audited reason.
- [ ] Support `ReadyForPickup`, `OutForDelivery`, and `Active` operational paths with correct fulfillment eligibility.
- [ ] Notify the customer when staff changes an already-selected slot, and deliver PRD-required preparation-shortage, schedule, payment/receipt, delivery, and activation notifications through the Outbox.
- [ ] Add audit events for serialized-unit assignment/replacement, prepared quantities, inspections, schedule overrides, payments/reversals, operational transitions, and Admin activation overrides.
- [ ] Build preparation, unit assignment, checkout inspection, cash payment, receipt, pickup, and delivery UI.
- [ ] Create `docs/manual-testing/milestone-06-preparation-and-handover.md` and record assignment, quantity, inspection, payment, status separation, notification, audit, and UI evidence.

**Exit criteria**

- [ ] Serialized units are first assigned during preparation and unavailable, damaged, retired, or blocked units cannot be assigned.
- [ ] Bulk prepared and handed-over quantities reconcile with rental quantities or show an unresolved shortage.
- [ ] A rental cannot become Active without full rental payment and checkout inspection; only a reasoned, audited Admin override bypasses the gate.
- [ ] Payment retries do not duplicate entries; corrections create linked reversals and update financial status without rewriting operational history.
- [ ] Preparation, payment, schedule, delivery, and activation events deliver required notifications and audit records.
- [ ] `docs/manual-testing/milestone-06-preparation-and-handover.md` contains completed evidence for all Exit Criteria.

## Milestone 7A: Returns, Overdue, and Late Fees

**Depends on:** Milestone 6

**Outcome:** Operations can reconcile and inspect returned equipment, release it back to availability, and administer overdue rentals and late fees independently.

- [ ] Implement `Returned` and `UnderInspection` transitions; clean returns transition `RentalOperationalStatus` to `Completed` after required inspection and operational validations pass.
- [ ] Reconcile serialized-unit returns and bulk returned, damaged, and missing quantities against handover records.
- [ ] Record serialized condition/accessory changes, bulk outcomes, and employee confirmation; inspection photos remain optional unless damage or loss is reported.
- [ ] When inspection finds damage or loss, create an initial `DamageReport` linked to the return inspection and affected inventory, validate and persist the required images, transition `RentalOperationalStatus` to `DamageReviewPending`, update `DamageResolutionStatus`, keep affected inventory unavailable, and release unaffected eligible inventory.
- [ ] Implement cancellation-aware, idempotent Overdue and Critical Overdue detection using subsequent confirmed-rental conflicts.
- [ ] Implement the cancellation-aware, observable, retry-safe overdue worker.
- [ ] Apply the two-hour grace period and round each later partial 24-hour period up using item rate/quantity snapshots.
- [ ] Create separate late-fee obligations and update `RentalFinancialStatus` without rewriting `RentalOperationalStatus`.
- [ ] Implement full cash payment and append-only reversal support for late-fee obligations with idempotency protection.
- [ ] Persist the late-fee payment or reversal, financial-status update, receipt, Outbox messages, audit records, and idempotency result atomically.
- [ ] Release inspected, eligible serialized units and bulk quantities back to availability after required inspection, even when a late fee remains unpaid.
- [ ] Deliver PRD-required return, overdue, critical-overdue, late-fee, payment, reversal, and receipt notifications through the Outbox.
- [ ] Add audit events for returns, inspection outcomes, damage handoff, quantity reconciliation, overdue decisions, late fees, late-fee payments/reversals, availability release, and overrides.
- [ ] Build return, inspection, damage-handoff, overdue, critical-overdue, late-fee, payment/reversal, receipt, and availability-release UI.
- [ ] Create `docs/manual-testing/milestone-07a-returns-and-overdue.md` and record reconciliation, initial `DamageReport` creation, required-image validation/persistence, damage handoff, grace-period boundaries, worker retry, late-fee settlement/reversal, atomicity, availability release, status separation, notification, and audit evidence.

**Exit criteria**

- [ ] No late fee applies within two hours; after grace, fees use snapshots and correct partial-day rounding.
- [ ] Returned, damaged, and missing quantities/units reconcile to handover records before inspection completes.
- [ ] Required inspection releases eligible equipment back to availability even when `RentalFinancialStatus = Outstanding` due to an unpaid late fee.
- [ ] Damage/loss inspection creates an initial `DamageReport` with the required validated images, enters `DamageReviewPending`, updates `DamageResolutionStatus`, keeps only affected inventory unavailable, and releases unaffected eligible inventory.
- [ ] Late-fee payment/reversal retries do not duplicate financial entries, receipts, or logical notifications, and all required records commit or roll back together.
- [ ] Overdue worker retries do not duplicate obligations or logical notifications, and Critical Overdue identifies a threatened confirmed rental.
- [ ] Return, overdue, inspection, fee, and availability events create required notifications and audit records.
- [ ] `docs/manual-testing/milestone-07a-returns-and-overdue.md` contains completed evidence for all Exit Criteria.

## Milestone 7B: Damage, Charges, and Maintenance

**Depends on:** Milestone 7A

**Outcome:** Damage, loss, financial obligations, and full maintenance workflows are managed without preventing inspected equipment from following correct availability rules.

- [ ] Continue processing the initial `DamageReport` created during return inspection, including affected products, units, accessories, or bulk quantities.
- [ ] Preserve evidence access controls and verify that the required validated images remain available before review or approval.
- [ ] Allow OperationsEmployee to propose repair/replacement cost and Admin to approve or reject it.
- [ ] Drive `DamageResolutionStatus` independently through report, review, approval/rejection, charge, and settlement outcomes.
- [ ] Create a separate damage charge only after Admin approval; a configured policy may block new submissions for an unpaid Admin-approved damage charge.
- [ ] After Admin approval or rejection, allow `RentalOperationalStatus` to transition to `Completed` independently from damage-charge payment settlement.
- [ ] Implement full cash settlement and append-only reversal records for damage-charge obligations with idempotency protection.
- [ ] Persist the damage-charge payment or reversal, financial-status update, receipt, Outbox messages, audit records, and idempotency result atomically.
- [ ] Implement documented Admin manual charges/adjustments without rewriting original rental pricing or operational history.
- [ ] Keep unaffected or inspected-eligible equipment available when a damage or financial obligation remains open; keep damaged/lost equipment unavailable according to its condition/status.
- [ ] When a block exists for the same maintenance reason and product/unit, atomically create the complete maintenance record and deactivate/supersede that block without an availability gap.
- [ ] Implement maintenance type, planned interval, description, cost, provider/technician, status, completion notes, and product/unit association.
- [ ] Prevent maintenance conflicts with confirmed rentals until explicitly resolved and exclude active maintenance intervals from availability.
- [ ] Deliver PRD-required damage-report, damage-decision/approved-charge, and maintenance-conflict notifications through the Outbox.
- [ ] Add audit events for evidence, proposed cost, damage decisions, charges, settlements/reversals, manual adjustments, booking blocks, and maintenance changes/overrides.
- [ ] Build damage report/review, evidence, charge, settlement, adjustment, booking-block, and maintenance UI.
- [ ] Create `docs/manual-testing/milestone-07b-damage-and-maintenance.md` and record evidence, approval, charge, settlement, status separation, availability, maintenance conflict, notification, and audit evidence.

**Exit criteria**

- [ ] Damage/loss cannot reach an approved charge without required images and Admin approval.
- [ ] `RentalOperationalStatus = Completed`, `RentalFinancialStatus = Outstanding`, and `DamageResolutionStatus = Charged` can coexist, and eligible inspected equipment remains available.
- [ ] Admin approval or rejection releases the operational workflow to complete; an unpaid approved damage charge does not prevent `RentalOperationalStatus = Completed`.
- [ ] Damage-charge payments/reversals are append-only and idempotent, commit all related records atomically, and do not rewrite original pricing.
- [ ] Damaged/lost inventory remains unavailable as required, while maintenance blocks only affected products/units for overlapping periods.
- [ ] Maintenance cannot silently overlap a confirmed rental, and worker/API/UI paths show actionable conflicts.
- [ ] Damage, charge, payment, adjustment, booking-block, and maintenance events create required notifications and audit records.
- [ ] `docs/manual-testing/milestone-07b-damage-and-maintenance.md` contains completed evidence for all Exit Criteria.

## Milestone 8: Notification Center and Customer Engagement

**Depends on:** Milestones 4-7B

**Outcome:** Existing lifecycle notifications have a consistent in-app/real-time experience, scheduled reminders are orchestrated, and customers can use wishlist and verified reviews.

- [ ] Build the Notification Center with customer/staff isolation, pagination, deep links, and read/unread state.
- [ ] Add SignalR delivery for persisted in-app notifications without making real-time transport the persistence source of truth.
- [ ] Implement cancellation-aware, idempotent, observable, retry-safe scheduled reminder orchestration for hold, start, return, and other PRD-required reminders.
- [ ] Review all lifecycle email/in-app templates for consistent branding, localization-ready text, links, event data, and failure handling.
- [ ] Ensure failed Outbox and reminder deliveries remain retryable and observable with correlation IDs.
- [ ] Implement product/package wishlists that reserve no inventory and preserve no price.
- [ ] Implement one verified review per completed rental item, ratings, optional text/images, customer updates, and product/package aggregates.
- [ ] Implement audited Admin review hiding with mandatory reason and no content rewriting.
- [ ] Build notification read/unread and interaction surfaces, wishlist UI, review submission/edit UI, and review moderation UI.
- [ ] Create `docs/manual-testing/milestone-08-notifications-and-engagement.md` and record center, read state, SignalR, reminders, retries, templates, wishlist, review eligibility, moderation, and audit evidence.

**Exit criteria**

- [ ] Notifications from Milestones 1 and 4-7B appear consistently in the correct Notification Center and deliver through SignalR when connected.
- [ ] Duplicate worker execution does not duplicate logical notifications, and failed deliveries remain retryable and observable.
- [ ] Scheduled reminders fire at the configured business time using `TimeProvider` and do not resend after successful processing.
- [ ] Only eligible completed-rental customers can review an item; wishlist and review behavior never changes availability.
- [ ] Review moderation produces a reasoned audit record without altering customer content.
- [ ] `docs/manual-testing/milestone-08-notifications-and-engagement.md` contains completed evidence for all Exit Criteria.

## Milestone 9: Administration Completion, Documents, and Reporting

**Depends on:** Milestones 2-8

**Outcome:** Remaining administration gaps, business documents, dashboards, reports, settings, and audit inspection are complete without rebuilding earlier features.

- [ ] Review Admin capabilities delivered in prior milestones and complete only missing PRD-required functionality.
- [ ] Complete System Settings for approved MVP policies, defaults, business identity, and operational configuration with reasoned audit events for sensitive changes.
- [ ] Build operational, financial, damage-resolution, and inventory dashboard aggregation queries using the three independent rental status dimensions.
- [ ] Ensure dashboard filters and labels do not treat operational completion as proof of payment or damage settlement.
- [ ] Implement reports for rentals, revenue, utilization, customer history, cancellations/No-shows, damage/maintenance, and outstanding balances.
- [ ] Include independent operational, financial, and damage-resolution status fields/filters in relevant reports and exports.
- [ ] Implement server-paginated CSV exports.
- [ ] Finalize QuestPDF invoices, receipts, late-fee charges, damage charges, manual adjustments, and reversal documents from immutable snapshots/source records.
- [ ] Build the searchable Audit Log Admin viewer with actor, action, target, date, IP, reason, and sensitive-operation filters.
- [ ] Perform final audit-event coverage review across rental decisions/overrides, inventory/prices, assignments, payments/reversals, damage, charges, suspension, roles/settings, delivery configuration, maintenance, and review moderation.
- [ ] Review availability, catalog search, customer history, Outbox/hold, active-filter, dashboard, and report indexes using observed PostgreSQL query plans.
- [ ] Build/complete responsive Admin dashboard, reports, exports, documents, settings, and Audit Log UI.
- [ ] Create `docs/manual-testing/milestone-09-administration-and-reporting.md` and record reconciliation, status dimensions, exports, PDFs, settings, query plans, audit filters, and UI evidence.

**Exit criteria**

- [ ] Dashboard and report totals reconcile with PostgreSQL source records for each independent status dimension.
- [ ] A completed rental with an outstanding late fee/damage charge appears as operationally completed and financially outstanding without contradictory totals.
- [ ] All list/report endpoints paginate server-side, and representative query plans use appropriate indexes without unbounded scans under expected MVP data.
- [ ] CSV exports and PDF documents contain required snapshots, totals, statuses, and business/customer details.
- [ ] The Audit Log viewer finds every PRD-listed sensitive operation and exposes mandatory reasons where required without leaking secrets.
- [ ] `docs/manual-testing/milestone-09-administration-and-reporting.md` contains completed evidence for all Exit Criteria.

## Milestone 10: Hardening, Manual Acceptance, and Release

**Depends on:** Milestones 0A, 0B, and 1-9

**Outcome:** The MVP is secure, observable, deployable, and has recorded evidence for all PRD critical acceptance scenarios.

- [ ] Validate API ownership and policy enforcement across every customer, operations, and Admin endpoint.
- [ ] Validate file type, content, size, extension, storage access, and authorization for every upload path.
- [ ] Verify field-level validation, 401/403/404/409/429 mapping, correlation IDs, and safe unexpected-error responses.
- [ ] Verify cancellation and retry safety for hold expiry, Outbox, reminders, and overdue workers.
- [ ] Review availability query plans, pagination, caching boundaries, and the under-500-ms target for normal MVP requests.
- [ ] Verify Redis is optional in Development and no cache is used as the source of truth for availability.
- [ ] Verify responsive behavior, keyboard navigation, focus visibility, semantic labels, non-color status cues, and supported browsers.
- [ ] Verify English UI strings are localization-ready and layouts do not block future Arabic/RTL activation.
- [ ] Configure production secrets, CORS, rate limits, object storage, SMTP, optional Redis/Seq, health checks, database backups, and a documented restore procedure.
- [ ] Document deployment, migration, rollback, worker operation, log inspection, and manual smoke-test procedures.
- [ ] Execute every manual test area in PRD section 33.1 and consolidate evidence in `docs/manual-testing/release-acceptance.md`.
- [ ] Execute all ten critical acceptance scenarios in PRD section 33.2, including concurrent submissions and idempotent retries.
- [ ] Add release scenarios proving atomic pending-request modification rollback and preservation of the original hold expiration.
- [ ] Add release scenarios proving operational, financial, and damage statuses remain independent.
- [ ] Add release scenarios proving returned equipment re-enters availability after required inspection while late-fee/damage obligations remain outstanding.
- [ ] Resolve release-blocking defects and complete the PRD section 34 acceptance checklist.

**Exit criteria**

- [ ] All ten PRD critical acceptance scenarios pass with `ID`, preconditions, steps, expected/actual results, status, evidence, and tested timestamp recorded.
- [ ] Atomic hold replacement, rollback, idempotency, concurrency conflict, and original-expiration preservation scenarios pass with PostgreSQL evidence.
- [ ] Status-separation scenarios reconcile API, PostgreSQL, dashboards, reports, documents, and UI without blocking eligible post-inspection availability.
- [ ] No open release-blocking security, data-integrity, availability, payment, worker, notification, or authorization defects remain.
- [ ] Production deployment and database restore are rehearsed, observable, and documented.
- [ ] Product ownership accepts every PRD section 34 MVP criterion or records an explicit approved exception.
- [ ] `docs/manual-testing/release-acceptance.md` contains completed evidence for all release Exit Criteria.

## Future Scope Boundary

Do not pull these items into MVP milestones without an approved scope change: Arabic translation activation, multi-vendor behavior, multiple branches, online payments/refunds, partial payments, advances/deposits, tax, SMS/social login/notifications, Firebase/native push notifications, GPS pricing, routes/drivers, installation/operators, employee-team scheduling, customizable packages, product/category-specific coupons, loyalty/automatic promotions, dynamic or seasonal pricing, dynamic specification filtering, usage-based maintenance, delivery reviews, complex analytical PDFs, microservices, or automated test projects.
