module ViewRepository

open Graph
open Collections

type [<Sealed>] View = interface System.IComparable
with
    member Name: string
    member Filters: string set
    member Parameters: string set
    member References: bool
    member ReferencedBy: bool
    member Modified : bool
    member Builder: BuilderType
    member Projects: Project set
    member Save: isDefault : bool option
              -> unit
    member Delete: unit
                -> unit

and [<Sealed>] ViewRepository =
    member DefaultView : View option
    member Views : View set
    member CreateView: name : string
                    -> filters : string set
                    -> parameters: string set
                    -> dependencies : bool
                    -> referencedBy : bool
                    -> modified : bool
                    -> builder : BuilderType
                    -> View

val from: graph : Graph
       -> ViewRepository
