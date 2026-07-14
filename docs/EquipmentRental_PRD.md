# Equipment Rental Platform — Product Requirements Document

## 1. Document Information

| Field | Value |
| --- | --- |
| Product | Equipment Rental Platform |
| Domain | Event equipment rental |
| Target market | Jordan |
| MVP language | English |
| Future language | Arabic with full RTL support |
| Currency | Jordanian Dinar (JOD) |
| Business model | Single-vendor MVP with future multi-vendor readiness |
| Backend architecture | Clean Architecture aligned with LifeDrop |
| Document status | Final MVP PRD |

## 2. Product Summary

The Equipment Rental Platform is a full-stack e-commerce application for renting event equipment such as sound systems, microphones, lighting, LED screens, projectors, tables, chairs, tents, stages, photo booths, and generators.

Guests can browse the catalog, view prices, inspect product details, and check approximate availability. Registered and verified customers can select a rental period, add individual products or fixed packages to a rental cart, choose pickup or delivery, apply a coupon, and submit a rental request.

The business reviews each request before confirmation. Submitted requests temporarily hold the required quantities for 12 hours. Confirmed rentals move through preparation, pickup or delivery, active rental, return, inspection, payment, and completion workflows.

The platform must support both individually tracked high-value equipment and quantity-based bulk equipment. It must prevent double booking, account for preparation and return buffers, block equipment during maintenance, record cash payments, manage late returns, document damage, and maintain a complete audit trail for sensitive operations.

## 3. Product Goals

- Digitize the complete event-equipment rental lifecycle.
- Provide customers with a clear storefront, availability checking, rental pricing, and request tracking.
- Prevent double booking across products, equipment units, packages, temporary holds, confirmed rentals, and maintenance periods.
- Support serialized equipment and bulk quantity-based equipment in one catalog.
- Give operations employees reliable workflows for preparation, handover, return, inspection, and damage reporting.
- Give administrators control over the catalog, pricing, inventory, delivery zones, coupons, customers, employees, reporting, and system settings.
- Build the MVP for one rental business while preserving reasonable extension points for future multi-vendor support.
- Reuse the backend technologies and Clean Architecture approach established in LifeDrop.

## 4. Non-Goals

The MVP will not include:

- Multi-vendor onboarding, commissions, payouts, or vendor disputes.
- Online payment gateways.
- Partial payments.
- Security deposits or booking advances.
- Tax calculation.
- SMS OTP or social login.
- GPS-based delivery pricing, route optimization, or driver assignment.
- Multiple business branches.
- Installation, removal, or on-site operator services.
- Customer-customizable packages.
- Dynamic pricing or seasonal pricing engines.
- Automatic preventive maintenance based on usage counts or operating hours.
- Advanced dynamic filtering by arbitrary product specifications.
- Automated unit, integration, or architecture tests.
- Microservices.

## 5. Business Model and Scope

### 5.1 MVP Model

The MVP serves one company that owns and rents all equipment. The company manages products, physical units, bulk quantities, reservations, delivery zones, operations, payments, inspections, and maintenance.

The domain must not hardcode the company identity into products or rules. Ownership and organization boundaries should be modeled in a way that permits future expansion, but the MVP must not implement multi-tenancy behavior.

### 5.2 Future Multi-Vendor Readiness

Future versions may introduce vendors, vendor-owned equipment, commissions, payouts, vendor dashboards, disputes, and tenant-level policies. These capabilities are extension points only and must not add unnecessary MVP complexity.

## 6. User Roles and Permissions

### 6.1 Guest

- Browse the public storefront.
- Search and filter equipment and packages.
- View product details, prices, reviews, and specifications.
- Select a rental period and check availability.
- Maintain a local rental cart.
- Register or sign in.
- Cannot submit a rental request.

### 6.2 Customer

- Perform all Guest actions.
- Submit a rental request after email verification.
- Manage saved addresses.
- Choose pickup or delivery.
- View, cancel, and track personal rentals according to policy.
- Submit modification and extension requests.
- View invoices, receipts, late fees, damage charges, and outstanding balances.
- Manage a wishlist.
- Receive and manage notifications.
- Review products and packages from completed rentals.
- Update profile and security information.

### 6.3 OperationsEmployee

- Review pending rental requests.
- Confirm or reject requests within granted policy.
- Prepare equipment and assign serialized units.
- Record bulk quantities prepared and handed over.
- Manage pickup and delivery schedules.
- Activate rentals after full cash payment.
- Record handover and return inspections.
- Record returned, damaged, and missing quantities.
- Create damage reports and proposed charges.
- Record full cash payments and issue receipts.
- Manage corrective and preventive maintenance.
- Cannot manage employees, system-wide settings, or unrestricted price configuration.

