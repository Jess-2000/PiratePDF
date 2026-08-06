# BlackFlag Memory and Resource Architecture

| Field | Value |
|---|---|
| **Document ID** | PP-ARCH-MEM-001 |
| **Category** | Architecture |
| **Version** | 1.0 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1040, BF-1042 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Purpose

This document defines the memory and resource principles that govern
BlackFlag Engine as it grows toward large-document loading, decoding,
rendering, editing, and writing.

## Principles

- Let the CLR manage ordinary managed-object lifetime.
- Do not force garbage collection as routine application behavior.
- Give large data explicit ownership and lifecycle rules.
- Avoid duplicating source data without a concrete need.
- Prefer lazy and bounded materialization.
- Pool temporary buffers only through controlled ownership boundaries.
- Bound caches by memory cost and apply explicit eviction.
- Make cancellation and resource budgets part of public subsystem contracts.
- Dispose unmanaged resources deterministically.

## Stream Data

BF-1039 stores an owned immutable stream snapshot for parser correctness. That
is an accepted foundation-stage representation, not a requirement that a
future document loader eagerly copy every stream.

Future document loading may introduce source-backed encoded stream ranges,
on-demand materialization, and decoded output that can spill to controlled
temporary storage when memory budgets require it.

## Filter Decoding

BF-1040 establishes these requirements:

- decoded bytes are limited independently of encoded size;
- expansion ratio is limited;
- filter-chain length is limited;
- temporary buffer size is controlled;
- cancellation is observable;
- decoders do not own caches or source I/O;
- decoded results have explicit ownership.

## Caches

Caches must declare:

- maximum memory cost;
- accounting unit;
- eviction policy;
- ownership of cached values;
- disposal behavior; and
- behavior under memory pressure.

Unbounded dictionaries are not acceptable caches.

## Undo and Editing

Undo should use compact commands, inverse operations, and shared immutable
state rather than complete document copies. Undo history must eventually be
bounded by memory cost and may use disk-backed storage for large operations.

## Profiling

Memory decisions are validated through allocation measurements, large-file
regression tests, and benchmark evidence. Pooling or specialized allocation
must not be introduced solely by assumption.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-06 | Established the initial memory and resource architecture for BF-1040. |
