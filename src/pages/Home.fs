module HomePage

open Fable.Core.JsInterop
open Components
open Feliz

let private HomePage =
    Fable.React.FunctionComponent.Of<obj>
        ((fun _ ->
            article "About" [
               pargraph "Software Engineer and Mathematician with over 10 years of experience in F#"
               link "https://github.com/musheddev" "github"
               link "https://twitter.com/orlandotheegg" "twitter"
                    ] ), "Home")

exportDefault HomePage