### 6.4 Admin

- Perform all OperationsEmployee actions.
- Manage products, categories, packages, images, prices, quantities, and equipment units.
- Manage customers, employees, roles, and account status.
- Manage delivery zones, delivery slots, branch data, coupons, and system settings.
- Approve damage charges and sensitive manual adjustments.
- Suspend and reactivate customer booking access.
- Reverse incorrectly recorded payments.
- Access reports and audit logs.
- Override selected workflow restrictions only when a reason is recorded in the audit log.

### 6.5 Authorization Requirements

- The API must enforce all authorization rules.
- Frontend visibility is not a security boundary.
- Use role-based authorization for broad access and policy-based authorization for sensitive actions.
- Customers may access only their own rentals, addresses, payments, notifications, invoices, charges, and reviews.

## 7. Product Catalog

### 7.1 Categories

- Admin can create, update, activate, deactivate, and arrange categories.
- Categories may be hierarchical.
- Products belong to one primary category.
- Example hierarchy:

```text
Audio
├── Speakers
├── Microphones
└── Mixers

Lighting
├── Stage Lighting
└── Decorative Lighting
```

### 7.2 Equipment Product

Each product must include:

- Name.
- Unique slug.
- Short description.
- Full description.
- Category.
- Daily rental rate.
- Tracking type.
- Main image.
- Ordered image gallery.
- Structured specifications.
- Pickup and delivery eligibility.
- Preparation buffer hours.
- Return buffer hours.
- Average rating and review count.
- Featured status.
- Active or inactive status.
- Organization or owner reference prepared for future expansion.

### 7.3 Product Specifications

- Store specifications as structured key/value records.
- Examples include power, dimensions, color, capacity, wireless support, and included accessories.
- Display specifications on the product details page.
- Dynamic filtering by arbitrary specifications is not part of the MVP.

### 7.4 Product Images

- Support one main image and multiple gallery images.
- Store alt text and display order.
- Validate file type, size, and content.
- Use local file storage in Development.
- Use S3-compatible or cloud object storage in Production.

### 7.5 Search, Filtering, and Sorting

The storefront must support:

- Search by product name and description.
- Category and subcategory filters.
- Rental period availability.
- Required available quantity.
- Daily price range.
- Rating.
- Pickup eligibility.
- Delivery eligibility.
- Featured items.
- Sorting by relevance, price ascending, price descending, rating, and newest.

All list endpoints must use server-side pagination.

## 8. Equipment Tracking

### 8.1 Tracking Types

Every product uses one of two tracking modes:

```text
Serialized
QuantityBased
```

### 8.2 Serialized Equipment

Each physical unit must include:

- Equipment unit ID.
- Product ID.
- Serial number.
- Internal code.
- Current condition.
- Operational status.
- Purchase date when available.
- Notes.
- Inspection history.
- Damage history.
- Maintenance history.

Supported statuses:

```text
Available
Reserved
Rented
UnderInspection
UnderMaintenance
Damaged
Retired
```

### 8.3 Quantity-Based Equipment

Bulk products must track:

- Total quantity.
- Disabled, damaged, or maintenance quantity.
- Temporarily held quantity by period.
- Confirmed reserved quantity by period.
- Active rented quantity.
- Prepared, handed-over, returned, damaged, and missing quantities per rental.

Available quantity must be calculated from the source data and overlapping periods. It must not be maintained as an independently mutable value that can drift from reality.

### 8.4 Serialized Unit Assignment

- Confirmation reserves the required product quantity but does not assign specific serial numbers.
- Operations employees assign actual serialized units when the rental enters `Preparing`.
- Assigned units must be available, operational, and outside maintenance or conflicting rental periods.
- Assignments are stored as `RentalItemUnitAssignment` records.

## 9. Fixed Packages

- Admin can create fixed packages containing products and required quantities.
- A package has a name, slug, description, cover image, daily package rate, active status, minimum rental days when applicable, and pickup/delivery eligibility.
- Customers cannot modify package contents in the MVP.
- Package availability depends on the availability of every included component.
- Package pricing is independent from the sum of its component prices.
- Customers may add fixed packages and individual products to the same rental cart.
- The platform must store package contents and prices as snapshots on submission so historical rentals do not change after package edits.
- Customer-customizable packages belong to Future Scope.

## 10. Rental Period, Pricing, and Cart

### 10.1 Rental Period

