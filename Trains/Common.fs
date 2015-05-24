[<AutoOpen>]
module Common

module Seq = 
    let stateMap f init xs = seq {
        let mutable state = init
        for x in xs do
            let x', state' = f x state
            state <- state'
            yield x'
    }
    let rec drop n xs = seq {
        let mutable dropping = n
        for x in xs do
            if dropping > 0 then dropping <- dropping - 1
            else yield x
    }
    let groupIn n xs = seq {
        let mutable rest = xs
        while not (Seq.isEmpty rest) do
            let group = Seq.take n rest
            rest <- drop n rest
            yield group
    }

module Async =
    let map f a = async { 
        let! a' = a
        return f a' }