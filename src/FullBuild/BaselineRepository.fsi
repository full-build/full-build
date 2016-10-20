module BaselineRepository

open Collections
open Graph

type [<Sealed>] Bookmark = interface System.IComparable
with
    member Repository : Repository
    member Version : string

and [<Sealed>] Baseline = interface System.IComparable
with
    member IsIncremental: bool
    member Bookmarks: Bookmark set
    static member (-): Baseline*Baseline
                    -> Repository set
    member Save: unit
              -> unit

and [<Sealed>] BaselineRepository =
    member Baseline : Baseline
    member CreateBaseline: incremental : bool
                        -> Baseline

val from: graph : Graph
       -> BaselineRepository