- Rentals are priced by day.
- The minimum rental duration is one day.
- Any duration shorter than 24 hours is charged as one full day.
- Any additional partial 24-hour period is charged as another rental day.
- Use `DateTimeOffset` for business timestamps.
- Store timestamps consistently and display them using the `Asia/Amman` timezone.

### 10.2 Pricing Formula

```text
Rental Item Subtotal = Daily Rate Snapshot × Quantity × Charged Rental Days

Rental Subtotal
− Coupon Discount
+ Delivery Fee
+ Approved Additional Charges
= Total
```

- Currency is JOD.
- Use `decimal` for monetary values.
- The MVP has no tax.
- Late fees and damage charges are separate financial obligations.
- Store price, product, package, and quantity snapshots on the rental.

### 10.3 Rental Cart

- Guests and customers may browse without selecting dates.
- Catalog prices appear as `From X JOD/day` before a rental period is selected.
- A rental period is required before adding the first item to the rental cart.
- All cart items use the same rental period.
- Changing the rental period triggers a full availability recheck.
- A guest cart is stored locally and merged with the account after login.
- A cart does not reserve equipment.
- A temporary hold is created only when a verified customer submits a rental request.

## 11. Availability and Double-Booking Prevention

### 11.1 Availability Sources

Availability must consider:

- Requested rental period.
- Preparation and return buffers.
- Active temporary holds.
- Confirmed and active rentals.
- Serialized unit status.
- Bulk unavailable quantities.
- Maintenance periods.
- Package component requirements.
- Cancelled, rejected, expired, and completed records as appropriate.

### 11.2 Turnaround Buffers

- Default preparation buffer: 4 hours.
- Default return and inspection buffer: 4 hours.
- Admin may override both values per product.
- Effective availability interval:

```text
Effective Start = Rental Start − Preparation Buffer
Effective End = Rental End + Return Buffer
```

- Buffers affect availability but do not add customer charges.
- Package availability must honor component buffers.

### 11.3 Concurrency

- Recheck availability inside the request-submission transaction.
- Use optimistic concurrency for contested inventory records.
- Handle concurrency conflicts explicitly.
- Return `409 Conflict` if another request acquires the required availability first.
- The error response should identify affected items and allow the customer to adjust dates or quantities.
- Sensitive create and confirmation commands must support idempotency keys.

## 12. Rental Request and Temporary Hold

### 12.1 Request Submission

Only an authenticated, email-verified, non-suspended customer may submit a rental request.

Submission must:

1. Validate customer eligibility.
2. Revalidate dates, quantities, package components, coupon, delivery zone, and price.
3. Check availability inside a transaction.
4. Create rental and item snapshots.
5. Create a temporary hold for required availability.
6. Set the hold to expire after 12 hours.
7. Notify the customer and operations staff.

### 12.2 Approval Workflow

```text
PendingApproval
├── Confirmed
├── Rejected
├── CancelledByCustomer
└── Expired
```

- Pending requests hold required availability for 12 hours.
- Admin or OperationsEmployee may confirm or reject a request according to authorization policy.
- Confirmation converts the hold into a confirmed reservation.
- Rejection, customer cancellation, or expiration releases held availability.
- A Background Worker must expire overdue pending requests safely and idempotently.

## 13. Rental Lifecycle

Primary operational states:

```text
PendingApproval
Confirmed
Preparing
ReadyForPickup
OutForDelivery
Active
Overdue
Returned
UnderInspection
DamageReviewPending
Completed
```

Terminal or exceptional states:

```text
Rejected
Expired
CancelledByCustomer
CancelledByAdmin
NoShow
```

State transitions must be validated in the Domain layer. Invalid transitions must return explicit Result errors rather than being silently accepted.

## 14. Pickup and Delivery

### 14.1 Pickup

- The MVP supports one branch.
- Branch data includes name, address, phone, working hours, and optional map URL.
- Customers select a pickup appointment within working hours.
- Some products or packages may be marked Delivery Only.

### 14.2 Customer Addresses

- Customers may save multiple delivery addresses.
- Addresses include governorate, city or area, street, building details, and delivery notes.
- Each delivery rental stores an address snapshot.

### 14.3 Delivery Zones

- Admin defines manual delivery zones.
- Each zone has a name, governorate, fixed fee, active status, optional minimum rental subtotal, and delivery notes.
- No GPS distance calculation is included.
- Addresses outside supported zones cannot use Delivery.

### 14.4 Delivery and Collection Slots

- Admin defines delivery and collection windows.
- Each slot includes date, start time, end time, capacity, active status, and optional zone restriction.
- Customers cannot choose a full or inactive slot.
- Delivery must occur before the event start time.
- Collection uses a separate selected window.
- Admin may modify a slot after customer communication; the customer must be notified.
- Driver assignment and route optimization are Future Scope.

