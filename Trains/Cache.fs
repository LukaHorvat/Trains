module Cache
open System.IO
open System
open System.Runtime.Serialization.Json

let loadTrainsFromFile path =
    let str = File.ReadAllText path
    str.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map Int32.Parse
    |> Set.ofSeq

let loadRoutesFromFile path =
    let serializer = DataContractJsonSerializer(typeof<Crawler.TrainRoute []>)
    use file = File.OpenRead path
    serializer.ReadObject file :?> Crawler.TrainRoute []