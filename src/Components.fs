module Components

open Fable.React
open Flora.CssProvider
open System
open Feliz
open HookRouter

type Css = Flora.Stylesheet<"src/prebuild.css", naming = NamingMode.Verbatim >

let h2 (title : string) =
    Html.h3 [
        prop.classes [Css.``text-xl``]
        prop.children [Html.text title]]

let article (title : string) (content : seq<ReactElement>) =
    Html.article [
        prop.classes [Css.``w-4/5``]
        prop.children 
            (Seq.append
            [Html.h1 [
                prop.classes [Css.``text-4xl``]
                prop.children [ Html.text title]]]
            content)
        
    ] 

let pargraph (content : string) = 
    Html.paragraph [
        prop.children [ Html.text content]
    ]

let link (href : string) (txt : string) =
    HookRouter.A [ AProps.Href href ] [str txt] 

let sideNavItem href text selected =
    Html.li [
        prop.classes [Css.``hover:bg-secondary-base``; Css.``bg-transparent``; Css.``border-l-4``;] 
        prop.children [
            link href text
        ]
    ]

let sideNav (content : seq<ReactElement>) =
    Html.div [
        prop.classes [ Css.``w-1/5``; Css.``leading-normal``]
        prop.children [
            Html.img [prop.src "JulianDisc3_Black.jpg" ] 
            Html.h1 [
                prop.classes [Css.``text-base``; Css.``font-bold``; Css.``text-4xl``]
                prop.children [str "Orlando Anderegg"] ]
            Html.h2 [
                prop.classes [Css.``text-base``; Css.``font-bold``]
                prop.children [str "Menu"] ]
            Html.ul [
                prop.children content ]
        ]
    ]


let viewer nav article =
    Html.div [
        prop.classes [
            Css.``bg-background-base``
            Css.container 
            Css.``w-full`` 
            Css.flex 
            Css.``flex-wrap`` 
            Css.``mx-auto``
        ]
        prop.children [
            nav
            article
        ]]