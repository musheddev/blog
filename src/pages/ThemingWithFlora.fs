module ThemingPage

open Fable.Core.JsInterop
open ReactPrism
open Components

let private HomePage =
    Fable.React.FunctionComponent.Of<obj>
        ((fun _ ->
            article "Site Theaming with Css varibles and Flora.CssProvider" [   
                pargraph "WIP"
                ] ), "Home")

exportDefault HomePage