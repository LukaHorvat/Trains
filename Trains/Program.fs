open System.Text
open System.IO
open System.Runtime.Serialization.Formatters.Binary
open System.Runtime.Serialization.Json
open System

[<EntryPoint>]
let main argv = 
    let res = Cache.loadRoutesFromFile "allRoutes.json"
    let graph = Graph.graphFromRoutes res
    let koprivnica = graph.["KOPRIVNICA"]
    let path = Graph.dijkstra koprivnica "GUNJA" (DateTime.Today.AddDays(-2.0))
    0 // return an integer exit code
