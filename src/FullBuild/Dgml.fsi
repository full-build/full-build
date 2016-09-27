module Dgml

open System.Xml.Linq
open Graph
open Collections

val GraphContent: projects : Project set
               -> all : bool
               -> XDocument

