```c#
// AutoConsoleExample.cs
//
// Example using dsc_public.console.AutoConsole and CLIWord
// Demonstrates basic interactive CLI with autocomplete using RegexStartMatchIndex
//
// Author: Your Name
// License: MIT or your preferred license

using System;
using dsc_public.console;

class Program
{
    static void Main(string[] args)
    {
        // Initialize the AutoConsole
        var ac = new AutoConsole
        {
            Prompt = ">>> "
        };

        // === Define top-level commands ===

        // 'help' command, triggered by typing "h" or more
        var helpCmd = new CLIWord("help", 1, "Display help text");

        // 'exit' command, triggered by typing "e" or more
        var exitCmd = new CLIWord("exit", 1, "Exit the application");

        // === Define nested command: config set ===

        // 'config' is the base command
        var configCmd = new CLIWord("config", 1, "Manage configuration");

        // 'set' is a subcommand under config
        var setCmd = new CLIWord("set", 1, "Set configuration value");
        configCmd.AddSubWord(setCmd);

        // === Register all commands with autocomplete ===
        ac.AddWord(helpCmd);
        ac.AddWord(exitCmd);
        ac.AddWord(configCmd);

        Console.WriteLine("Interactive CLI Demo\n");
        Console.WriteLine("Try typing 'h' and press [Tab] for 'help'");
        Console.WriteLine("Try typing 'config s' and press [Tab] for 'config set'");
        Console.WriteLine("Type 'exit' to quit\n");

        // === Run the input loop ===
        while (true)
        {
            bool ok = ac.StartAutoConsole();
            string userInput = ac.LastCaptured;

            if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
                break;

            Console.WriteLine($"You entered: {userInput}\n");
        }

        Console.WriteLine("Goodbye!");
    }
}
```
# AutoConsole CLI Example

This project demonstrates how to use the `AutoConsole` and `CLIWord` classes from the `dsc_public.console` namespace to build an interactive CLI with:

- Syntax-aware input
- Autocompletion with [Tab]
- Line editing and history navigation
- Customizable prompt and colors

---

## ✨ Features

- Command history with [Up]/[Down] arrows
- Autocomplete using minimal keystrokes with `RegexStartMatchIndex`
- Supports nested commands (e.g., `config set`)
- Configurable debug levels and color themes

---

## 🚀 Running the Example

```bash
dotnet build
dotnet run