## 15. Cancellation and Customer Reliability

### 15.1 Cancellation Rules

- Customers may cancel pending requests at any time without penalty.
- Confirmed rentals may be cancelled without a late-cancellation record more than 48 hours before rental start.
- Cancellation within 48 hours is recorded as `LateCancellation`.
- The MVP does not automatically charge a cancellation fee.
- Store cancellation reason, actor, timestamp, late-cancellation flag, and administrative notes.

### 15.2 No-Show Policy

- Mark a rental `NoShow` when the customer does not attend pickup or is unavailable for delivery.
- Release equipment availability.
- Increase the customer's No-show count.
- First No-show: warning.
- Second No-show: manual review.
- Third No-show: automatically suspend new booking access.
- Suspended customers may sign in and view existing records but cannot submit new rental requests.
- Admin may reactivate booking access with an audited reason.

## 16. Rental Modification and Extension

### 16.1 Before Confirmation

Customers may modify dates, products, quantities, packages, fulfillment method, address, and coupon. Every change requires a new availability and price calculation.

### 16.2 After Confirmation

- Customers cannot directly mutate a confirmed rental.
- They submit a `ModificationRequest` for administrative review.
- Approval revalidates availability, pricing, delivery capacity, and conflicts.
- Rejection preserves the original rental.

### 16.3 Active Rental Extension

- Customers may request a later return date.
- The system must check subsequent reservations, buffers, and maintenance.
- Approved extensions add the calculated rental cost.
- Rejected extensions preserve the original return time.
- An unapproved late return follows the Late Fee policy.
- Reducing a confirmed rental duration does not automatically reduce the charge; Admin may create a documented manual adjustment.

## 17. Preparation and Handover

### 17.1 Preparation

- Operations employees review required products and quantities.
- Assign serialized units during preparation.
- Record prepared bulk quantities.
- Prevent assignment of unavailable, damaged, retired, or maintenance-blocked units.
- Mark incomplete preparation visibly.

### 17.2 Full Cash Payment

- Payment method in the MVP is Cash only.
- The full rental amount is due at Pickup or Delivery.
- Partial payments are not supported.
- The rental cannot become `Active` before full payment, except through an Admin override with an audited reason.
- Record amount, payment type, receiver, receipt number, time, and notes.
- Do not delete recorded payments.
- Admin corrects an incorrect payment through a reversal entry.
- Each payment generates a cash receipt.

Financial status:

```text
Unpaid → Paid
Paid → Reversed
```

Late fees and damage charges are separate obligations and must each be paid in full.

## 18. Inspections, Returns, Damage, and Missing Items

### 18.1 Checkout Inspection

- Required before a rental becomes Active.
- Record equipment condition, accessories, quantities, and existing damage.
- Photos are optional.
- Record employee confirmation.
- Record customer acknowledgment with name and timestamp in the MVP.

### 18.2 Return Inspection

- Required before completion.
- Record returned, damaged, and missing bulk quantities.
- Inspect serialized units individually.
- Record condition changes and missing accessories.
- Photos are mandatory when damage or loss is reported.

Damage levels:

```text
None
Minor
Major
Lost
BeyondRepair
```

### 18.3 Damage Review

- OperationsEmployee creates a damage report and proposed repair or replacement cost.
- Admin reviews and approves or rejects the charge.
- Approved charges create a separate `DamageCharge`.
- Do not automatically add damage cost without administrative approval.
- An unpaid outstanding damage charge may block future rental submission.
- A rental may be operationally returned while a financial obligation remains open.

## 19. Late Returns and Late Fees

- Each rental has a clear expected return time or collection window.
- Grace period: 2 hours.
- No late fee applies during the grace period.
- After the grace period, any partial 24-hour period is charged as a full additional rental day.
- The fee uses the affected item's daily rate snapshot and quantity.
- Mark affected rentals `Overdue`.
- Notify the customer and administration.
- Mark the rental `Critical Overdue` when the delay threatens a subsequent confirmed rental.
- Admin may create a documented manual charge for actual exceptional costs.

## 20. Maintenance

### 20.1 Maintenance Types

```text
Corrective
Preventive
```

### 20.2 Maintenance Record

Each record includes:

- Product or serialized unit.
- Maintenance type.
- Planned start and end.
- Description.
- Cost.
- Assigned technician or external provider.
- Status.
- Completion notes.

Statuses:

```text
Scheduled → InProgress → Completed
                     └→ Cancelled
```

### 20.3 Maintenance Rules

