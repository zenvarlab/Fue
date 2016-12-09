﻿module Fue.Tests.Parser

open NUnit.Framework
open FsUnit
open Fue.Core
open Fue.Parser
open FSharp.Data
open Fue.Rop

[<Test>]
let ``Parses simple value`` () = 
    "value" 
    |> parseTemplateValue 
    |> should equal (TemplateValue.SimpleValue("value"))

[<Test>]
let ``Parses function value`` () = 
    "value()" 
    |> parseTemplateValue 
    |> should equal (TemplateValue.Function("value", []))

[<Test>]
let ``Parses method`` () = 
    "record.Method()" 
    |> parseTemplateValue 
    |> should equal (TemplateValue.Function("record.Method", []))

[<Test>]
let ``Parses piped function value`` () = 
    "value |> fun1 |> fun2" 
    |> parseTemplateValue 
    |> should equal (
        TemplateValue.Function("fun2", 
            [
                TemplateValue.Function("fun1", [TemplateValue.SimpleValue("value")])
            ]))

[<Test>]
let ``Parses piped curried function value`` () = 
    "value |> fun1 y |> fun2 x" 
    |> parseTemplateValue 
    |> should equal (
        TemplateValue.Function("fun2", 
            [
                TemplateValue.SimpleValue("x")
                TemplateValue.Function("fun1", [TemplateValue.SimpleValue("y"); TemplateValue.SimpleValue("value")])
            ]))

[<Test>]
let ``Parses piped curried function value with first function`` () = 
    "value() |> fun1 y" 
    |> parseTemplateValue 
    |> should equal (
        TemplateValue.Function("fun1", 
            [
                TemplateValue.SimpleValue("y")
                TemplateValue.Function("value", [])
            ]))

[<Test>]
let ``Parses function value with params`` () = 
    "value(a,b)" 
    |> parseTemplateValue 
    |> should equal (
        TemplateValue.Function("value", 
            [TemplateValue.SimpleValue("a"); TemplateValue.SimpleValue("b")]))

[<Test>]
let ``Parses function value with inner function`` () = 
    "value(a,b(x),c())" 
    |> parseTemplateValue 
    |> should equal (
        TemplateValue.Function("value", 
            [
                TemplateValue.SimpleValue("a"); 
                TemplateValue.Function("b", [TemplateValue.SimpleValue("x")]);
                TemplateValue.Function("c", []);
            ]))

[<Test>]
let ``Parses simple value with white spaces`` () = 
    "value (a, b)" 
    |> parseTemplateValue 
    |> should equal (
        TemplateValue.Function("value", 
            [TemplateValue.SimpleValue("a"); TemplateValue.SimpleValue("b")]))

[<Test>]
let ``Parses for-cycle value`` () = 
    "x in y" 
    |> parseForCycle 
    |> should equal (TemplateNode.ForCycle("x", TemplateValue.SimpleValue("y")) |> Some)

[<Test>]
let ``Parses for-cycle value with function`` () = 
    "x in y(z)" 
    |> parseForCycle 
    |> should equal (TemplateNode.ForCycle("x", TemplateValue.Function("y", [TemplateValue.SimpleValue("z")])) |> Some)

[<Test>]
let ``Does not parse illegal for-cycle value`` () = 
    "in y" 
    |> parseForCycle 
    |> should equal None

[<Test>]
let ``Parses discriminated union case with no extraction`` () = 
    "Case" 
    |> parseDiscriminatedUnion "DU"
    |> should equal (TemplateNode.DiscriminatedUnion("DU", "Case", []))
    
[<Test>]
let ``Parses discriminiated case with extract`` () = 
    "Case(x, _)" 
    |> parseDiscriminatedUnion "DU"
    |> should equal (TemplateNode.DiscriminatedUnion("DU", "Case", ["x";"_"]))

[<Test>]
let ``Parses include`` () = 
    let expected = TemplateNode.Include("src.html", 
                    [
                        ("x", TemplateValue.SimpleValue("y"))
                        ("z", TemplateValue.Function("run", [TemplateValue.SimpleValue("a")]))
                    ])
    "x=y;z=run(a)" |> parseInclude "src.html" |> should equal expected

[<Test>]
let ``Parses include with no data`` () = 
    let expected = TemplateNode.Include("src.html", [])
    "" |> parseInclude "src.html" |> should equal expected

let parseNodeSuccess = parseNode >> extract >> Option.get

[<Test>]
let ``Parses for cycle node`` () = 
    let expected = TemplateNode.ForCycle("i", TemplateValue.SimpleValue("list"))
    HtmlNode.NewElement("a", [("fs-for","i in list")])
    |> parseNodeSuccess 
    |> should equal expected

[<Test>]
let ``Parses if condition node`` () = 
    let expected = TemplateNode.IfCondition(TemplateValue.SimpleValue("boolVal"))
    HtmlNode.NewElement("a", [("fs-if","boolVal")])
    |> parseNodeSuccess 
    |> should equal expected

[<Test>]
let ``Parses discriminated union node`` () = 
    let expected = TemplateNode.DiscriminatedUnion("union", "case", ["a";"_"])
    HtmlNode.NewElement("a", [("fs-du","union");("fs-case","case(a,_)")])
    |> parseNodeSuccess 
    |> should equal expected

[<Test>]
let ``Parses include node`` () = 
    let expected = TemplateNode.Include("zdroj.html", [("lambda", TemplateValue.SimpleValue("zdrojLambdy"))])
    HtmlNode.NewElement("fs-include", [("fs-src","zdroj.html");("fs-data","lambda=zdrojLambdy")])
    |> parseNodeSuccess 
    |> should equal expected