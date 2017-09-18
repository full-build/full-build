//   Copyright 2014-2017 Pierre Chalamet
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

module Threading

open System

// http://stackoverflow.com/questions/3739531/how-to-limit-the-number-of-threads-created-for-an-asynchronous-seq-map-operation
let private throttle n fs =
    let n = new Threading.Semaphore(n, n)
    let throttleTask f = 
        async {
            let! ok = Async.AwaitWaitHandle(n)
            let! result = Async.Catch f
            n.Release() |> ignore
            return match result with
                   | Choice1Of2 rslt -> rslt
                   | Choice2Of2 exn  -> raise exn 
        } 

    fs |> Seq.map throttleTask 


let ParExec fn from =
    let maxThrottle = 4
    let results = from |> Seq.map fn
                       |> throttle maxThrottle
                       |> Async.Parallel
                       |> Async.RunSynchronously
    results
