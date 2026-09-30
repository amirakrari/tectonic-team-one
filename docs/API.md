# API reference

[README](../README.md) | [Operations](OPERATIONS.md) | [Architecture](TECHNICAL.md)

This is the implemented contract in `src/Tectonic.API`. Examples assume the local
API at `http://127.0.0.1:5000`; change the base URL when using another listener.
Swagger is at `/swagger`, with the machine-readable document at
`/swagger/v1/swagger.json`.

## Conventions

- JSON names are camel-case; dates are `YYYY-MM-DD`.
- Transaction amounts are positive, two-decimal EUR values.
- `type` is a seeded code: `expense` or `income`.
- IDs in lookup tables are integers. Identity user IDs are strings.
- Ownership comes from the validated JWT `sub`, never from request JSON.
- All endpoints below except signup/login require `Authorization: Bearer <token>`.
- Responses are explicit records, not serialized EF navigation graphs.

## Authentication

### POST `/api/auth/signup`

```json
{
  "email": "presenter@example.test",
  "password": "<your disposable password>"
}
```

Success: **201** with:

```json
{
  "accessToken": "<signed JWT>",
  "tokenType": "Bearer",
  "expiresAt": "<UTC timestamp>"
}
```

Signup creates an Identity account, its `User` role assignment, three enabled
email preferences, and its own January 1, 2026 clock in one transaction.
Missing fields, duplicate email, or Identity validation failures return **400**:

```json
{"errors":[{"code":"<Identity error code>","description":"<explanation>"}]}
```

Default password requirements: six or more characters, uppercase, lowercase,
digit and non-alphanumeric character. Email ownership is not verified.

### POST `/api/auth/login`

Accepts the same email/password body. Success: **200** with the same bearer
response. Missing required fields return **400**; wrong password and unknown
email both return **401** with:

```json
{"error":"Invalid email or password."}
```

JWTs expire after 60 actual UTC minutes. The API checks signature, HS256 algorithm,
issuer, audience, expiry with zero clock skew, and account existence. Advancing
the demo date does not affect token expiry. There are no refresh, logout,
verified-email, or password-reset endpoints.

## Transactions

### POST `/api/transactions`

First advance your logical date to the transaction's date.

```json
{
  "type": "expense",
  "counterpartyKey": "example-telecom",
  "counterpartyName": "Example Telecom",
  "transactionKey": "home-internet",
  "description": "Fictional home internet",
  "amount": 45,
  "currency": "EUR",
  "date": "2026-01-05"
}
```

For income, use `type: "income"` and a payer counterparty such as an employer.
Income also requires a stable `transactionKey`, for example `monthly-salary`.
Names/descriptions are display text, not matching identifiers.

Success: **201**, `{transaction, notifications}`. The transaction contains the
input fields plus `id`; the notifications array contains only alerts generated
by this submission, including delivery outcomes.

- **400:** invalid required stream fields, unknown type, nonpositive/non-EUR/
  excess-decimal amount, or date unequal to your current logical date.
- **409:** the same user/type/counterparty/key/currency/date already exists.

There is no transaction update/delete endpoint or transaction-by-ID route.

### GET `/api/transactions`

Returns **200**, an array of your transactions ordered by date then ID.
Optional exact-date filter:

```text
/api/transactions?date=2026-02-05
```

No other user's transactions are returned.

## Recurring transactions

### GET `/api/recurring-transactions`

Returns **200**, your active recurring income and expenses in ID order:

```json
[
  {
    "id": 1,
    "type": "expense",
    "counterpartyKey": "example-telecom",
    "counterpartyName": "Example Telecom",
    "transactionKey": "home-internet",
    "currency": "EUR",
    "dayOfMonth": 5,
    "lastPaymentDate": "2026-02-05",
    "lastAmount": 49,
    "nextExpectedDate": "2026-03-05",
    "isActive": true
  }
]
```

The last amount/date and expected date are derived from the linked last
transaction and original recurrence day. Missing-month evaluation changes state
through clock advancement; there is no PATCH endpoint for critical-payment flags.

## Logical clock

### GET `/api/demo/date`

Returns **200**, `{"date":"2026-01-01"}` initially, for your account.

### PUT `/api/demo/date`

```json
{"date":"2026-02-05"}
```

Returns **200**, `{date, notifications}`, where notifications are newly generated
missing-payment alerts. Forward and equal dates are allowed. Backward movement
returns **400** without changing the clock.

Post all payments for the current day before advancing. Missing-payment checks
apply only after an expected month has fully ended. There is no reset endpoint.

## Notifications

### GET `/api/notifications`

Returns **200**, your notifications ordered by UTC creation time then ID.

| Field | Meaning |
|---|---|
| `id` | Notification ID |
| `kind` | `price-increased`, `recurring-added`, or `recurring-missing` |
| `channel` | Implemented delivery channel: `email` |
| `transactionId` | Nullable source transaction |
| `recurringTransactionId` | Nullable recurring-stream link |
| `missingMonth` | Nullable `YYYY-MM` for a missing payment |
| `subject`, `body` | Historical rendered message |
| `recipientAddress` | Configured presenter mailbox captured at creation |
| `occurredOn` | Account's logical date |
| `createdAt` | Actual UTC creation timestamp |
| `deliveryStatus` | `pending`, `sent`, or `failed` |

`sent` means transport acceptance, not independently established receipt.
Inspect Mailpit/the mailbox to confirm receipt. Delivery failure leaves business
data saved. Notifications are not automatically retried.

## Lookups and preferences

| Endpoint | Response |
|---|---|
| GET `/api/transaction-types` | `{id,code,name}[]` |
| GET `/api/conditions` | `{id,code,name,transactionTypeIds}[]` |
| GET `/api/notification-channels` | `{id,code,name,isSupported}[]` |
| GET `/api/me/conditions` | `{conditionId,isEnabled,channelId}[]` |

### Seeded applicability

| Condition ID / code | Type IDs |
|---|---|
| 1 / `price-increased` | 1 expense |
| 2 / `recurring-added` | 1 expense, 2 income |
| 3 / `recurring-missing` | 1 expense, 2 income |

The type-ID array is a response projection of `ConditionTransactionType` rows,
not a string-list field stored in the database.

Channels: **1 email supported**, **2 sms unavailable**, **3 in-app unavailable**.

### PUT `/api/me/conditions/{conditionId}`

```json
{"isEnabled":false,"channelId":1}
```

Returns **200** with the updated setting. Unknown condition returns **404**;
unknown or unsupported channel returns **400**. Only your setting is changed.
Disabling an alert never disables history storage or recurrence tracking.

## Errors and client integration

Business errors use `{"error":"<explanation>"}`. Invalid/missing bearer tokens
produce **401**, not a login redirect. Malformed binding uses framework errors;
clients should use HTTP status rather than rely on identical error bodies.

The real Blazor `ApiClient` sends the JWT on requests and maps the final API
records into its UI models. Its `RecurringExpense` UI class name does not imply
an expense-only backend. Mock-mode critical flags have no equivalent real endpoint.

The current API intentionally replaced `companyId`, `companyName`, `expenseKey`
and `/api/recurring-expenses`; do not send the earlier contract.
