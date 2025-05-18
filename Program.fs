open System  
open System.IO  
open System.Text.RegularExpressions  

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

let processFile (filePath: string) =  
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
 printfn "done"  

[<EntryPoint>]  
let main argv =  
 let filePath = @"C:\Users\benja\OneDrive - HTL Vöcklabruck\Desktop\Floch-Bot-V2\discord_clean.txt" //paste filepath here

 if File.Exists(filePath) then  
     processFile filePath  
     0  
 else  
     printfn "Error: File not found at path: %s" filePath  
     1
