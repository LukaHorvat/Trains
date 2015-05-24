module Crawler

open System.Net
open FSharp.Data
open System
open System.Text
open System.IO

let getDataFromId (i : int) = Http.AsyncRequestString("http://www.hzpp.hr/CorvusTheme/TimetableByStations/Result", ["location", i.ToString(); "TravelDirection", "O"])

let getDataFromName name = Map.find name Stations.stationToId |> getDataFromId

let trains name = async {
    printfn "%s" name
    let! src = getDataFromName name
    let doc = HtmlDocument.Parse(src)
    return doc.Descendants (fun node -> node.AttributeValue("class") = "expander")
    |> Seq.map HtmlNode.innerText
    |> Seq.map Int32.TryParse
    |> Seq.choose (fun (s, n) -> if s then Some n else None)
    |> Set.ofSeq
}

let allTrains = async {
    let! arrSets = Stations.arr |> Seq.map snd |> Seq.map trains |> Async.Parallel 
    return Seq.fold Set.union Set.empty arrSets
}

let store path seq =
    let builder = StringBuilder()
    seq |> Seq.iter (fun e -> builder.AppendLine(e.ToString()) |> ignore)
    File.WriteAllText(path, builder.ToString());

let loadTrainInfoHtml (i : int) = 
    Http.AsyncRequestString("http://www.hzpp.hr/CorvusTheme/TimetableByStations/ResultInner", ["Arrival", "false"; "Departure", "true"; "Train", i.ToString()])

type RoutePart = { station: string 
                   arrival: DateTime 
                   departure: DateTime }

type TrainRoute = { id: int
                    parts: RoutePart [] }

let parseTrainData i src = async {
    printfn "%d" i
    let today = DateTime.Today
    let doc = HtmlDocument.Parse src
    let parseOrMin str = if str <> "" then today + TimeSpan.Parse str else DateTime.MinValue.ToUniversalTime()
    let stations, times = 
        doc 
        |> HtmlDocument.descendantsNamed true ["tr"]
        |> Seq.drop 1
        |> Seq.map( fun tr -> 
            tr 
            |> HtmlNode.descendantsNamed false ["td"] 
            |> Seq.take 3
            |> Seq.map HtmlNode.innerText
            |> Seq.toList
            |> fun [name; arrive; depart] -> name, [parseOrMin arrive; parseOrMin depart]
        )
        |> Seq.toList
        |> List.unzip
    let correctedTimes = 
        times
        |> Seq.concat
        |> Seq.stateMap( fun time (add, last) -> 
                if time = DateTime.MinValue then time, (add, last)
                else if add then time.AddDays(1.), (add, time)
                else if time < last then time.AddDays(1.), (true, time)
                else time, (false, time)
            ) (false, today)
        |> Seq.groupIn 2
        |> Seq.map Seq.toList
    let parts =
        Seq.zip stations correctedTimes
        |> Seq.map (fun (st, [arrive; depart]) -> { station = st; arrival = arrive; departure = depart })
    return { id = i; parts = parts |> Seq.toArray }
}

let loadTrainData (i : int) = async {
    try
        let! src = loadTrainInfoHtml i
        if src = "" then return None
        else
            let! res = parseTrainData i src
            return Some res
    with _ -> return None
}

let loadAllTrainData trains = Seq.map loadTrainData trains |> Async.Parallel |> Async.map (Array.choose id)