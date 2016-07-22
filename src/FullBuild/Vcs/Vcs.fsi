//   Copyright 2014-2016 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module Vcs

open System.IO
open Anthology


val Clone : vcsType : VcsType 
         -> wsDir : DirectoryInfo 
         -> repo : Repository 
         -> shallow : bool 
         -> unit

val Tip : vcsType : VcsType 
       -> wsDir : DirectoryInfo 
       -> repo : Repository 
       -> string

val Checkout : vcsType : VcsType 
            -> wsDir : DirectoryInfo 
            -> repo : Repository 
            -> version : BookmarkVersion option 
            -> ignore : bool 
            -> unit

val Ignore : vcsType : VcsType 
          -> wsDir : DirectoryInfo 
          -> repo : Repository 
          -> unit

val Pull : vcsType : VcsType 
        -> wsDir : DirectoryInfo 
        -> repo : Repository 
        -> rebase : bool
        -> unit

val Commit : vcsType : VcsType 
          -> wsDir : DirectoryInfo 
          -> repo : Repository 
          -> comment : string 
          -> unit

val Push : vcsType : VcsType 
        -> wsDir : DirectoryInfo 
        -> repo : Repository 
        -> unit

val Clean : vcsType : VcsType 
         -> wsDir : DirectoryInfo 
         -> repo : Repository 
         -> unit

val Log : vcsType : VcsType 
       -> wsDir : DirectoryInfo 
       -> repo : Repository 
       -> version : BookmarkVersion 
       -> string

val LastCommit : vcsType : VcsType 
              -> wsDir : DirectoryInfo 
              -> repo : Repository 
              -> relativeFile : string 
              -> BookmarkVersion option
