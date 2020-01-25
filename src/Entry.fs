module Entry
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open HookRouter
open System
open Browser
open Fable.Core
open ReactPrism
open Components


importSideEffects "./style.css"
importSideEffects "./prebuild.css"
importSideEffects "prismjs"
importSideEffects "prismjs/components/prism-fsharp"
importSideEffects "./prism-monokai.css"

type Route = {
    Name : string
    Route : string
    Fn : obj
}


let rl = ReactBindings.React.``lazy``

let private HomePage =
    Fable.React.FunctionComponent.Of<obj>
        ((fun _ ->
            article "About" [
               pargraph "Software Engineer and Mathematician with over 10 years of experience in F#"
               link "https://github.com/musheddev" "github"
               link "https://twitter.com/orlandotheegg" "twitter"
                    ] ), "Home")


//let HomePage: obj = rl (fun () -> importDynamic @"./pages/Home.fs")
let ThemingPage: obj = rl (fun () -> importDynamic @"./pages/ThemingWithFlora.fs")
let ActiveParsersPage: obj = rl (fun () -> importDynamic @"./pages/ActiveParsers.fs")

let pages : Route list = 
    [
        { Name = "About"; Route = "/"; Fn = HomePage}
        { Name = "Theming With Flora"; Route = "/theming"; Fn = ThemingPage}
        { Name = "Active Parsers"; Route = "/activeparsers"; Fn = ActiveParsersPage}
    ]

let routes =
    pages |> Seq.map (fun x -> 
        (* (x.Route.Remove(0,5))*) x.Route  ==> fun _ -> ReactBindings.React.createElement (x.Fn, null, [])
        ) |> createObj

let NotFoundPage =
    div []
        [ str "Page not found" ]

let menuItems currentPath =
        pages
        |> Seq.filter (fun p -> p.Route <> "/")
        |> Seq.map (fun p ->
            let isActive = p.Route = currentPath
            p.Route,p.Name, isActive)

let Loading = div [ Id "preloader" ] [ div [ Id "loader" ] [] ]

type SuspenseProp = Fallback of ReactElement

let suspense props children = ofImport "Suspense" "react" (keyValueList CaseRules.LowerFirst props) children

#if !DEBUG
setBasepath "/blog"
#endif

let layout path (r : ReactElement) =
  let menu = (menuItems path) |> Seq.map (fun (x,y,z) -> Components.sideNavItem x y z)
  Components.viewer
    (Components.sideNav menu)
    (r)

let App =
    FunctionComponent.Of
        ((fun _ ->
            let routeResults = useRoutes routes
            let path = usePath()

            match routeResults with
            | Some r -> suspense [ Fallback Loading ] [ layout path r ]
            | None -> layout path NotFoundPage),
         "App")

let app = document.getElementById "app"

[<EntryPoint>]
let main args =
  setBasepath "/blog"
  ReactDom.render (App(), app)
  0
