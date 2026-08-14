# PiratePDF BF-1051H CA1822 Fix

Corrects the seven CA1822 analyzer errors introduced by the BF-1051H
preservation contract.

The preservation API remains instance-based. Contract values are now immutable
instance state, and each editing session retains its preservation-contract
reference. This keeps the API suitable for future per-session preservation
policies without suppressing analyzers or converting the contract to a static
utility surface.

The overlay may be applied to the dirty in-progress BF-1051H working tree.
It only copies files and does not run the release gate.