- Maintenance blocks availability during the overlapping effective period.
- Damaged serialized units may be moved to `UnderMaintenance` after review.
- Do not schedule maintenance over a confirmed rental without resolving the conflict.
- Preventive maintenance is scheduled manually in the MVP.
- Usage-based automatic maintenance belongs to Future Scope.

## 21. Coupons and Discounts

Supported coupon types:

```text
Percentage
FixedAmount
```

Coupon rules:

- Start and end dates.
- Minimum rental subtotal.
- Maximum discount for percentage coupons.
- Total usage limit.
- Per-customer usage limit.
- Active or inactive status.
- One coupon per rental.
- Applies only to Rental Subtotal.
- Does not discount delivery, late fees, damage charges, or manual charges.
- Revalidate at submission and confirmation.
- Save a coupon and discount snapshot on the rental.

Category-specific, product-specific, loyalty, and automatic promotion rules are Future Scope.

## 22. Wishlist and Reviews

### 22.1 Wishlist

- Authenticated customers may save products and packages.
- Wishlist entries do not reserve inventory or preserve prices.
- Availability appears only after a rental period is selected.

### 22.2 Verified Reviews

- Only customers with a completed rental item may submit a review.
- One review per rental item.
- Rating from 1 to 5.
- Optional written review and images.
- Display a `Verified Rental` label.
- Customers may update their review.
- Admin may hide inappropriate content with a recorded reason but may not rewrite the customer's content.
- Reviews target products or packages, not serialized units.
- Delivery-service reviews are Future Scope.

## 23. Authentication and Account Security

### 23.1 Registration

Required fields:

- Full name.
- Email.
- Jordanian phone number.
- Password.
- Password confirmation.
- Acceptance of Terms and Conditions.

### 23.2 Verification and Recovery

- Verify email using an OTP sent through MailKit and configured SMTP provider.
- Email verification is required before rental submission.
- Phone number is required but SMS verification is not implemented in the MVP.
- Operations staff may mark a phone number confirmed after direct contact.
- Support Forgot Password, OTP verification, Reset Password, and OTP resend.

### 23.3 Tokens and Passwords

- Custom JWT Bearer authentication.
- Short-lived access tokens.
- Refresh-token rotation.
- Revoke relevant refresh tokens after password changes.
- Hash passwords using BCrypt.Net-Next.
- Apply rate limits to authentication and OTP endpoints.
- Apply account lockout after configured failed login attempts.
- Social login and SMS OTP are Future Scope.

## 24. Notifications

### 24.1 In-App Notifications

- Read and unread states.
- Real-time delivery through SignalR.
- Deep link to the related rental or administrative record.

### 24.2 Customer Email Notifications

Send emails for:

- Email verification.
- Rental request submission.
- Request confirmation, rejection, cancellation, or expiration.
- Pending hold approaching expiration.
- Pickup, delivery, or collection schedule changes.
- Reminder before rental start.
- Rental activation.
- Reminder before return.
- Overdue rental.
- Approved damage charge.
- Invoice and receipt issuance.

### 24.3 Administrative Notifications

- New pending request.
- Hold approaching expiration.
- Rental starting or due today.
- Overdue or critical-overdue rental.
- New damage report.
- Preparation shortage.
- Availability or schedule conflict requiring action.

Use the Outbox Pattern and Background Workers for reliable asynchronous notification processing.

## 25. Invoices, Receipts, and Charges

The platform must generate:

- Rental invoice.
- Cash receipt.
- Late fee charge.
- Damage charge.
- Manual adjustment record.
- Payment reversal record.

PDF documents are generated with QuestPDF. Documents must show the business identity, customer, rental reference, line-item snapshots, dates, quantities, discounts, delivery fee, total, payment status, and related charge details.

The MVP has no tax, booking advance, security deposit, online payment, or partial payment.

## 26. Dashboards and Reports

### 26.1 Operational Dashboard

- Pending requests.
- Confirmed rentals.
- Rentals starting today.
- Rentals due today.
- Active rentals.
- Overdue rentals.
- Equipment under maintenance.
- Pending damage reviews.

### 26.2 Financial Dashboard

- Revenue today.
- Revenue this month.
- Outstanding rental payments.
- Outstanding damage charges.
- Late fees collected.
- Delivery revenue.
- Discounts applied.

### 26.3 Inventory Dashboard

- Most-rented products.
- Low-availability products.
- Product utilization rate.
- Damaged and missing equipment.
- Maintenance costs.
- Rarely or never rented units.

### 26.4 Reports

