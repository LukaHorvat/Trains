module Graph

open System.Collections.Generic
open System
open FSharpx.Collections

type Connection = 
        { startTime: DateTime
          endTime: DateTime
          trainId : int
          station: Station }
        override x.ToString () = "To " + x.station.ToString() + " at " + x.startTime.ToString() + " via " + x.trainId.ToString()

and [<CustomEquality; CustomComparison>]
    Station = 
        { name: string
          mutable connections: Connection Set }
        override x.Equals y =
            match y with
            | :? Station as st -> x.name = st.name
            | _ -> sprintf "Cannot campare Station to %A" (y.GetType()) |> invalidArg "y"
        override x.ToString () = x.name
        interface IComparable with
            member x.CompareTo y = 
                match y with
                | :? Station as st -> x.name.CompareTo st.name
                | _ -> sprintf "Cannot campare Station to %A" (y.GetType()) |> invalidArg "y"
        

let graphFromRoutes (routes : Crawler.TrainRoute []) =
    let dict = Dictionary<string, Station>()
    routes 
    |> Seq.collect (fun tr -> tr.parts |> Seq.map (fun rt -> rt.station))
    |> Set.ofSeq
    |> Seq.map (fun s -> { name = s; connections = Set.empty })
    |> Seq.iter (fun s -> dict.Add(s.name, s))
    routes |> Array.iter( fun tr -> 
        tr.parts |> Seq.pairwise |> Seq.iter( fun (rp1, rp2) -> 
            let station = dict.[rp1.station] 
            station.connections <- Set.add { startTime = rp1.departure; endTime = rp2.arrival; station = dict.[rp2.station]; trainId = tr.id } station.connections
        ) 
    )
    dict

type Line = { startStation: Station
              endStation:   Station
              startTime:    DateTime
              endTime:      DateTime
              trainId:      int }
type Path = { lines:     Line list
              startTime: DateTime
              endTime:   DateTime }

let toPath (pth : (DateTime * DateTime * int * Station) list) =
    let toLine (l : (DateTime * DateTime * int * Station) seq) =
        let (_, startTime, i, startStation) = Seq.head l
        let (endTime, _, _, endStation) = Seq.last l
        { startStation = startStation; endStation = endStation; startTime = startTime; endTime = endTime; trainId = i }
    let fixLine (l1, l2) =
        { l2 with startStation = l1.endStation }
    let lines = 
        pth
        |> Seq.groupBy (fun (_, _, i, _) -> i)
        |> Seq.map (fun (_, l) -> toLine l)
        |> Seq.pairwise
        |> Seq.map fixLine
        |> Seq.toList
    { lines = lines; startTime = lines |> List.head |> (fun l -> l.startTime); endTime = lines |> Seq.last |> fun l -> l.endTime }

let dijkstra (startStation : Station) endName (startTime : DateTime) =
    let ins endTime startTime trainId stat l q = PriorityQueue.insert (endTime, startTime, trainId, stat, (endTime, startTime, trainId, stat) :: l) q
    let mutable visited = Set.singleton startStation
    let mutable bestTime = DateTime.MaxValue
    let rec iter q  =
        if PriorityQueue.isEmpty q then None
        else
            let (curTime, _, _, curStat, curPath), q' = PriorityQueue.pop q
            if curTime > bestTime then None
            else if curStat.name = endName then 
                visited <- Set.singleton startStation
                bestTime <- curTime
                Some curPath
            else if Set.contains curStat visited then iter q'
            else
                visited <- Set.add curStat visited
                curStat.connections
                |> Seq.filter (fun stat -> stat.startTime >= curTime)
                |> Seq.fold (fun q'' con -> ins con.endTime con.startTime con.trainId con.station curPath q'') q'
                |> iter
    let initial time = [time, startTime, 0, startStation]
    startStation.connections
    |> Seq.filter (fun stat -> stat.startTime >= startTime)
    |> Seq.choose (fun stat -> ins stat.endTime stat.startTime stat.trainId stat.station (initial stat.startTime) (PriorityQueue.empty false) |> iter)
    |> Seq.map List.rev
    |> Seq.map toPath
    |> Seq.groupBy (fun p -> p.endTime)
    |> Seq.minBy fst
    |> fun (_, pts) -> Seq.maxBy (fun p -> p.startTime) pts