module ViewRepository

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

type [<CustomEquality; CustomComparison>] View =
    { Graph : Graph
      View : Anthology.View }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.View)

    member this.Name = this.View.Name
    member this.Filters = this.View.Filters
    member this.Parameters = this.View.Parameters
    member this.References = this.View.SourceOnly
    member this.ReferencedBy = this.View.Parents
    member this.Modified = this.View.Modified
    member this.Builder = match this.View.Builder with
                          | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                          | Anthology.BuilderType.Skip -> BuilderType.Skip

    member this.Projects : Project set =
        let filters = this.View.Filters |> Set.map (fun x -> if x.IndexOfAny([|'/'; '\\' |]) = -1 then x + "/*" else x)
                                        |> Set.map (fun x -> x.Replace('\\', '/'))
        let projects = PatternMatching.FilterMatch<Project> 
                            this.Graph.Projects 
                            (fun x -> sprintf "%s/%s" x.Repository.Name x.Output.Name) 
                            filters

        let baselineRepo = BaselineRepository.from this.Graph
        let newBaseline = baselineRepo.CreateBaseline false
        let modRepositories = if this.Modified then baselineRepo.Baseline - newBaseline
                              else Set.empty
        let modProjects = modRepositories |> Set.map (fun x -> x.Projects)
                                          |> Set.unionMany
        let viewProjects = Project.Closure (projects + modProjects)
        let depProjects = if this.References then Project.TransitiveReferences viewProjects  
                          else Set.empty
        let refProjects = if this.ReferencedBy then Project.TransitiveReferencedBy viewProjects
                          else Set.empty
        let projects = viewProjects + depProjects + refProjects + modProjects
        projects |> Set.filter (fun x -> x.Repository.IsCloned)

    member this.Save (isDefault : bool option) = 
        let viewId = Anthology.ViewId this.View.Name
        Configuration.SaveView viewId this.View isDefault

    member this.Delete () =
        Configuration.DeleteView (Anthology.ViewId this.View.Name)


and [<Sealed>] ViewRepository(graph : Graph) =
    let mutable viewMap : System.Collections.Generic.IDictionary<Anthology.ViewId, View> = null

    member this.ViewMap : System.Collections.Generic.IDictionary<Anthology.ViewId, View> = 
        if viewMap |> isNull then
            let vwDir = Env.GetFolder Env.Folder.View
            viewMap <- vwDir.EnumerateFiles("*.view") |> Seq.map (fun x -> System.IO.Path.GetFileNameWithoutExtension(x.Name) |> Anthology.ViewId)
                                                      |> Seq.map Configuration.LoadView
                                                      |> Seq.map (fun x -> x.Name |> Anthology.ViewId, { Graph = graph; View = x })
                                                      |> dict
        viewMap
            
    member this.Views = this.ViewMap.Values |> set

    member this.DefaultView =
        let viewId = Configuration.DefaultView ()
        match viewId with
        | None -> None
        | Some x -> Some this.ViewMap.[x]

    member this.CreateView name filters parameters dependencies referencedBy modified builder =
        let view = { Anthology.View.Name = name 
                     Anthology.View.Filters = filters
                     Anthology.View.Parameters = parameters
                     Anthology.View.SourceOnly = dependencies
                     Anthology.View.Parents = referencedBy
                     Anthology.View.Modified = modified
                     Anthology.View.Builder = match builder with
                                              | BuilderType.MSBuild -> Anthology.BuilderType.MSBuild
                                              | BuilderType.Skip -> Anthology.BuilderType.Skip }

        { Graph = graph
          View = view }

let from graph =
    ViewRepository(graph)
