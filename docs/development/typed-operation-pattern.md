# Handwritten typed MP operations

Use the current Variables and Event Operations as the pattern for migrating an
existing catalog operation. This is guidance for handwritten source in each
independent target, not a shared runtime library or an operation generator.

An operation has one small class in its domain folder. Its `Descriptor` records
the reviewed MP step, public RPC identity, effect, execution scope, replay
safety, and risk flags. `CreateCommand` reads the generated request directly,
applies the exact target's defaults and validation, and constructs typed worker
arguments in MP order. `OutputContracts` names the expected output shape;
`CreateResult` reads the already validated output values by position and
constructs the generated result. A thin service override calls the existing
`OperationExecutor` with those four members. Register the descriptor once in
`SpatialAnalyzerApi.Operations` and remove the matching interpreter entry.

For examples, `SetDoubleVariableOperation` shows a scalar mutation,
`GetNamedDoubleListVariableOperation` shows a list result, and
`SetRelationshipFitConstraintsScalarTypeOperation` shows a nested option whose
omitted leaves have separate reviewed defaults. The shared
`FitConstraintScalarOptionsMapper` exists because the same value family is
used in both directions. Add a shared value mapper only when several
operations genuinely use the same semantic type and rules. Keep a one-off
conversion beside its operation.

The output contract and output argument deliberately repeat the expected
names/kinds at two trust boundaries: one describes what the host requested and
one validates what the worker returned. `OperationExecutor` owns policy,
admission, outcome classification, and auditing. The operation class should
not reimplement those concerns. SDK binding names carried by the private
command are checked against the worker's expected typed binding; the worker
selects an SDK call by value kind, not by invoking a host-supplied method name.

Before migrating a family, compare each operation with committed evidence for
its target. Preserve missing-versus-explicit defaults, list requirements,
object-versus-item choice domains, output retrieval failures, and all risk
flags. Use a generated-client RPC test for the actual route and focused
negative cases for that family's new semantics. Reuse existing executor,
transport, cancellation, and lifecycle tests for shared behavior.

Review of scalar, list, and nested implementations found no additional helper
that would remove meaningful repetition without hiding MP bindings or adding
another framework. Keep this pattern stable while completing the remaining
domains.
