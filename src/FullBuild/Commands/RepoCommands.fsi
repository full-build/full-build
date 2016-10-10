
module RepoCommands


val List: unit
       -> unit

val Clone: cmd : Commands.CloneRepositories
        -> unit

val Add: cmd : Commands.AddRepository
      -> unit

val Drop: name : string
       -> unit

