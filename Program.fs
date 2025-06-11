open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions

type Author = {
    id: string
    isBot: bool
}

type Message = {
    content: string
    author: Author
    timestamp: DateTime
    ``type``: string
}

type MessageContainer = {
    messages: Message[]
}

let containsLink (line: string) =
    let pattern = @"https?://\S+"
    Regex.IsMatch(line, pattern)

let containsDate (line: string) =
    let pattern = @"^\[\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2} [APap][Mm]\]"
    Regex.IsMatch(line, pattern)

let containsAttachmentOrEmbed (line: string) =
    line.Contains("{Attachment}") || line.Contains("{Embed}") || line.Contains("{Attachments}") || line.Contains("{Embeds}")

let removeDateAndName (line: string) =
    let pattern = @"^\[\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2} [APap][Mm]\] \w+ "
    Regex.Replace(line, pattern, "")

let isInvalid (message: Message) =
    if message.author.isBot then true
    elif message.``type`` <> "Default" then true
    else
        message.content.StartsWith("!") ||
        message.content.StartsWith("$") ||
        message.content.StartsWith(".") ||
        message.content.StartsWith("-") ||
        message.content.StartsWith("edy:") ||
        message.content.StartsWith("@EdyBot")

let cleanMessages (messages: Message[]) =
    let mutable cleanMessages = ""
    let mutable lastMessage = messages.[0]
    
    for message in messages do
        if not (isInvalid message) then
            let cleanedMessage = message.content.Replace("\n", ". ")
            let timeDiff = message.timestamp - lastMessage.timestamp
            
            if timeDiff.TotalMinutes > 10.0 then
                cleanMessages <- cleanMessages + "\n\n\n" + message.content
            else
                if lastMessage.author.id = message.author.id then
                    cleanMessages <- cleanMessages + ". " + cleanedMessage  
                else
                    cleanMessages <- cleanMessages + "\n" + cleanedMessage
            
            lastMessage <- message
    
    cleanMessages

let processJsonFile (inputPath: string) (outputPath: string) =
    try
        let jsonContent = File.ReadAllText(inputPath)
        let options = JsonSerializerOptions()
        options.PropertyNameCaseInsensitive <- true
        let messageContainer = JsonSerializer.Deserialize<MessageContainer>(jsonContent, options)
        
        printfn "Processing %s with %d messages..." (Path.GetFileName(inputPath)) messageContainer.messages.Length
        
        let cleanedText = cleanMessages messageContainer.messages + "\n\n"
        File.WriteAllText(outputPath, cleanedText)
        printfn "Processed: %s -> %s" (Path.GetFileName(inputPath)) (Path.GetFileName(outputPath))
    with
    | ex -> printfn "Error processing %s: %s" inputPath ex.Message

let processTextFile (filePath: string) =
    let buffer = File.ReadAllText(filePath).Trim('\n', '\r')
    let lines = buffer.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    let filteredLines =
        lines
        |> Array.filter (fun line ->
            not (containsLink line) &&
            not (containsDate line) &&
            not (containsAttachmentOrEmbed line) &&
            not (String.IsNullOrWhiteSpace(line)))
        |> Array.map removeDateAndName
    let cleanedContent = String.Join("\n", filteredLines)
    File.WriteAllText(filePath, cleanedContent)
    printfn "Text file processed: %s" (Path.GetFileName(filePath))

let processDirectory (inputDir: string) (outputDir: string) =
    if not (Directory.Exists(inputDir)) then
        printfn "Input directory does not exist: %s" inputDir
    else
        if not (Directory.Exists(outputDir)) then
            Directory.CreateDirectory(outputDir) |> ignore
        
        let files = Directory.GetFiles(inputDir, "*.json")
        printfn "Processing %d JSON files..." files.Length
        
        for file in files do
            let fileName = Path.GetFileNameWithoutExtension(file)
            let outputPath = Path.Combine(outputDir, fileName + ".txt")
            processJsonFile file outputPath

[<EntryPoint>]
let main argv =
    match argv with
    | [| inputDir; outputDir |] when Directory.Exists(inputDir) ->
        processDirectory inputDir outputDir
        0
    | [| filePath |] when File.Exists(filePath) ->
        if Path.GetExtension(filePath).ToLower() = ".json" then
            let outputPath = Path.ChangeExtension(filePath, ".txt")
            processJsonFile filePath outputPath
        else
            processTextFile filePath
        0
    | _ ->
        printfn "Usage:"
        printfn "  For JSON processing: %s <input_directory> <output_directory>" (System.AppDomain.CurrentDomain.FriendlyName)
        printfn "  For single file: %s <file_path>" (System.AppDomain.CurrentDomain.FriendlyName)
        printfn "Examples:"
        printfn "  %s \"C:\\path\\to\\in\" \"C:\\path\\to\\out\"" (System.AppDomain.CurrentDomain.FriendlyName)
        printfn "  %s \"C:\\path\\to\\file.json\"" (System.AppDomain.CurrentDomain.FriendlyName)
        printfn "  %s \"C:\\path\\to\\file.txt\"" (System.AppDomain.CurrentDomain.FriendlyName)
        1