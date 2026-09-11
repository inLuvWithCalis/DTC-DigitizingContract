# Structured contract payment milestones

Migration `20260910090117_StructuredContractPaymentMilestones` adds typed
payment terms, template milestones, immutable contract-version milestones and
the optional ledger link `PaymentMilestoneId`.

## Deployment order

1. Run the legacy inventory query in every tenant database and archive the
   result. Do not infer a version or payment term for legacy rows.
2. Apply the idempotent migration script to the tenant database.
3. Deploy the backend before or together with the frontend. The backend reads
   rich text v1 and v2; the frontend writes v2.
4. Copy each published template to a Draft version, explicitly mark one term as
   `Payment`, enter milestones totaling exactly 100%, regenerate preview and
   publish the new version.
5. Existing contracts without structured milestones continue to render
   `tbl_PaymentSchedule`. New contract versions use structured milestones.

## Due-date behavior

Supported anchors are contract signing, contract effective date, acceptance,
full payment of the previous milestone and a manual event. A manual event must
be entered by a payment manager with an anchor date and audit reason.

`BusinessDays` currently skips Saturday and Sunday only. Vietnamese public
holidays are not excluded until a shared business calendar is introduced.

## Rollback and data policy

The first migration keeps `tbl_PaymentSchedule`. Do not delete it until every
tenant inventory is empty or each row has been reviewed and retained as an
historical artifact. Template changes never rewrite milestones already copied
to a contract version.
