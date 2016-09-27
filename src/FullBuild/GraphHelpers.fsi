module GraphHelpers

open Graph
open Collections

val ComputeTransitiveReferences : Project set -> Project set
val ComputeTransitiveReferencedBy : Project set -> Project set
val ComputeClosure : Project set -> Project set
