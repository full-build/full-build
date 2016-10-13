
module Commands.Application

val Publish: pubInfo : CLI.Commands.PublishApplications
          -> unit

val List: unit
       -> unit

val Add: addInfo : CLI.Commands.AddApplication
      -> unit

val Drop: name : string
       -> unit

val BindProject: bindInfo : CLI.Commands.BindProject
              -> unit

