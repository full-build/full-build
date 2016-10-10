module ViewCommands


val Add: cmd : Commands.AddView
      -> unit

val Drop: name : string
       -> unit

val List: unit
       -> unit

val Describe: name : string
           -> unit

val Graph: cmd : Commands.GraphView
        -> unit

val Build: cmd : Commands.BuildView
        -> unit

val Alter: cmd : Commands.AlterView
        -> unit

val Open: cmd : Commands.OpenView
      -> unit
