module Solution

open System.Xml.Linq
open Collections
open Graph

val GenerateSolutionDefines: projects : Project set 
                          -> XDocument

val GenerateSolutionContent: projects : Project set 
                          -> string seq

