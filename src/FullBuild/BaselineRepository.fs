module BaselineRepository

open Graph
open Collections




#nowarn "0346" // GetHashCode missing


let compareTo<'T, 'U> (this : 'T) (other : System.Object) (fieldOf : 'T -> 'U) =
    match other with
    | :? 'T as x -> System.Collections.Generic.Comparer<'U>.Default.Compare(fieldOf this, fieldOf x)
    | _ -> failwith "Can't compare values with different types"

let refEquals (this : System.Object) (other : System.Object) =
    System.Object.ReferenceEquals(this, other)



// =====================================================================================================

type [<CustomEquality; CustomComparison>] Bookmark =
    { Graph : Graph
      Bookmark : Anthology.Bookmark }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Bookmark.Repository)

    member this.Repository =
        this.Graph.Repositories |> Seq.find (fun x -> x.Name = this.Bookmark.Repository.toString)

    member this.Version = this.Bookmark.Version.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Baseline =
    { Graph : Graph 
      Baseline : Anthology.Baseline }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Baseline)

    member this.IsIncremental = this.Baseline.Incremental
    
    member this.Bookmarks = 
        this.Baseline.Bookmarks |> Set.map (fun x -> { Graph = this.Graph; Bookmark = x })
    
    static member (-) (a:Baseline, b : Baseline) =
        let changes = Set.difference a.Bookmarks b.Bookmarks
        changes |> Set.map (fun x -> x.Repository)

    member this.Save () = 
        Configuration.SaveBaseline this.Baseline

// =====================================================================================================

and [<Sealed>] BaselineRepository(graph : Graph) =
    member this.Baseline = 
        let baseline = Configuration.LoadBaseline()
        { Graph = graph; Baseline = baseline }

    member this.CreateBaseline (incremental : bool) =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let bookmarks = graph.Repositories |> Set.map (fun x -> { Anthology.Bookmark.Repository = Anthology.RepositoryId.from x.Name
                                                                  Anthology.Bookmark.Version = Anthology.BookmarkVersion (Tools.Vcs.Tip wsDir x) })
        let baseline = { Anthology.Baseline.Incremental = incremental 
                         Anthology.Baseline.Bookmarks = bookmarks }
        { Graph = graph
          Baseline = baseline }
          
          
let from graph =
    BaselineRepository(graph)
