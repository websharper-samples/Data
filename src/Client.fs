namespace Workbooks.Education

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Charting

open FSharp.Data

[<JavaScript>]
module Client =    
    type WorldBank = WorldBankDataProvider<Asynchronous=true>
    let data = WorldBank.GetDataContext()

    let countries =
        [| data.Countries.Austria
           data.Countries.Hungary
           data.Countries.``United Kingdom``
           data.Countries.``United States`` |]

    let schoolEnrollment =
        countries
        |> Seq.map (fun c -> c.Indicators.``School enrollment, tertiary (gross), gender parity index (GPI)``)
        |> Async.Parallel

    let gdpPerCapita =
        countries
        |> Seq.map (fun c -> c.Indicators.``GDP per capita (current US$)``)
        |> Async.Parallel

    let mkData (i : Runtime.WorldBank.Indicator) =
        Seq.zip (Seq.map string i.Years) i.Values

    let randomColor =
        let rand = System.Random()
        fun () ->
            let r = rand.Next 256
            let g = rand.Next 256
            let b = rand.Next 256
            Color.Rgba(r,g,b,1.)

    let colors = 
        countries |> Array.map (fun _ -> randomColor ())

    let chart source =
        let cfg = ChartJs.CommonChartConfig()

        async {
            let! data = source
            return
                data
                |> Array.map mkData
                |> Array.zip colors
                |> Array.map (fun (c, e) -> 
                    Chart.Line(e)
                        .WithStrokeColor(c)
                        .WithPointColor(c))
                |> Chart.Combine
                |> fun c -> Renderers.ChartJs.Render(c, Size = Size(600, 400), Config = cfg)
        }

    let legend =
        div [] (colors 
             |> Array.zip countries
             |> Array.map (fun (c, color) -> 
                div [] [
                    span [attr.style <| "width: 15px; height: 15px;
                                             margin-right: 10px;
                                             display: inline-block;
                                             background-color: " + color.ToString()] []
                    span [] [text c.Name]
                ]))

    [<SPAEntryPoint>]
    let Main () =
        let chrt1 =
            chart schoolEnrollment
            |> View.Const
            |> View.MapAsync id
            |> Doc.EmbedView

        let chrt2 =
            chart gdpPerCapita
            |> View.Const
            |> View.MapAsync id
            |> Doc.EmbedView

        legend
        |> Doc.RunById "countries"
        
        Doc.Concat [
            h4 [] [text "Tertiary school enrollment (% gross)"]
            chrt1
        ]
        |> Doc.RunById "tertiary-enrollment"

        Doc.Concat [
            h4 [] [text "GDP per capita (in USD)"]
            chrt2
        ]
        |> Doc.RunById "gdp-per-capita"
