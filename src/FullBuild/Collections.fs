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

module Collections

open FSharp.Collections

type set<'T when 'T : comparison> = Set<'T>

let (?) (q: bool) (yes: 'a, no: 'a) = if q then yes else no


let compareTo<'T, 'U> (this : 'T) (other : System.Object) (fieldOf : 'T -> 'U) =
    match other with
    | :? 'T as x -> System.Collections.Generic.Comparer<'U>.Default.Compare(fieldOf this, fieldOf x)
    | _ -> failwith "Can't compare values with different types"

let refEquals (this : System.Object) (other : System.Object) =
    System.Object.ReferenceEquals(this, other)

let memoize (f: 'a -> 'b) : 'a -> 'b =
    let cache = System.Collections.Concurrent.ConcurrentDictionary<'a, 'b>()
    fun (x: 'a) ->
        cache.GetOrAdd(x, f)
