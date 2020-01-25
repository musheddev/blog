module ActiveParsersPage

open Fable.Core.JsInterop
open ReactPrism
open Components

let private ActiveParsersPage =
    Fable.React.FunctionComponent.Of<obj>
        ((fun _ ->
            article "Active Parsers" [   
                pargraph "A while ago I wrote a css parser as part of a css provider. 
                 Originally I wanted write a blog post about theming with css variables using the css provider but I also desire to make the date I signed up for.
                 However the parsing strategies I used are interesting on their own right.
                 Lets start with the parser type.
                "
                prismCode "type ActiveParser<'a,'t> = IStream<'t> -> ('a * IStream<'t>) option"
                pargraph "Let us also look at the FParsec definition"
                prismCode "type Parser<'TResult, 'TUserState> = CharStream<'TUserState> -> Reply<'TResult>"
                pargraph "'a and 'TResult are equivalent being the type of desired result say an AST or a Token in the case of a tokenizer.
                This result is encapsulated into two different compound types, in the FParsec case, the Reply type has Ok and Error states, whereas I used the option type.
                Both IStream<'t> and CharStream<'TUserState> are stateful interfaces in which the position of the stream is particularly important in both. 
                The 'TUserState is a state not controlled by the CharStream but useful for a stateful parser which are parsers which require some contextual information like being in {} block or indentation level. 
                The CharStream's position state will be mutated by a given parser, where as in the active parser the IStream<'t> position state is built into a new IStream<'t> object in the return leaving the original unaffected.
                The 't in IStream is the stream type which in our first case will be char.
                "
                prismCode "type IStream<'t> =
    abstract Read : int -> 't [] option //read number items off of stream if available
    abstract Consume : int -> IStream<'t> //advance the head in a new stream
    abstract Head : unit -> 't option //read the item at the head
    abstract SubSearch : ('t -> 't -> bool) * 't [] -> int option //given a comparator and an array will find the local of that array in the stream
    abstract Search : ('t -> 't -> bool) *'t -> int option //given a comparator and item will find the position of that item if possible
    abstract Length : unit -> int //total length of the stream
    abstract Position : unit -> int //position of head of stream"
                pargraph "Now this interface is functionally pure as long as the implementation of Consume does not return itself. 
                Further implementation details like if it was wrapping a System.IO.Stream could include more state or internal caching logic etc."

                prismCode "let PString (s : string) : ActiveParser<unit,char> =
  fun stream -> 
    match stream.Read(s.Length) with
    | Some(str) when String(str) = s -> Some((),stream.Consume(s.Length))
    | _ -> None"

                pargraph "Our first simple parser. Let's activate it by casting it into an active pattern."
                prismCode "let (|PString|_|) = PString"
                pargraph "Active patterns have been used in several other libraries before for parsing, but let's look at a use case to see why."
                prismCode "let tokenise (input : IStream<char>) = //IStream<char> -> Token * IStream<char>
    match input with
    | PChar ']' (_,left) -> Token.SquareEnd, left
    | PString \"^=\" (_,left) -> Token.PrefixMatch, left
    | PChar '{' (_,left)-> Token.CurlyStart, left
    | PChar '}' (_,left) -> Token.CurlyEnd, left
    | PString \"|=\" (_,left) -> Token.DashMatch, left
    | PString \"||\" (_,left) -> Token.Column, left
    | PString \"~=\" (_,left) -> Token.IncludeMatch, left
    | Char (chr,left) -> Token.Delim chr, left"
                pargraph "This is only a small part of the css tokenizer but easy to follow. Notice the function signature looks like an ActiveParser.
                It is missing the optional result because the css spec requires unmatched chars will marked as Delimitator tokens. 
                This structure becomes straight forward to unfold."
                prismCode "input |> Seq.unfold (fun stream -> if stream.Head().IsNone then None else Some(tokenise stream))"
                h2 "Composability"
                pargraph "In this next example which checks if the char is legal for a Identifier token is built from another parser \"Char\". 
                Note that a UTF-8 char can multiple bytes and the characters that do are legal."
                prismCode "let (|IdentCodon|_|) = function
    | Char(c,left) ->
        match UTF8Encoding.UTF8.GetBytes([|c|]) with
        | [|u|] when u >= 65uy && u <= 90uy -> Some(c)
        | [|u|] when u >= 97uy && u <= 122uy -> Some(c)
        | [|u|] when u >= 48uy && u <= 57uy -> Some(c)
        | [|95uy|] -> Some(c)
        | [|u|] when u > 128uy -> Some(c)
        | ary when ary.Length > 1 -> Some(c)
        | _ -> None
        |> Option.map (fun x -> x,left)
    | _ -> None"
                pargraph "We can enhance the compossibility even more using Parser Combinators modeled from FParsec. 
                We will start by defining the monadic Bind, Return and Either function."
                prismCode "let Return (x: 'a): ActiveParser<'a,'t> =
  fun stream -> Some(x, stream) //new parser that returns x not consuming from the stream
  
let Bind (p: ActiveParser<'a,'t>) (f: 'a -> ActiveParser<'b,'t>) : ActiveParser<'b,'t> =
  fun stream -> //new parser
    match p stream with //that will run p
    | Some(x, rest) -> (f x) rest //if successful then exec f and run the result of f
    | None -> None

let Either (p1: ActiveParser<'a,'t>) (p2: ActiveParser<'a,'t>) : ActiveParser<'a,'t> =
  fun stream -> //new parser
    match p1 stream with //that will run p1
    | None -> p2 stream //if not successful then run p2
    | res -> res
                "
                pargraph "Now for the parsec operators."
                prismCode "let (>>=) = Bind

let (>>%) p x : ActiveParser<'b,'t> =
    p >>= (fun _ -> Return x) //run p; if successful return x instead

let (>>.) p1 p2 : ActiveParser<'b,'t> =
    p1 >>= (fun _ -> p2) //run p1; if successful run p2 and keep its result instead

let (.>>) p1 p2 : ActiveParser<'a,'t> =
    p1 >>= (fun x -> p2 >>% x) //run p1; if successful run p2 if that is successful too then keeps the p1's result

let (.>>.) p1 p2: ActiveParser<'a*'b,'t> =
    p1 >>= (fun x -> p2 >>= (fun y -> Return (x, y))) //run p1; if successful run p2; if successful keep both results as tuple

let (<|>) = Either"
                pargraph "Now some use case examples. I included the first line to show that a parser defined as an Active Pattern can be named as a regular function type."
                prismCode "let Ident = (|Ident|_|)

let (|Function|_|) = Ident .>> PChar '(' 

let (|AtKeyword|_|) = PChar '@' >>. Ident

let (|Space|_|) = NewLine <|> PChar '\\t' <|> PChar ' '"
                pargraph "Using the Bind and Return functions we can also build a computation expression though I generally use the other forms."
                prismCode "type ParserBuilder() =
    member x.Zero () = fun stream -> Some((),stream)
    member x.Bind(p, f) = Bind p f
    member x.Return(y) = Return y

let parser = new ParserBuilder()

let (|Comment|_|)  =
  parser {
    let! chrs = PString \"/*\" >>. SplitWith \"*/\" //SplitWith is a parser that utilizes the IStream search capabilites
    return String(chrs)
  }"
                h2 "Multi Stage Parsing"
                pargraph "There is still a big problem; these parsers are not contextually stateful in the way to parse nested blocks.
                However we can get around that and utilize separation of concerns to make the css parser more easier to verify. 
                So the first step I built a Tokenizer and showed how we can unfold that to a seq of tokens, 
                and since IStream is generic we can build an IStream<Token>. 
                Next we can build a shaper which only concern is to resolve recursive block structures of css.
                Lets define that structure." 

                prismCode "type StylesheetShape = RuleShape list

and RuleShape =
    | Qualified of ComponentShape list * BlockShape
    | At of string * ComponentShape list * BlockShape

and ComponentShape =
    | Preserved of Token
    | CurlyBlock of BlockShape //{}
    | ParenBlock of BlockShape //()
    | SquareBlock of BlockShape //[]
    | Function of string * BlockShape

and BlockShape = ComponentShape list"

                pargraph "The next bit of code I am going to show are the recursive parsers for shaping stage. 
                They are the most complex of the parsers, so I will try to explain.
                The active parsers by definition yield a new IStream (usally called \"left\") which can be further matched against which is being done here. 
                The Head parser which matches Head of the stream to a Token; instead of resulting with left in the second tuple position it is being matched with CompShapeList which is another active parser.
                The next parser CompShapeList using a different kind of unfold function which encapsulates all the permutations for yielding a result and continuing with unfold: Break, BreakWith, Continue, ContinueWith.
                Some rejiggering of the result of unfold was required to fit in an active parser. Also take note of the mutual recursion of CompShape and CompShapeList.
                Cool part about this is not that it removes overall complexity of the parsing task but it isolates it from the other stages making it more analyzable/verifiable/testable."
                prismCode "let rec (|CompShape|_|) = function
    | Head (Token.Function(name), CompShapeList Token.ParenEnd (inner,left)) -> 
      Some(ComponentShape.Function(name,inner),left)
    | Head (Token.SquareStart, CompShapeList Token.SquareEnd (inner,left)) -> 
      Some(ComponentShape.SquareBlock(inner),left)
    | Head (Token.CurlyStart, CompShapeList Token.CurlyEnd (inner,left)) -> 
      Some(ComponentShape.CurlyBlock(inner),left)
    | Head (Token.ParenStart, CompShapeList Token.ParenEnd (inner,left)) -> 
      Some(ComponentShape.ParenBlock(inner),left)
    | Head (a, left) -> Some(ComponentShape.Preserved a, left)
    | _ -> None

and (|CompShapeList|_|) (terminal : Token) (input : IStream<Token>) =
    Stream.unfold (function
        | Head (a,left) when a = terminal -> Unfold.Break,left
        | CompShape(shape,left) -> Unfold.ContinueWith(shape),left
        | _ -> failwith \"broken shaper\") input
    |> (function | ([],s) -> None | (x,s) -> Some(x,s))

and (|RuleShape|_|) = function
    | CompShapeList Token.SwiggleStart (inner,CompShapeList Token.SwiggleEnd (inner2, left)) ->
        Some(RuleShape.Qualified(inner,inner2),left)
    | Head (Token.At(name), CompShapeList Token.SwiggleStart (inner,CompShapeList Token.SwiggleEnd (inner2, left))) ->
        Some(RuleShape.At(name,inner,inner2),left)
    | _ -> None

let parseShape (input : IStream<Token>) : StylesheetShape =
    Stream.unfold (function
        | RuleShape(r,left) -> Unfold.ContinueWith(r),left
        | Head (a, left) -> Unfold.Continue,left //do not preserve tokens not matching rules (TODO: collect comments)
        | s -> Unfold.Break,s
        ) input
    |> fst"
                pargraph "The final stage of the css parser primary operates off of IStream<ComponentShape> converting from ComponentShape list. 
                No recursive/stateful parsers were needed. The final output type \"Stylesheet\" is a well structured representation of css."
                pargraph "A note about performance: This active parser design was designed to be able to isolate different parts the parsing to be able to correlate them to the different spec documents. 
                Css tokenization and css object model were different documents. Readability and verifyabilty were of most importance. 
                However it is adequately fast, able parse large css files in less than a second. The streams use a shared reference to the memory so there are no Large Object Heap allocations every time the .Consume returns a new IStream."
                pargraph "Perhaps this post could help you the next time you need to parse some structured document."
                link "https://github.com/Fable-Fauna/Fable.Flora" "https://github.com/Fable-Fauna/Fable.Flora"
                
                 ] ), "Active Parsers")

exportDefault ActiveParsersPage