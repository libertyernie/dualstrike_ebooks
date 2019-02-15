namespace DualStrikeQuotes

module DualStrike =
    open System.Text.RegularExpressions
    open System.IO
    open System
    open System.Net
    open PragmaticSegmenterNet

    let span_rid = new Regex("""</span><span id="faqspan-[0-9]+">""")

    let readLines (sr: TextReader) = seq {
        let mutable finished = false
        while not finished do
            let l = sr.ReadLine()
            if isNull l then
                finished <- true
            else
                yield span_rid.Replace(l, "")
    }

    let filterLines (firstContains: string) (lastContains: string) (lines: seq<string>) = seq {
        let mutable started = false
        for line in lines do
            if line.Contains(firstContains) then
                started <- true
            if started then
                yield line
            if line.Contains(lastContains) then
                started <- false
    }

    let getParagraphs (lines: seq<string>) = seq {
        let buffer = new ResizeArray<string>()
        for line in lines do
            if line |> Seq.exists Char.IsLetter then
                buffer.Add(line)
            else
                if buffer.Count > 0 then
                    yield buffer |> String.concat " "
                buffer.Clear()
        if buffer.Count > 0 then
            yield buffer |> String.concat " "
    }

    let parseQuote (paragraphs: seq<string>) = seq {
        for p in paragraphs do
            let splitByColon = p.Split(':')
            if splitByColon.Length > 1 then
                let first = splitByColon.[0]
                let rest = splitByColon |> Seq.skip 1 |> String.concat ":"
                let rest2 = rest.Trim().Replace("\"", "")
                yield (first, rest2)
    }

    let readFaq = async {
        let req = WebRequest.CreateHttp "https://gamefaqs.gamespot.com/ds/924889-advance-wars-dual-strike/faqs/39218"
        use! resp = req.AsyncGetResponse()
        use sr = new StreamReader(resp.GetResponseStream())
        let! html = sr.ReadToEndAsync() |> Async.AwaitTask
    
        use stringReader = new StringReader(html)
        return
            stringReader
            |> readLines
            |> filterLines "Rachel, I want you to be careful" "Aha ha ha! Aha ha ha ha ha!"
            |> getParagraphs
            |> Seq.map WebUtility.HtmlDecode
            |> parseQuote
            |> Seq.toList
    }

    let removeEllipsis (str: string) =
        str.Replace("...", "xxxELLIPSISxxx").Replace("..", "xxxELLIPSISxxx")

    let restoreEllipsis (str: string) =
        str.Replace("xxxELLIPSISxxx", "...")

    let getAllSentences = async {
        let! faq = readFaq
        return faq
            |> Seq.map snd
            |> Seq.map removeEllipsis
            |> Seq.collect Segmenter.Segment
            |> Seq.map restoreEllipsis
            |> Seq.distinct
            |> Seq.cache
    }

    let GetAllSentencesAsync () = getAllSentences |> Async.StartAsTask