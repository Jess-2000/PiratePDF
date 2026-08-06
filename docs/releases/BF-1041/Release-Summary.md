# BF-1041 Release Summary

| Field | Value |
|---|---|
| **Document ID** | PP-REL-BF-1041 |
| **Category** | Release Documentation |
| **Version** | 1.0 |
| **Status** | Tested |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1041 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Release Candidate

```text
BF-1041-RC1
```

## Delivered

BF-1041 adds production ASCIIHex, ASCII85, RunLength, and LZW decoding to the
BlackFlag filter framework.

The release also adds reusable filter corpora, chained-filter integration
tests, large-input regression coverage, strict malformed-data handling, and
default registry integration.

## Verification

The release gate requires:

- expected feature branch;
- required cumulative files;
- no merge-conflict markers;
- Release clean and restore;
- complete Release build;
- all Release tests;
- DocGen generation;
- zero documentation errors;
- Git format validation; and
- an exact working-tree allowlist.
