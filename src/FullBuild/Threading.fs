module Threading

open System

// http://stackoverflow.com/questions/3739531/how-to-limit-the-number-of-threads-created-for-an-asynchronous-seq-map-operation
let private throttle n fs =
    seq { let n = new Threading.Semaphore(n, n)
          for f in fs ->
              async { let! ok = Async.AwaitWaitHandle(n)
                      let! result = Async.Catch f
                      n.Release() |> ignore
                      return match result with
                             | Choice1Of2 rslt -> rslt
                             | Choice2Of2 exn  -> raise exn
                    }
        }


let ParExec fn from = 
    let maxThrottle = System.Environment.ProcessorCount*4
    let results = from |> Seq.map fn
                       |> throttle maxThrottle 
                       |> Async.Parallel 
                       |> Async.RunSynchronously
    results
