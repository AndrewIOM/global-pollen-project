module Docs

open Markdig
open System.IO
open System.Text.RegularExpressions
open System.Collections.Generic

type Heading = {
    Name:        string
    LinkId:      string
    SubHeadings: string list
}

type DocViewModel = {
    Html: string
    Metadata: IDictionary<string,string>
    Headings: Heading list
}

let pipeline = 
    MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .UseAutoIdentifiers()
        .UseBootstrap()
        .Build()

let lineToKeyValue (line:string) =
    let p = line.Split(':')
    match p |> Array.length with
    | 2 -> Some (p.[0].Trim(),p.[1].Trim())
    | _ -> None

let extractFrontMatter filePath =
    let markdownLines = File.ReadAllLines filePath
    if markdownLines |> Array.isEmpty
        then []
        else
            match markdownLines |> Array.head with
            | "---" ->
                let endIndex = 
                    markdownLines 
                    |> Array.skip 1 
                    |> Array.tryFindIndex (fun l -> l = "---")
                match endIndex with
                | None -> []
                | Some i ->
                    markdownLines
                    |> Array.skip 1
                    |> Array.take i
                    |> Array.choose lineToKeyValue
                    |> Array.toList
            | _ -> []

let getFileMarkdown filePath =
    let text = File.ReadAllText filePath
    Markdown.ToHtml(text,pipeline)

let getSidebarHeadings (html:string) =
    let regex = "<h3[^>]*?>(?<TagText>.*?)</h3>"
    let headerTags = Regex.Matches(html,regex,RegexOptions.Singleline)
    headerTags
    |> Seq.map (fun h -> { Name = Regex.Replace(h.Value, "(<h[^>]*>|</h[^>]*>)", "")
                           LinkId = Regex.Replace(h.Value, "(<h[^>] id=\"|\">.*)", "")
                           SubHeadings = []})
    |> Seq.toList

let guideDocuments =
    match Directory.Exists "Docs" with
    | false -> [||]
    | true ->
        Directory.GetFiles "Docs"
        |> Array.sort
        |> Array.map (fun f ->
            let meta = extractFrontMatter f
            meta, getFileMarkdown f)
