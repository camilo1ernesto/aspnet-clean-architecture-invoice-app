POST /invoices/{invoiceId}/payments

Business Rules

- Invoice must exist.
- Payment amount must be > 0.
- Payment cannot exceed outstanding balance.
- Payment reference must be unique.
- Partial payments are permitted.
- Invoice becomes paid once outstanding balance is zero.

Architecture

- Domain owns business rules.
- Application coordinates the use case.
- Infrastructure owns persistence.
- API handles HTTP concerns.

Testing

- Domain unit tests.
- Application/use-case tests.
- Integration test.
- Existing tests remain green.