- Rentals by date range.
- Revenue by date range.
- Product utilization.
- Customer rental history.
- Cancellations and No-shows.
- Damage and maintenance.
- Outstanding balances.
- CSV export in the MVP.
- PDF invoices and receipts.
- Complex analytical PDF reports are Future Scope.

## 27. Audit Logging

Audit the following sensitive operations:

- Confirming, rejecting, cancelling, or overriding a rental.
- Changing prices or inventory quantities.
- Assigning or replacing serialized units.
- Recording or reversing a payment.
- Approving or rejecting a damage charge.
- Creating manual charges or adjustments.
- Suspending or reactivating customer booking access.
- Changing delivery zones, slot capacities, roles, or system settings.
- Hiding customer reviews.

Each audit record includes:

- Acting user.
- Action.
- Target type and ID.
- Timestamp.
- Relevant old and new values.
- IP address when available.
- Mandatory reason for manual overrides.

## 28. Frontend Requirements

### 28.1 Technology Stack

- Next.js.
- TypeScript.
- Tailwind CSS.
- shadcn/ui.
- TanStack Query for server-state management.
- React Hook Form for forms.
- Zod for client-side schemas.

### 28.2 Frontend Architecture

```text
src/
├── app/                 # Routes, layouts, and route-level loading/error states
├── features/            # Auth, catalog, cart, rentals, delivery, admin, etc.
├── components/
│   ├── ui/              # Reusable design-system primitives
│   └── shared/          # Shared application components
├── services/            # Typed API clients
├── hooks/
├── schemas/
├── types/
├── localization/
└── lib/
```

Use one Next.js application for the public storefront, customer account, operations dashboard, and admin dashboard in the MVP. Separate them through route groups, layouts, authorization boundaries, and feature modules.

### 28.3 Localization Readiness

- MVP UI is English.
- All user-facing strings should be localization-ready.
- Design tokens and layouts must support future RTL.
- Avoid CSS assumptions that prevent direction switching.
- Arabic translation is Future Scope.

### 28.4 Public Storefront Pages

- Home.
- Equipment Catalog.
- Category.
- Search Results.
- Product Details.
- Package List.
- Package Details.
- Wishlist.
- Rental Cart.
- Login.
- Register.
- Verify Email.
- Forgot Password.
- Reset Password.
- Terms and Conditions.
- Rental Policy.
- Contact.

### 28.5 Customer Account Pages

- Customer Dashboard.
- My Rentals.
- Rental Details.
- Modification and Extension Requests.
- Saved Addresses.
- Wishlist.
- Notifications.
- Invoices and Receipts.
- Outstanding Charges.
- Reviews.
- Profile and Security.

### 28.6 Operations Dashboard Pages

- Overview.
- Pending Requests.
- Rental Calendar.
- Preparing Rentals.
- Pickup and Delivery Schedule.
- Active Rentals.
- Returns and Inspections.
- Overdue Rentals.
- Damage Reports.
- Maintenance.
- Cash Payments.

### 28.7 Admin Dashboard Pages

All Operations pages, plus:

- Products.
- Equipment Units.
- Categories.
- Packages.
- Delivery Zones.
- Delivery Slots.
- Coupons.
- Customers.
- Employees.
- Review Moderation.
- Reports.
- Audit Logs.
- System Settings.

### 28.8 UX Requirements

- Responsive layouts for mobile, tablet, laptop, and desktop.
- Clear loading, empty, success, and error states.
- Accessible dialogs, menus, forms, tables, and notifications.
- Visible focus states and keyboard navigation.
- Semantic HTML and properly associated labels.
- `aria-label` for icon-only actions.
- Do not rely on color alone for status.
- Preserve cart and intended return route across login.
- Display server validation and conflict responses clearly.
- Provide retry actions for recoverable failures.

## 29. Backend Requirements

### 29.1 Technology Stack

- .NET 10.
- ASP.NET Core Web API.
- C# using the SDK-supported language version.
- Entity Framework Core 10.
- PostgreSQL with Npgsql.
- MediatR.
- FluentValidation.
- HybridCache with Redis support.
- Custom JWT Bearer authentication.
- BCrypt.Net-Next.
- SignalR.
- MailKit.
- QuestPDF.
- Serilog with Console and optional Seq sinks.
- Swagger/OpenAPI through Swashbuckle.
- ASP.NET Core Rate Limiting.
- Response Compression.
- Health Checks.
- Docker.

### 29.2 Clean Architecture Structure

Use the same five-project structure as LifeDrop:

