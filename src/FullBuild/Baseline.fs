module Baseline

open Anthology
open Collections

type BookmarkVersion = BookmarkVersion of string
with
    member this.toString = (fun (BookmarkVersion x) -> x)this
    static member from (version : string) = BookmarkVersion (version)

type Bookmark =
    { Repository : RepositoryId
      Version : BookmarkVersion }

type Baseline =
    { Incremental : bool
      Bookmarks : Bookmark set }
