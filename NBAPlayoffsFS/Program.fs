// Learn more about F# at http://fsharp.net

open System
open System.IO
open System.Reflection

type Conference =
    | Eastern
    | Western

type Team = {
    Name : string;
    Wins : int;
    Losses : int;
    Conference : Conference
}

let filename = (Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName)  + "\Teams.txt"
let totalgames = 82
let max = totalgames + 1
let r = new Random()
let conferencesize = 15
let playoffteamsperconference = 8
let half = playoffteamsperconference / 2

let getteams() =
    seq {
        let mapteam conference (l : string) =
            let wins = r.Next(0, max)
            { Name = l.Trim(); Wins = wins; Losses = totalgames - wins; Conference = conference;}

        let teams = filename |> File.ReadAllLines

        // first 15 teams are the eastern conference
        let eastern = teams
                        |> Seq.take conferencesize
                        |> Seq.map (mapteam Eastern)
        
        yield! eastern

        // last 15 are the western conference
        let western = teams
                        |> Seq.skip conferencesize
                        |> Seq.take conferencesize
                        |> Seq.map (mapteam Western)

        yield! western
    }

let printteams (teams : seq<Team>) =
    teams
    |> List.ofSeq
    |> printfn "%A"

let getteamsbyconference() =
    getteams()
    |> Seq.groupBy (fun t -> t.Conference)

let printteamsbyconference (conferences : seq<(Conference * seq<Team>)>) =
    conferences
    |> Seq.iter (fun (_, teams) -> teams |> printteams)

let getplayoffteams() =
    getteamsbyconference()
    |> Seq.map (fun (c, teams) -> (c, teams |> 
                                      Seq.sortBy (fun t -> t.Losses)
                                      |> Seq.take playoffteamsperconference))
let printplayoffbracket() =
    getplayoffteams()
    |> Seq.iter (fun (c, teams) -> 
                        Console.ForegroundColor <- ConsoleColor.Red

                        printfn "%A conference matchups\n" c

                        let topfour = teams |> Seq.take half
                        let bottomfour = teams |> Seq.skip half |> Seq.take half |> Seq.sortBy (fun t -> t.Wins)
                        
                        Console.ForegroundColor <- ConsoleColor.Yellow

                        bottomfour
                        |> Seq.zip topfour 
                        |> Seq.iter (fun (topseed, bottomseed) -> 
                                        printfn "%s (%d-%d) vs %s (%d-%d) \n" topseed.Name topseed.Wins topseed.Losses bottomseed.Name bottomseed.Wins bottomseed.Losses)
                        Console.ResetColor())

//do getteams() |> printteams
//do getteamsbyconference() |> printteamsbyconference
//do getplayoffteams() |> printteamsbyconference
do printplayoffbracket()