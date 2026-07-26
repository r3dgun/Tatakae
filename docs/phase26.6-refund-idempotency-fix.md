# Phase 26.6 — Zarinpal refund idempotency fix

`Refund.Reason` is the client-supplied request description and participates in
matching repeated refund requests. It must remain immutable after creation.

Previously, provider outcome messages were appended to `Reason`. A repeated
request with the original description therefore failed to find the existing
pending refund and created a second record with a new identifier.

The repository now:

- normalizes the request reason once before lookup and insert;
- keeps the stored reason unchanged when persisting provider outcomes;
- recognizes legacy Phase 26.5 values stored as `<reason> | <provider message>`.

The provider is therefore called once and repeated requests return the original
refund record and identifier.