```text
EquipmentRental/
├── Core/                     # Domain layer
│   ├── Entities/
│   ├── Enums/
│   ├── Events/
│   ├── Common/
│   └── Helpers/
│
├── Services/                 # Application layer
│   ├── Features/
│   ├── Behaviors/
│   ├── Interfaces/
│   └── Abstractions/
│
├── infrastructure/           # Infrastructure layer
│   ├── Data/
│   ├── Repositories/
│   ├── Security/
│   ├── Services/
│   └── Migrations/
│
├── Api/                      # Presentation layer
│   ├── Controllers/
│   ├── Hubs/
│   ├── Middleware/
│   ├── BackgroundWorkers/
│   ├── Extensions/
│   └── Program.cs
│
└── Shared/                   # Shared DTOs, responses, and configuration contracts
```

Dependency direction:

```text
Api -> Services -> Core
Api -> infrastructure -> Services/Core
Shared provides cross-layer DTOs and contracts.
```

### 29.3 Architectural Patterns

- Clean Architecture.
- CQRS with MediatR.
- Generic Repository.
- Unit of Work.
- Result Pattern.
- Domain Events.
- Outbox Pattern.
- Idempotency behavior.
- Optimistic concurrency.
- Manual mapping and LINQ projections.
- Do not use AutoMapper.

### 29.4 MediatR Pipeline

Preserve the LifeDrop behavior order:

```text
UnhandledExceptionBehaviour
→ PerformanceBehaviour
→ ValidationBehavior
→ IdempotencyBehavior
→ CachingBehavior
→ Handler
```

### 29.5 Background Workers

Workers are required for:

- Expiring 12-hour pending holds.
- Processing Outbox messages.
- Sending scheduled reminders.
- Detecting overdue rentals.
- Updating operational alerts where required.

Workers must be cancellation-aware, idempotent, observable, and safe under retries.

### 29.6 Time Abstraction

- Use `TimeProvider` in application and domain-facing time logic.
- Avoid direct reliance on `DateTime.UtcNow` in business rules.
- This is required to keep expiration, grace periods, reminders, availability, and late-fee behavior consistent and manually verifiable.

## 30. Core Data Model

Expected primary entities include:

- Organization.
- User.
- CustomerProfile.
- EmployeeProfile.
- RefreshToken.
- OtpCode.
- Address.
- Branch.
- DeliveryZone.
- DeliverySlot.
- Category.
- EquipmentProduct.
- ProductImage.
- ProductSpecification.
- EquipmentUnit.
- EquipmentPackage.
- PackageItem.
- WishlistItem.
- Rental.
- RentalItem.
- RentalItemSnapshot.
- PackageSnapshot.
- TemporaryHold.
- RentalItemUnitAssignment.
- ModificationRequest.
- ExtensionRequest.
- CheckoutInspection.
- ReturnInspection.
- InspectionImage.
- DamageReport.
- DamageCharge.
- LateFeeCharge.
- ManualCharge.
- Payment.
- PaymentReversal.
- Invoice.
- Receipt.
- MaintenanceRecord.
- Coupon.
- CouponUsage.
- Review.
- Notification.
- OutboxMessage.
- IdempotentRequest.
- AuditLog.

The final database design must define indexes for date-range overlap queries, active status filtering, product and category search, customer rental history, Outbox processing, hold expiration, and operational dashboards.

## 31. API Behavior

### 31.1 Response Format

Use a consistent response wrapper aligned with LifeDrop:

```json
{
  "code": 200,
  "message": "Success",
  "data": {}
}
```

### 31.2 Error Mapping

| Error | HTTP Status |
| --- | --- |
| Validation | 400 Bad Request |
| Unauthorized | 401 Unauthorized |
| Forbidden | 403 Forbidden |
| Not Found | 404 Not Found |
| Conflict or unavailable inventory | 409 Conflict |
| Rate limited | 429 Too Many Requests |
| Unexpected failure | 500 Internal Server Error |

- Expected business failures must use the Result Pattern.
- Use global exception handling for unexpected failures.
- Validation responses must identify fields and actionable messages.
- Availability conflicts must identify affected cart or rental items.

## 32. Non-Functional Requirements

### 32.1 Performance

- Normal catalog and CRUD API requests should target responses below 500 ms under expected MVP load, excluding external services and large file transfers.
- Availability checks must use appropriate database indexes and bounded queries.
- All lists must use pagination.
- Use caching only for suitable reference and catalog data.
- PostgreSQL remains the source of truth for availability.

### 32.2 Reliability

- Use transactions for request submission, confirmation, inventory changes, payment recording, and other multi-record operations.
- Use Outbox processing for reliable side effects.
- Use idempotency for duplicate-sensitive commands.
- Background Workers must tolerate retries.
- Production requires scheduled database backups and a documented restore process.

### 32.3 Security

