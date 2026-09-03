# COOP Backend — API Reference

Complete reference for the COOP backend. This is the single source of truth for building the frontends: Angular (merchant dashboard + admin panel) and Flutter (customer + driver apps).

**Status:** 8 of 9 phases complete · 141 endpoints · 21 controllers · 3 background services · 1 SignalR hub
**Last updated:** 31 August 2026

---

## Contents

1. [Stack and conventions](#1-stack-and-conventions)
2. [Authentication](#2-authentication)
3. [Enums](#3-enums)
4. [Auth endpoints](#4-auth-endpoints)
5. [Merchant profile](#5-merchant-profile)
6. [Merchant branches](#6-merchant-branches)
7. [Verification documents](#7-verification-documents)
8. [Categories](#8-categories)
9. [Products](#9-products)
10. [Offers](#10-offers)
11. [Public marketplace](#11-public-marketplace)
12. [Customer addresses](#12-customer-addresses)
13. [Cart](#13-cart)
14. [Favorites, follows, checkout](#14-favorites-follows-checkout)
15. [Orders (customer)](#15-orders-customer)
16. [Orders (merchant)](#16-orders-merchant)
17. [Payments](#17-payments)
18. [Driver profile](#18-driver-profile)
19. [Delivery tasks](#19-delivery-tasks)
20. [Confirmation codes](#20-confirmation-codes)
21. [Notifications](#21-notifications)
22. [Reviews](#22-reviews)
23. [Complaints](#23-complaints)
24. [Admin panel](#24-admin-panel)
25. [Real-time tracking (SignalR)](#25-real-time-tracking-signalr)
26. [Background services](#26-background-services)
27. [Response conventions](#27-response-conventions)
28. [Frontend integration notes](#28-frontend-integration-notes)
29. [Known gaps](#29-known-gaps)

---

## 1. Stack and conventions

| Item | Value |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| Database | PostgreSQL with PostGIS |
| ORM | EF Core (Npgsql + NetTopologySuite) |
| Auth | JWT Bearer + BCrypt |
| Real-time | SignalR |
| API docs | Swagger / Scalar at `/scalar/v1` |
| Root namespace | `coop` |

### Conventions applied throughout

- **REST routes, no verbs in URLs.** `POST /api/merchants` creates a merchant; there is no `/api/merchants/AddMerchant`.
- **Role guards sit on the controller class**, not on individual methods. Public endpoints are marked `[AllowAnonymous]`.
- **Ownership is enforced inside the query.** A merchant fetching a branch queries by both branch id and their own merchant id, so another merchant's branch returns 404 rather than 403 — no information leaks.
- **Deletes are soft.** `DELETE` sets `isActive = false` rather than removing the row, except where explicitly stated (offer drafts, product images, review deletion).
- **Side effects run after the commit.** Database write → save → commit → SignalR broadcast → notification. Nothing is broadcast before it is persisted.
- **All error messages are in Arabic** and written to be displayed directly to the end user.

---

## 2. Authentication

Every protected request needs:

```
Authorization: Bearer <accessToken>
```

### Token lifecycle

| Token | Lifetime | Behavior |
|---|---|---|
| Access token | 30 minutes | JWT, HMAC-SHA256 signed |
| Refresh token | 30 days | Rotated on every use — the old one is revoked and linked to its replacement |

On a `401`, call `POST /api/auth/refresh` with the stored refresh token and retry the original request. If the refresh also fails, send the user to login.

Changing or resetting a password revokes **every** active refresh token for that user, logging them out on all devices.

### Roles

The role is a claim inside the access token.

| Value | Role | Frontend |
|---|---|---|
| `0` | Customer | Flutter |
| `1` | Merchant | Angular |
| `2` | Driver | Flutter |
| `3` | Admin | Angular |

**Public registration is Customer-only.** `POST /api/auth/register` rejects every role except Customer (`0`) with `'التسجيل العام متاح للزبائن فقط'`. Merchant and Driver accounts can no longer be self-registered — an Admin creates them through the admin panel (§24), with the initial password relayed to the account owner out of band.

---

## 3. Enums

**The API sends and receives integers, not strings.** Map these on the client.

### UserStatus
| Value | Name |
|---|---|
| `0` | Active |
| `1` | Suspended |
| `2` | Deleted |

### VerificationStatus
Used by both `Merchant` and `DriverProfile`.

| Value | Name |
|---|---|
| `0` | Pending |
| `1` | Approved |
| `2` | Rejected |
| `3` | NeedsInformation |

### VerificationCodePurpose
| Value | Name |
|---|---|
| `0` | AccountVerification |
| `1` | PasswordReset |

### OfferStatus
| Value | Name | Notes |
|---|---|---|
| `0` | Draft | Editable and deletable |
| `1` | PendingApproval | Awaiting admin review |
| `2` | Approved | Transient — a background service moves it on within a minute |
| `3` | Rejected | Editable again, can be resubmitted |
| `4` | Scheduled | Approved, start time not yet reached |
| `5` | Active | **The only status visible to customers** |
| `6` | Paused | Merchant paused it manually |
| `7` | SoldOut | Reserved for future use — not currently set |
| `8` | Expired | End time passed |
| `9` | Cancelled | Merchant cancelled it |

### OrderStatus
| Value | Name |
|---|---|
| `0` | PendingPayment |
| `1` | PendingMerchantConfirmation |
| `2` | Accepted |
| `3` | Rejected |
| `4` | Preparing |
| `5` | ReadyForPickup |
| `6` | DriverAssigned |
| `7` | OutForDelivery |
| `8` | Delivered |
| `9` | Completed |
| `10` | Cancelled |
| `11` | DeliveryFailed |

### DeliveryStatus
| Value | Name |
|---|---|
| `0` | SearchingDriver |
| `1` | Offered |
| `2` | Assigned |
| `3` | GoingToMerchant |
| `4` | ArrivedAtMerchant |
| `5` | PickedUp |
| `6` | GoingToCustomer |
| `7` | ArrivedAtCustomer |
| `8` | Delivered |
| `9` | Failed |
| `10` | Cancelled |

### PaymentMethod
| Value | Name |
|---|---|
| `0` | CashOnDelivery |
| `1` | MockOnlinePayment |

### PaymentStatus
| Value | Name |
|---|---|
| `0` | Pending |
| `1` | Paid |
| `2` | Failed |
| `3` | Refunded |

### StockReservationStatus
| Value | Name |
|---|---|
| `0` | Active |
| `1` | Confirmed |
| `2` | Released |
| `3` | Expired |

### DriverTaskOfferStatus
| Value | Name |
|---|---|
| `0` | Pending |
| `1` | Accepted |
| `2` | Rejected |
| `3` | Expired |

### ComplaintStatus
| Value | Name |
|---|---|
| `0` | Open |
| `1` | UnderReview |
| `2` | Resolved |
| `3` | Rejected |

### ReviewStatus
| Value | Name |
|---|---|
| `0` | Visible |
| `1` | Hidden |

### ConfirmationTokenType
| Value | Name |
|---|---|
| `0` | MerchantPickup |
| `1` | CustomerDelivery |

### DevicePlatform
| Value | Name |
|---|---|
| `0` | Android |
| `1` | iOS |
| `2` | Web |

---

## 4. Auth endpoints

Route prefix: `api/auth`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/register` | Public | Create a **Customer** account. Rejects any other role with `'التسجيل العام متاح للزبائن فقط'`. Enforces unique email and phone. |
| POST | `/login` | Public | Sign in. Rejects non-active accounts. |
| POST | `/refresh` | Public | Exchange a refresh token for a new pair, rotating the old one. |
| POST | `/logout` | Authenticated | Revoke the supplied refresh token. |
| GET | `/me` | Authenticated | Current user profile. |
| PUT | `/profile` | Authenticated | Update full name, phone, profile image. Phone uniqueness enforced. |
| PUT | `/change-password` | Authenticated | Verify current password, set a new one, revoke all sessions. |
| POST | `/send-verification-code` | Public | Issue a 6-digit code valid for 10 minutes. |
| POST | `/verify-code` | Public | Validate a code. Maximum 5 attempts per code. |
| POST | `/forgot-password` | Public | Issue a password-reset code. Returns a generic message whether or not the email exists. |
| POST | `/reset-password` | Public | Set a new password using the code. Revokes all sessions. |

### Request bodies

**`POST /register`** — `fullName`, `email`, `phoneNumber`, `password`, `role` (must be `0`/Customer — any other value is rejected)

**`POST /login`** — `email`, `password`

**`POST /refresh`, `POST /logout`** — `refreshToken`

**`PUT /profile`** — `fullName`, `phoneNumber`, `profileImageUrl` (nullable)

**`PUT /change-password`** — `currentPassword`, `newPassword`

**`POST /send-verification-code`** — `destination` (email or phone), `purpose`

**`POST /verify-code`** — `destination`, `code`, `purpose`

**`POST /forgot-password`** — `email`

**`POST /reset-password`** — `email`, `code`, `newPassword`

### Response shape

`register`, `login` and `refresh` all return the same object:

| Field | Type | Notes |
|---|---|---|
| `accessToken` | string | JWT |
| `refreshToken` | string | Store securely; rotates on every refresh |
| `expiresAt` | ISO 8601 UTC | Access token expiry |
| `user` | object | See below |
| `user.id` | GUID | |
| `user.fullName` | string | |
| `user.email` | string | Normalized to lowercase |
| `user.phoneNumber` | string | |
| `user.role` | int | See UserRole |
| `user.status` | int | See UserStatus |
| `user.profileImageUrl` | string, nullable | |

> **Development only:** `send-verification-code` and `forgot-password` return the generated code in the response body as `simulatedCode`, because no real SMS or email provider is wired up. Build the UI as though the code arrives out of band; this field must be removed before any real deployment.

---

## 5. Merchant profile

Route prefix: `api/merchants` · Role: **Merchant**

> **The merchant profile is no longer self-created.** An Admin creates the User (role Merchant) and the Merchant profile together in one transaction via `POST /api/admin/users/merchant` (§24), with `VerificationStatus` set to `Approved` immediately. There is no merchant-facing create endpoint and no resubmission flow — `MerchantsController` never had a `submit-verification` endpoint; an earlier version of this reference documented one in error.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/my` | Current merchant profile. |
| PUT | `/my` | Update general fields only. |
| GET | `/my/verification-status` | Status, rejection reason, verified-at timestamp. **Still exists, but every merchant is created Approved, so this will always return Approved.** Keep it only as a defensive check. |

### Fields

**`PUT /my`** — `name`, `description` (nullable), `contactEmail`, `contactPhone`, `logoUrl` (nullable), `coverImageUrl` (nullable)

> `registrationNumber` and `verificationStatus` are not editable through the profile endpoint.

---

## 6. Merchant branches

Route prefix: `api/merchant-branches` · Role: **Merchant**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Active branches of the current merchant. |
| POST | `/` | Add a branch. **Requires `verificationStatus == 1` (Approved)** — otherwise 403. The first branch is automatically the main branch. Every merchant is now created Approved (§5), so this always passes in practice; the check remains as a guard for a suspended account. |
| GET | `/{id}` | Branch details. |
| PUT | `/{id}` | Update branch. |
| DELETE | `/{id}` | Deactivate. The main branch cannot be deactivated — 400. |
| PUT | `/{id}/set-main` | Make this the main branch and clear the flag on all siblings. A deactivated branch cannot be made main. |

### Fields

`name`, `address`, `city`, `area`, `latitude`, `longitude`, `phoneNumber`, `openingTime`, `closingTime`, `deliveryRadiusKm`, `minimumOrderAmount`, `baseDeliveryFee`

`openingTime` and `closingTime` are `TimeOnly` — serialized as `"HH:mm:ss"`.

### Architectural decision

**One account = one merchant = many branches.** There is no separate login per branch; the owner manages every branch from one dashboard. Per-branch staff accounts are a possible future feature, not built.

There is also **no temporary open/closed toggle** on a branch and no `isOpen` field. Availability is controlled entirely through offers. `isActive` exists only for soft deletion.

---

## 7. Verification documents

Route prefix: `api/verification-documents` · Role: **Merchant** or **Driver**

| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Upload a document (multipart/form-data). Linked to the caller's merchant or driver profile automatically. |
| GET | `/my` | Documents belonging to the current user. |
| GET | `/{id}` | Document details. |
| DELETE | `/{id}` | Delete — allowed only while status is Pending. |

### Decision

> **Nothing reviews these documents any more.** Merchant and driver accounts are created pre-Approved by an Admin (§24), so upload is optional record-keeping, not an approval gate. There is no endpoint to approve an individual document, and no queue that reads them.

---

## 8. Categories

Route prefix: `api/categories`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Public | Active category tree, ordered by `displayOrder`. |
| GET | `/{id}/offers` | Public | Offers within a category. |
| POST | `/` | Admin | Create a category. |
| PUT | `/{id}` | Admin | Update a category. |
| DELETE | `/{id}` | Admin | Deactivate a category. |

### Fields

`parentCategoryId` (nullable — makes it a tree), `nameEn`, `nameAr`, `description` (nullable), `imageUrl` (nullable), `displayOrder`, `isActive`

Both `nameEn` and `nameAr` are stored, so the UI can switch language later without a data migration.

---

## 9. Products

Route prefix: `api/products` · Role: **Merchant**

| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Create a product. Requires an approved merchant and an existing active category. Every merchant is now created Approved (§5), so this always passes in practice; the check remains as a guard for a suspended account. |
| GET | `/my` | Active products of the current merchant. |
| GET | `/{id}` | Product details **including its images**, ordered by `displayOrder`. |
| PUT | `/{id}` | Update product. Category is re-validated. |
| DELETE | `/{id}` | Deactivate product. |
| POST | `/{id}/images` | Add an image. |
| DELETE | `/{id}/images/{imageId}` | Remove an image (hard delete). |

### Fields

**Product** — `categoryId`, `name`, `description`, `sku` (nullable), `brand` (nullable), `mainImageUrl` (nullable), `isActive` (on update only)

**Image** — `fileUrl`, `displayOrder`

A product is a catalog entry. It is not purchasable on its own — only offers built on top of a product are.

---

## 10. Offers

Route prefix: `api/offers` · Role: **Merchant**

An offer is the sellable unit. It wraps a product with a discounted price, a validity window, and per-branch stock.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Create as Draft. |
| GET | `/my` | All offers of the current merchant, newest first. |
| GET | `/{id}` | Offer details **plus its branches and their stock levels**. |
| PUT | `/{id}` | Update — only while Draft or Rejected. |
| DELETE | `/{id}` | Hard delete — only while Draft. Use cancel otherwise. |
| POST | `/{id}/branches` | Attach a branch with a stock quantity. Rejects duplicates. |
| PUT | `/{id}/branches/{branchOfferId}` | Update stock and availability. New stock cannot go below reserved + sold. |
| DELETE | `/{id}/branches/{branchOfferId}` | Detach a branch — blocked while it holds reserved stock. |
| POST | `/{id}/submit` | Submit for admin review. |
| POST | `/{id}/pause` | Pause an Active or Scheduled offer. |
| POST | `/{id}/resume` | Resume a paused offer. |
| POST | `/{id}/cancel` | Cancel permanently — blocked while any branch holds reserved stock. |

### Fields

**Offer** — `productId`, `title`, `description` (nullable), `originalPrice`, `discountedPrice`, `startAt`, `endAt`, `maximumQuantityPerCustomer` (nullable)

**Branch stock (create)** — `merchantBranchId`, `totalStock`

**Branch stock (update)** — `totalStock`, `isAvailable`

### Validation rules

- `0 < discountedPrice < originalPrice`
- `startAt < endAt`
- `endAt` must be in the future
- **`discountPercentage` is computed server-side and never accepted from the request.** Read it, do not send it.
- `submit` requires at least one attached branch with stock, and refuses if `endAt` has already passed.

### Status lifecycle

```
Draft ──submit──> PendingApproval ──admin approve──> Approved
                        │                                │
                   admin reject                   (background service, ≤1 min)
                        ↓                                ↓
                    Rejected                    Scheduled ──startAt──> Active
                        │                                                 │
                   edit + resubmit                              pause ⇄ resume
                                                                          │
                                                              endAt ──> Expired
```

`Cancelled` is reachable from any status except Cancelled and Expired.

### Stock model

Available stock is always computed, never stored:

```
available = totalStock − reservedStock − soldStock
```

`totalStock` is never decremented by the system. Reservations move quantity between the reserved and sold counters.

---

## 11. Public marketplace

Route prefix: `api/marketplace` · **Public — no authentication**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/offers` | Search and filter active offers, paginated. |
| GET | `/offers/nearby` | Offers near a coordinate, ranked by distance (PostGIS). |
| GET | `/offers/ending-soon` | Offers close to their end time. |
| GET | `/offers/top-discounts` | Highest discount percentages. |
| GET | `/offers/{id}` | Offer details with per-branch availability. |
| GET | `/merchants` | Search merchants. |
| GET | `/merchants/{id}` | Merchant storefront. |
| GET | `/merchants/{id}/offers` | Offers of one merchant. |

### Query parameters

**`/offers`** — `search`, `categoryId`, `merchantId`, `city`, `minimumDiscount`, `minPrice`, `maxPrice`, `sortBy`, `pageNumber`, `pageSize`

**`/offers/nearby`** — `lat`, `lng`, `radiusKm`

Only offers with status `Active` (5) appear in any marketplace endpoint.

---

## 12. Customer addresses

Route prefix: `api/addresses` · Role: **Customer**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Addresses of the current customer. |
| POST | `/` | Add an address. The first one becomes the default automatically. |
| GET | `/{id}` | Address details. |
| PUT | `/{id}` | Update address. |
| DELETE | `/{id}` | Delete address. |
| PUT | `/{id}/set-default` | Set as the default address. |

### Fields

`label`, `contactName`, `contactPhone`, `city`, `area`, `street`, `building` (nullable), `floor` (nullable), `additionalDirections` (nullable), `latitude`, `longitude`

The address is captured with coordinates because the driver app navigates to them and the delivery-fee calculation uses them.

---

## 13. Cart

Route prefix: `api/cart` · Role: **Customer**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Current cart with computed totals. |
| POST | `/items` | Add an offer to the cart. |
| PUT | `/items/{itemId}` | Change quantity. |
| DELETE | `/items/{itemId}` | Remove an item. Removing the last item deletes the cart. |
| DELETE | `/` | Empty the cart. |
| GET | `/validate` | **Pre-checkout gate.** Call this before showing the payment screen. |

### Request bodies

**`POST /items`** — `offerId`, `quantity`

**`PUT /items/{itemId}`** — `quantity`

### Cart response

| Field | Type | Notes |
|---|---|---|
| `id` | GUID | `Guid.Empty` when the cart is empty |
| `merchantBranchId` | GUID | The single branch this cart is bound to |
| `items` | array | |
| `items[].id` | GUID | Cart item id, used for update and remove |
| `items[].offerId` | GUID | |
| `items[].title` | string | Offer title |
| `items[].quantity` | int | |
| `items[].unitPrice` | decimal | Current discounted price |
| `items[].lineTotal` | decimal | `unitPrice × quantity` |
| `subtotal` | decimal | Sum of **original** prices |
| `totalDiscount` | decimal | Sum of savings |
| `estimatedTotal` | decimal | `subtotal − totalDiscount`, before delivery fee |

An empty cart returns this same shape with an empty item list — **not a 404**.

### Validate response

| Field | Type | Notes |
|---|---|---|
| `isValid` | bool | `false` if any issue exists |
| `issues` | string array | Arabic messages, ready to display |
| `cart` | object | Same shape as above |

`validate` checks: offer still Active, branch still active, sufficient stock, per-customer quantity limit, branch minimum order amount, and **price drift** — whether the offer price changed since the item was added.

### Single-branch rule

A cart is bound to exactly one `merchantBranchId`. Adding an offer from a different branch returns 400 with a message telling the customer to empty the cart first. Surface this clearly in the UI before the customer gets there.

When the cart is empty, the branch is chosen automatically — the one with the most available stock for that offer. The customer does not pick a branch. Display the chosen branch name in the cart.

The cart expires after **24 hours**; the window is refreshed on every mutation.

---

## 14. Favorites, follows, checkout

Role: **Customer**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/favorites` | Favorited offers. |
| POST | `/api/favorites/{offerId}` | Add to favorites. |
| DELETE | `/api/favorites/{offerId}` | Remove from favorites. |
| GET | `/api/follows` | Followed merchants. |
| POST | `/api/follows/{merchantId}` | Follow a merchant. |
| DELETE | `/api/follows/{merchantId}` | Unfollow. |
| POST | `/api/checkout/calculate` | Compute subtotal, delivery fee and total before placing the order. |

---

## 15. Orders (customer)

Route prefix: `api/orders` · Role: **Customer**

| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Place the order from the current cart. |
| GET | `/` | Order history, newest first. |
| GET | `/{id}` | Order details with line items. |
| GET | `/{id}/tracking` | Status, timestamps, driver details, full status history. |
| POST | `/{id}/cancel` | Cancel — allowed only through `Accepted`. |
| POST | `/{id}/delivery-code` | Generate the delivery code to read to the driver. |
| POST | `/{id}/confirm-delivery` | Confirm receipt and close the order. |

### Request bodies

**`POST /`** — `customerAddressId`, `paymentMethod`, `customerNotes` (nullable)

**`POST /{id}/cancel`** — `reason` (nullable)

**`POST /{id}/delivery-code`** — no body. Returns `code` and `expiresAt`.

### Place-order response

`id`, `orderNumber`, `status`, `totalAmount`

Order numbers follow the format `COOP-yyyyMMdd-XXXXXX`.

### What `POST /` actually does

This is the most delicate endpoint in the system. It runs inside a database transaction and does all of the following atomically:

1. Validates the address belongs to the customer
2. Validates the cart exists, is non-empty, and its branch is active
3. Re-validates **every** cart item — offer still active, branch stock sufficient
4. Creates the `Order` with a generated order number
5. Creates one `OrderItem` per cart item, snapshotting the product name and both prices
6. Increments `reservedStock` on each branch offer
7. Creates a `StockReservation` per item with a 30-minute expiry
8. Enforces the branch minimum order amount
9. Writes the first `OrderStatusHistory` row
10. Deletes the cart and its items

If any step fails, nothing is written.

Initial status depends on payment method: `MockOnlinePayment` starts at `PendingPayment` (0), `CashOnDelivery` starts at `PendingMerchantConfirmation` (1).

### Order status flow

```
PendingPayment / PendingMerchantConfirmation
        │ merchant accepts
        ↓
   Preparing                    ← delivery task is created here
        │ merchant marks ready
        ↓
 ReadyForPickup                 ← pickup code is generated
        │ driver accepts the task
        ↓
 DriverAssigned
        │ driver confirms pickup with the merchant's code
        ↓
 OutForDelivery
        │ driver completes with the customer's code
        ↓
   Delivered                    ← reserved stock becomes sold
        │ customer confirms
        ↓
   Completed
```

**Branch exits:**

| Status | Triggered by |
|---|---|
| `Rejected` | Merchant rejects the order |
| `Cancelled` | Customer cancels, or the reservation times out before the merchant accepts |
| `DeliveryFailed` | Driver reports a failed delivery |

Cancellation is allowed only while the status is `PendingPayment`, `PendingMerchantConfirmation` or `Accepted`. Once `Preparing`, the customer cannot cancel.

### Stock lifecycle

| Event | Effect |
|---|---|
| Order placed | `reservedStock +=` quantity, reservation valid 30 minutes |
| Delivery completed | `reservedStock −=`, `soldStock +=` |
| Cancelled / rejected / failed / reservation expired | `reservedStock −=` only |

---

## 16. Orders (merchant)

Route prefix: `api/merchant-orders` · Role: **Merchant**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Orders across the merchant's branches, filterable by status. |
| GET | `/{id}` | Order details. |
| POST | `/{id}/accept` | Accept and begin preparing. **Creates the delivery task**, which starts the driver search. |
| POST | `/{id}/reject` | Reject — releases reserved stock and refunds any completed payment. |
| POST | `/{id}/ready` | Mark ready. **Returns the pickup code in the response.** |
| POST | `/{id}/pickup-code` | Re-issue the pickup code, revoking the previous one. |

### Request bodies

**`POST /{id}/reject`** — `reason`

**`POST /{id}/ready`** — no body. Returns an object containing the order plus `pickupCode`.

**`POST /{id}/pickup-code`** — no body. Returns `code` and `expiresAt`.

### Design note

The delivery task is created on **accept**, not on **ready**. This lets the driver search and travel to the branch while the merchant is still preparing the order. As a consequence, `confirm-pickup` on the driver side checks that the order actually reached `ReadyForPickup` or `DriverAssigned` before allowing handover.

---

## 17. Payments

Route prefix: `api/payments`

| Method | Endpoint | Description |
|---|---|---|
| POST | `/mock-charge` | Simulated online payment. |
| GET | `/{orderId}` | Payment status for an order. |
| POST | `/{id}/refund` | Simulated refund. |

There is no real payment gateway and no card data is collected anywhere in the system. Cash-on-delivery payments are marked `Paid` automatically when the driver completes the delivery.

---

## 18. Driver profile

Route prefix: `api/drivers` · Role: **Driver**

> **The driver profile is no longer self-created.** An Admin creates the User (role Driver) and the DriverProfile together in one transaction via `POST /api/admin/users/driver` (§24), with `VerificationStatus` set to `Approved` immediately, `isAvailable` false, and `CompletedDeliveries` 0.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/my` | Current driver profile. |
| PUT | `/my` | Update vehicle details. |
| GET | `/my/verification-status` | Verification status. **Still exists, but every driver is created Approved, so this will always return Approved.** Keep it only as a defensive check. |
| POST | `/my/go-online` | Start a shift — sets `isAvailable = true`. |
| POST | `/my/go-offline` | End a shift. |
| PUT | `/my/location` | Update current position. |
| GET | `/my/schedule` | Weekly shift schedule. |
| POST | `/my/schedule` | Add a shift slot. |
| PUT | `/my/schedule/{id}` | Update a shift slot. |
| DELETE | `/my/schedule/{id}` | Delete a shift slot. |
| GET | `/my/stats` | Delivery counts and average rating. |

### Request bodies

**`PUT /my`** — `vehicleType`, `vehiclePlateNumber`, `maximumCapacity`

**`PUT /my/location`** — `latitude`, `longitude`

**`POST /my/schedule`** — `dayOfWeek`, `startTime`, `endTime`

**`PUT /my/schedule/{id}`** — `startTime`, `endTime`, `isActive`

`dayOfWeek` is `0` for Sunday through `6` for Saturday. Overlapping slots on the same weekday are rejected with 409.

### Important behaviors

- **`go-online` requires two things:** verification status Approved, **and** a known current location. Call `PUT /my/location` before `go-online` or it returns 400.
- **`go-offline` is refused** while the driver has an active delivery task.
- `PUT /my/location` always updates the profile's current coordinates, but only appends a location history row **while a task is active** — tracking is not continuous by design. During an active task it also broadcasts the position over SignalR to the customer.
- **`DriverAvailability` is a weekly shift schedule, not an online/offline flag.** Live availability is the `isAvailable` boolean on the profile, toggled by go-online and go-offline.

---

## 19. Delivery tasks

Route prefix: `api/delivery-tasks` · Role: **Driver**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/offers` | Pending task offers for this driver. |
| POST | `/offers/{id}/accept` | Accept a task. Expires every competing offer for the same task. |
| POST | `/offers/{id}/decline` | Decline. The driver is excluded from future rounds for this task. |
| GET | `/my` | Active tasks. |
| GET | `/{id}` | Full task details — everything the driver app needs on one screen. |
| POST | `/{id}/arrived-at-merchant` | Mark arrival at the branch. |
| POST | `/{id}/confirm-pickup` | Confirm handover using the merchant's code. |
| POST | `/{id}/arrived-at-customer` | Mark arrival at the customer address. |
| POST | `/{id}/complete` | Complete using the customer's code. |
| POST | `/{id}/report-failure` | Report a failed delivery. |

### Request bodies

**`POST /offers/{id}/decline`** — `reason` (nullable)

**`POST /{id}/confirm-pickup`** — `code`

**`POST /{id}/complete`** — `code`

**`POST /{id}/report-failure`** — `reason`

### Task details response

`GET /{id}` returns everything the driver needs without further calls:

| Group | Fields |
|---|---|
| Task | `id`, `orderId`, `orderNumber`, `status`, `deliveryFee`, `driverEarning` |
| Pickup | `branchName`, `branchAddress`, `branchPhone`, `branchLatitude`, `branchLongitude` |
| Dropoff | `customerName`, `customerPhone`, `customerCity`, `customerArea`, `customerStreet`, `customerBuilding`, `customerFloor`, `additionalDirections`, `customerLatitude`, `customerLongitude` |
| Payment | `paymentMethod`, `amountToCollect` — the full order total for cash orders, `0` for prepaid |
| Timestamps | `assignedAt`, `arrivedAtMerchantAt`, `pickedUpAt`, `arrivedAtCustomerAt` |

### Task flow

```
SearchingDriver
      │ matching service issues an offer (2-minute window)
      ↓
   Assigned  ──arrived-at-merchant──>  ArrivedAtMerchant
                                              │ confirm-pickup (merchant code)
                                              ↓
                                          PickedUp
                                              │ arrived-at-customer
                                              ↓
                                      ArrivedAtCustomer
                                              │ complete (customer code)
                                              ↓
                                          Delivered
```

`report-failure` moves the task to `Failed` from any non-terminal state and releases the reserved stock.

### Constraints

- A driver can hold only **one active task at a time**. Accepting a second returns 400.
- Accepting a task that another driver already took returns 400.
- An expired offer cannot be accepted — it is marked Expired and returns 400.
- `complete` and `report-failure` run inside a transaction because they move stock.

---

## 20. Confirmation codes

Two separate 6-digit codes protect the two handover points. Both are stored SHA256-hashed, are single-use, and expire after 30 minutes.

| Code | Generated by | Read aloud to | Consumed by |
|---|---|---|---|
| **Pickup** (`MerchantPickup`) | Merchant, via `POST /api/merchant-orders/{id}/ready` or `/pickup-code` | The driver | `POST /api/delivery-tasks/{id}/confirm-pickup` |
| **Delivery** (`CustomerDelivery`) | Customer, via `POST /api/orders/{id}/delivery-code` | The driver | `POST /api/delivery-tasks/{id}/complete` |

Generating a new code revokes any outstanding unused code of the same type for that task.

### Security decision

**Neither code is broadcast over SignalR.** The order group contains the customer, the merchant *and* the driver — pushing the delivery code into that group would hand it to the person it is meant to verify. Each party pulls its own code over HTTP instead.

### UI implications

- **Merchant app:** show the pickup code prominently on the order screen after marking it ready, with a button to re-issue.
- **Customer app:** a "Show delivery code" button that appears once the task is out for delivery.
- **Driver app:** a code entry field at two points in the flow — pickup and delivery.

---

## 21. Notifications

Route prefix: `api/notifications` · Role: **Authenticated**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Latest 50 notifications, newest first. |
| GET | `/unread-count` | Unread count for the badge. |
| PUT | `/{id}/read` | Mark one as read. |
| PUT | `/read-all` | Mark all as read. |
| DELETE | `/{id}` | Delete a notification. |
| POST | `/device-tokens` | Register an FCM device token. |
| DELETE | `/device-tokens/{token}` | Unregister a device token. |

### Request body

**`POST /device-tokens`** — `token`, `platform`

Registering a token that already exists re-points it at the current user, which handles a shared device correctly.

### Notification object

| Field | Type | Notes |
|---|---|---|
| `id` | GUID | |
| `title` | string | Arabic |
| `message` | string | Arabic |
| `type` | string | Not an enum — see the list below |
| `isRead` | bool | |
| `createdAt` | ISO 8601 UTC | |

The stored notification also carries `relatedEntityType` and `relatedEntityId` — use these to deep-link into the right screen.

### Notification types

| Type | Sent to | Trigger |
|---|---|---|
| `OrderAccepted` | Customer | Merchant accepts |
| `OrderRejected` | Customer | Merchant rejects |
| `OrderReady` | Customer | Merchant marks ready |
| `DriverAssigned` | Customer, Merchant | Driver accepts the task |
| `DriverArrived` | Merchant | Driver reaches the branch |
| `DriverArrivedAtCustomer` | Customer | Driver reaches the address |
| `OutForDelivery` | Customer | Driver confirms pickup |
| `OrderDelivered` | Customer, Merchant | Driver completes |
| `DeliveryFailed` | Customer, Merchant | Driver reports failure |
| `DeliveryTaskOffered` | Driver | Matching service issues an offer |
| `MerchantApproved` / `MerchantRejected` | Merchant owner | Admin decision |
| `DriverApproved` / `DriverRejected` | Driver | Admin decision |
| `OfferApproved` / `OfferRejected` | Merchant owner | Admin decision |
| `ComplaintResolved` | Complainant | Admin resolves |

> **FCM push is currently a stub.** Notifications are written to the database but not actually delivered to devices — the push method logs instead of sending. Until Firebase is wired up, the frontends must rely on polling `GET /` and on SignalR.

---

## 22. Reviews

Route prefix: `api/reviews`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/` | Customer | Rate the merchant and driver after delivery. One review per order. |
| GET | `/merchant/{id}` | Public | Reviews for a merchant, paginated. |
| GET | `/my` | Customer | Reviews written by the current customer. |
| PUT | `/{id}` | Customer | Edit — within 24 hours of creation only. |
| DELETE | `/{id}` | Customer | Delete — within 24 hours of creation only. |

### Request bodies

**`POST /`** — `orderId`, `merchantRating`, `driverRating` (nullable), `comment` (nullable)

**`PUT /{id}`** — `merchantRating`, `driverRating` (nullable), `comment` (nullable)

### Rules

- Ratings are integers from 1 to 5.
- The order must be `Delivered` or `Completed`.
- `driverRating` is ignored if no driver was assigned to the order.
- Creating, editing or deleting a review **recomputes** `averageRating` on the merchant and driver from all visible reviews — it is not a running average.

### Query parameters

**`GET /merchant/{id}`** — `pageNumber` (default 1), `pageSize` (default 20, max 100)

---

## 23. Complaints

Route prefix: `api/complaints` · Role: **Authenticated**

| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | File a complaint. |
| GET | `/my` | Complaints filed by the current user, filterable by `?status=`. |
| GET | `/{id}` | Complaint details. |

### Request body

**`POST /`** — `orderId` (nullable), `merchantId` (nullable), `driverProfileId` (nullable), `offerId` (nullable), `category`, `description`, `evidenceUrl` (nullable)

At least one of the four target ids must be non-null. If `orderId` is supplied, the caller must be either the customer or the merchant owner on that order.

### Response object

`id`, `orderNumber`, `targetName`, `category`, `description`, `evidenceUrl`, `status`, `adminResponse`, `createdAt`, `resolvedAt`

---

## 24. Admin panel

Route prefix: `api/admin` · Role: **Admin**

> **The merchant/driver verification queue is gone.** There is no longer a review step between account creation and going live — an Admin creates Merchant and Driver accounts directly, pre-Approved. User management (below) replaces it.

### User management

| Method | Endpoint | Description |
|---|---|---|
| POST | `/users/merchant` | Creates a User (role Merchant) + Merchant profile in one transaction. `VerificationStatus` is set to Approved immediately, with `VerifiedAt` and `VerifiedByUserId` recorded. Returns 201. |
| POST | `/users/driver` | Creates a User (role Driver) + DriverProfile in one transaction. `VerificationStatus` Approved, `IsAvailable` false, `CompletedDeliveries` 0. Returns 201. |
| GET | `/users` | Paginated user list. Query: `role`, `status`, `search`, `pageNumber` (default 1), `pageSize` (default 20, max 100). Returns `{ items, totalCount, pageNumber, pageSize }`. |
| PUT | `/users/{id}/suspend` | Body `{ reason }` (required). Sets `UserStatus.Suspended`. Refuses self-suspension, refuses Admin accounts, refuses an already-suspended account. Returns 204. |
| PUT | `/users/{id}/activate` | No body. Sets `UserStatus.Active`. Refuses an already-active account and refuses a Deleted account. Returns 204. |

**`POST /users/merchant`** — `fullName`, `email`, `phoneNumber`, `password`, `merchantName`, `description` (nullable), `registrationNumber` (nullable), `contactEmail`, `contactPhone`, `logoUrl` (nullable), `coverImageUrl` (nullable)

**`POST /users/driver`** — `fullName`, `email`, `phoneNumber`, `password`, `vehicleType`, `vehiclePlateNumber`, `maximumCapacity`

For both: the admin sets the initial password and passes it to the account owner out of band. The new user receives an in-app notification of type `AccountCreated` telling them to change it. Password minimum length is 6. Email and phone uniqueness are enforced, returning 409.

### Offer moderation

Unchanged — offers still require admin approval before going live.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/offers/pending` | Offers awaiting approval. |
| POST | `/offers/{id}/approve` | Approve — sets the offer to Scheduled or Active depending on its start time. |
| POST | `/offers/{id}/reject` | Reject with a review note. |

### Complaints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/complaints` | All complaints, filterable by `?status=`. |
| PUT | `/complaints/{id}/resolve` | Resolve with a written response. |

### Request bodies

**`PUT /users/{id}/suspend`** — `reason`

**Offer reject** — `reason`

**`PUT /complaints/{id}/resolve`** — `status`, `adminResponse`

Every admin decision sends a notification to the affected user.

---

## 25. Real-time tracking (SignalR)

### Connecting

```
/hubs/tracking?access_token=<accessToken>
```

The token goes in the query string because browsers cannot set an `Authorization` header on a WebSocket handshake. The server reads `access_token` for any path starting with `/hubs`.

### Hub methods

| Method | Parameters | Description |
|---|---|---|
| `JoinOrderGroup` | `orderId` | Subscribe to updates for one order. |
| `LeaveOrderGroup` | `orderId` | Unsubscribe. |

Group membership is authorized on join. The caller must be the order's customer, the merchant owner, the assigned driver, or an admin. Anyone else gets a `HubException`.

### Events

**`order.status.changed`**

| Field | Type |
|---|---|
| `orderId` | GUID |
| `orderNumber` | string |
| `oldStatus` | int, sometimes omitted |
| `newStatus` | int |
| `changedAt` | ISO 8601 UTC |

**`delivery.driver.assigned`**

| Field | Type |
|---|---|
| `orderId` | GUID |
| `deliveryTaskId` | GUID |
| `driverName` | string |
| `driverPhone` | string |
| `vehicleType` | string |
| `vehiclePlateNumber` | string |
| `assignedAt` | ISO 8601 UTC |

**`delivery.status.changed`**

| Field | Type |
|---|---|
| `orderId` | GUID |
| `deliveryTaskId` | GUID |
| `newStatus` | int (DeliveryStatus) |
| `failureReason` | string — only when the status is Failed |
| `changedAt` | ISO 8601 UTC |

**`delivery.location.updated`**

| Field | Type |
|---|---|
| `orderId` | GUID |
| `deliveryTaskId` | GUID |
| `latitude` | double |
| `longitude` | double |
| `recordedAt` | ISO 8601 UTC |

Location events fire only while a task is active, at whatever rate the driver app calls `PUT /api/drivers/my/location`.

---

## 26. Background services

Three hosted services run on a one-minute tick.

| Service | What it does |
|---|---|
| `OfferStatusService` | Moves offers between statuses based on time: Approved becomes Scheduled or Active depending on `startAt`; Scheduled becomes Active when the start time arrives; anything past `endAt` becomes Expired. |
| `StockReservationCleanupService` | Releases stock reservations past their 30-minute expiry and cancels the orders holding them — but **only** while the order is still `PendingPayment` or `PendingMerchantConfirmation`. Once the merchant accepts, the reservation holds until delivery. |
| `DriverMatchingService` | Expires stale task offers, then for each task still searching finds the nearest approved, available, idle driver within 15 km using a PostGIS query, issues a single offer with a two-minute window, and notifies that driver. Drivers who declined or timed out are excluded from later rounds. |

**Frontend implication:** order and offer state can change on its own, with no user action. Rely on SignalR while a screen is open, and refetch on screen focus.

---

## 27. Response conventions

### Success codes

| Code | Used for |
|---|---|
| `200 OK` | Reads and updates |
| `201 Created` | Resource creation |
| `204 No Content` | Deletes and deactivations |

### Error codes

| Code | Meaning | Example situation |
|---|---|---|
| `400` | Validation or business-rule failure | Requested quantity exceeds available stock |
| `401` | Missing or expired token | Access token expired — refresh and retry |
| `403` | Role or state forbids the action | Merchant not yet verified tries to add a branch |
| `404` | Not found, **or not owned by the caller** | Order belongs to a different customer |
| `409` | Conflict | Account already has a merchant profile |

Error bodies are plain Arabic strings suitable for direct display.

> **Note on 404:** ownership is enforced inside the query, so requesting a resource that exists but belongs to someone else returns 404, not 403. This is deliberate — it prevents probing for the existence of other users' data.

---

## 28. Frontend integration notes

1. **All timestamps are UTC** in ISO 8601. Convert to local time for display.
2. **`decimal` values arrive as JSON numbers.** Be careful with floating-point arithmetic on money in JavaScript; prefer computing totals server-side where possible.
3. **`TimeOnly` values arrive as `"HH:mm:ss"` strings** — branch opening hours and driver shift slots.
4. **Enums are integers.** Build a mapping layer; do not compare against strings.
5. **Role guards are enforced server-side.** Do not render a screen the user's role cannot reach — the API will return 403.
6. **The cart is bound to one branch.** Handle the mixed-branch rejection with a clear prompt rather than a raw error toast.
7. **Always call `GET /api/cart/validate` before the payment screen.** Offers expire and stock runs out while the customer browses.
8. **Confirmation codes need dedicated UI.** Merchant: display the pickup code. Customer: a button to reveal the delivery code. Driver: a code entry field at two points.
9. **Poll notifications.** FCM push is not yet live, so the notification bell needs `GET /api/notifications/unread-count` on an interval or on screen focus.
10. **Arabic RTL is the first-phase UI language.** Categories carry both `nameEn` and `nameAr`, so an English UI is a later frontend change, not a backend one.
11. **Driver location must be set before going online.** Request location permission and push a position before enabling the go-online button.
12. **The tracking map needs two sources:** `GET /api/orders/{id}/tracking` for the initial state, then SignalR for live updates.

---

## 29. Known gaps

Accepted for this stage, but each needs closing before a real deployment.

| Gap | Impact on the frontend |
|---|---|
| Verification codes are returned in the API response as `simulatedCode` | Usable for testing. Build the UI as though the code arrives by SMS. **A direct security hole if shipped.** |
| FCM push is a stub | Notifications are stored but never reach the device. Poll instead. |
| `AuditLog` table exists but nothing writes to it | No admin activity log screen is possible yet. |
| No rate limiting anywhere | Verification codes cap at 5 attempts per code, but nothing throttles request volume. |
| The customer cannot choose a branch | Display the auto-selected branch name in the cart, with no change option. |
| `OfferStatus.SoldOut` is never set | Treat a zero-availability Active offer as sold out on the client. |
| `DriverProfile` earnings and delivery history endpoints are not built | The DTOs exist but no endpoint serves them. |
