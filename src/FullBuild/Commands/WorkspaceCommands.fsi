
module WorkspaceCommands


val Create: Commands.SetupWorkspace
         -> unit

val Push: Commands.PushWorkspace
       -> unit

val Checkout: Commands.CheckoutVersion
           -> unit

val Branch: Commands.BranchWorkspace
         -> unit

val Install: unit
          -> unit

val Pull: Commands.PullWorkspace
       -> unit

val Init: Commands.InitWorkspace
       -> unit

val Exec: Commands.Exec
       -> unit

val Clean: unit
        -> unit

val UpdateGuid: repositoryId : string
             -> unit

val History: Commands.History
          -> unit

val Index: Commands.IndexRepositories
        -> unit

val Convert: Commands.ConvertRepositories
          -> unit