- Validate authorization at API and application boundaries.
- Validate uploaded file type, size, and permitted extensions.
- Do not commit secrets.
- Use environment variables, user secrets, or a production secret manager.
- Apply CORS allowlists.
- Apply rate limiting to sensitive endpoints.
- Protect customer financial and personal records from cross-account access.
- Log security-sensitive actions without logging passwords, OTP values, tokens, or secrets.

### 32.4 Observability

- Structured logging through Serilog.
- Request logging.
- Performance behavior for slow requests.
- Health-check endpoint.
- Correlation identifiers for requests and asynchronous work where practical.
- Optional Seq sink for centralized log inspection.

### 32.5 Accessibility and Compatibility

- Target WCAG 2.1 AA where practical.
- Support current versions and the previous major version of Chrome, Firefox, Edge, and Safari.
- Responsive behavior across common mobile, tablet, laptop, and desktop widths.

## 33. Manual Testing Strategy

The MVP will not contain automated test projects. Verification is manual through Swagger, Postman, the frontend, PostgreSQL, and Serilog logs.

### 33.1 Required Manual Test Areas

- Registration, email OTP verification, login, refresh, logout, and password reset.
- Role and policy authorization.
- Guest versus Customer access.
- Serialized and quantity-based availability.
- Package availability.
- Overlapping date ranges and turnaround buffers.
- Two concurrent submissions for the same inventory.
- Temporary hold creation, confirmation, rejection, cancellation, and expiration.
- Idempotent request retries.
- Coupon validation and limits.
- Pickup, delivery zone, and slot-capacity selection.
- Modification and extension approval.
- Serialized unit assignment.
- Full cash payment and receipt generation.
- Payment reversal.
- Checkout and return inspection.
- Damage evidence and Admin charge approval.
- Missing bulk quantities.
- Late return grace period and late-fee calculation.
- Corrective and preventive maintenance conflicts.
- Cancellation, late cancellation, No-show count, and customer suspension.
- Wishlist and verified reviews.
- SignalR and email notifications.
- CSV export and PDF document generation.
- Audit-log creation for sensitive actions.
- File upload validation.
- Pagination, filtering, search, and access isolation.

### 33.2 Critical Acceptance Scenarios

1. Two customers cannot successfully reserve the same unavailable quantity for overlapping effective periods.
2. An expired pending request releases its temporary hold.
3. Maintenance removes affected equipment from availability.
4. A package is unavailable when any required component is insufficient.
5. A rental cannot become Active without full payment and checkout inspection unless Admin performs an audited override.
6. Damage charges cannot be created as final charges without Admin approval and required evidence.
7. Late fees begin only after the two-hour grace period.
8. A third No-show suspends new booking access.
9. Customers cannot access another customer's rentals or financial records.
10. Retrying an idempotent command does not create duplicate rentals, payments, or charges.

## 34. MVP Acceptance Criteria

The MVP is complete when:

- Guests can browse and search active equipment and packages.
- Customers can register, verify email, authenticate, and manage addresses.
- Customers can select a period, build a rental cart, check availability, choose fulfillment, apply a coupon, and submit a request.
- The system creates a concurrency-safe 12-hour temporary hold.
- Staff can confirm, reject, prepare, hand over, activate, return, inspect, and complete rentals.
- Serialized and bulk inventory are handled correctly.
- Fixed packages calculate price and availability correctly.
- Pickup and delivery slot rules operate correctly.
- Full cash payments and receipts are recorded.
- Late fees, damage reports, damage charges, and maintenance workflows function.
- Cancellation and No-show policies are enforced.
- Customers receive in-app and email notifications.
- Admin can manage catalog, inventory, packages, zones, slots, coupons, users, settings, reports, reviews, and audit logs.
- The UI is responsive, accessible, English-first, and prepared for future RTL.
- All critical manual test scenarios pass.

## 35. Future Scope

- Arabic translation and RTL activation.
- Multi-vendor marketplace, commissions, payouts, and disputes.
- Multiple branches and inter-branch inventory transfers.
- Online payments and refunds.
- Booking advances and security deposits.
- SMS OTP, SMS notifications, and social login.
- Firebase or native mobile push notifications.
- Installation and removal services.
- Employee-team scheduling.
- On-site equipment operators.
- Driver assignment and route optimization.
- GPS-based delivery pricing.
- Customer-customizable packages.
- Product-specific and category-specific coupon rules.
- Loyalty points and automatic promotions.
- Dynamic specification filters.
- Usage-based preventive maintenance.
- Delivery-service reviews.
- Complex PDF analytical reports.
- Automated testing if added in a later engineering phase.

