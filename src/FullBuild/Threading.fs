module Threading

open System

let throttle n fs =
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

