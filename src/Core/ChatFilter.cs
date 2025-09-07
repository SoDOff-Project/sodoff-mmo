

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


namespace sodoffmmo.Core;
internal static class ChatFilter
{

    public static ChatFilterConfiguration ChatFilterConfiguration { get; private set; } = new ChatFilterConfiguration();


    public static void Initialize()
    {


        using FileStream stream = File.OpenRead("src\\chatfilter.json");
        ChatFilterConfiguration? ChatFilterConfiguration = JsonSerializer.Deserialize<ChatFilterConfiguration>(stream);
        ChatFilter.ChatFilterConfiguration = ChatFilterConfiguration;
        
        if (ChatFilter.ChatFilterConfiguration is null)
        {
            Console.WriteLine("Filter list returned null");
            return;
        }

    }
}

internal sealed class ChatFilterConfiguration
{
    public string[] FilteredWords { get; set; } = Array.Empty<string>();
    public string[] charsToRemove { get; set; } = Array.Empty<string>();
    public string blockedMessage { get; set; } = string.Empty;

}