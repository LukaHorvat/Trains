# Trains
Pathfinding on train routes in Croatia.

Why
---
At the time of writing, using HŽ's online system, it's impossible to find a route from Koprivnica to Gunja (for example) because
the service points you in the wrong direction.
This project runs an actual path finding algorithm and finds correct routes.

How
---
Crawls the web pages of HŽ and gets information about trains (parsing the strings that specify which days a route is active isn't implemented yet).
Then it converts that info into a graph and runs Dijkstra's pathfinding on it.

Example
-------
```
let res = Cache.loadRoutesFromFile "allRoutes.json"
let graph = Graph.graphFromRoutes res
let koprivnica = graph.["KOPRIVNICA"]
let path = Graph.dijkstra koprivnica "GUNJA" (DateTime.Today.AddDays(-2.0)) //The AddDays here is because the cached data is 2 days old
```

Result
![Result](http://i.imgur.com/5kAOX6x.png)
