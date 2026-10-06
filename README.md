# FiscApi

Back-end API for **FiscApp**, a personal finance tracker (.NET MAUI).
Part 1 stores data in a JSON file; Part 2 adds SQL Server behind the same interfaces.

## Resource: Transaction

| Field         | Type     | Set by | Notes                                     |
|---------------|----------|--------|-------------------------------------------|
| `id`          | int      | Server | Assigned by the repository                |
| `createdAt`   | DateTime | Server | UTC timestamp when the record was created |
| `date`        | DateTime | Client | When the transaction happened             |
| `amount`      | decimal  | Client | 0.01 – 1,000,000                          |
| `type`        | string   | Client | `Income` or `Expense`                     |
| `category`    | string?  | Client | Optional, max 50 characters               |
| `description` | string   | Client | Required, 1–100 characters                |
| `notes`       | string?  | Client | Optional, max 250 characters              |

## Endpoints

| Method | URL                       | What it does                                                        | Success          | Failure codes |
|--------|---------------------------|---------------------------------------------------------------------|------------------|---------------|
| GET    | `/api/transactions`       | List all transactions. Optional filters: `?type=` and `?category=`  | 200 OK           | —             |
| GET    | `/api/transactions/{id}`  | Get one transaction by id                                           | 200 OK           | 404           |
| POST   | `/api/transactions`       | Create a transaction; returns it with a `Location` header           | 201 Created      | 400, 409      |
| PUT    | `/api/transactions/{id}`  | Replace an existing transaction                                     | 204 No Content   | 400, 404, 409 |
| DELETE | `/api/transactions/{id}`  | Delete a transaction                                                | 204 No Content   | 404           |

## Business rule

No duplicate transactions: a transaction with the same **date** (calendar day), **amount**, and
**description** (case-insensitive, trimmed) as an existing one is rejected with **409 Conflict**.
On PUT, the transaction being updated is excluded from the check.

## Validation (400 Bad Request)

- `description` is required, 1–100 characters
- `amount` must be between 0.01 and 1,000,000
- `type` is required and must be `Income` or `Expense`
- `date` is required
- `category` max 50 characters; `notes` max 250 characters
