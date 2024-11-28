using dsc_public.console;

namespace yournamespace
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ///Initialize AutoConsole Object
            dsc_public.console.AutoConsole myconsole = new dsc_public.console.AutoConsole();
          
            ///Create some verbs, a few overloads available:
            ///CLIVerb(string fullName, string regex, string help, List<CLIVerb> subverbs, string displayname = "")
            ///CLIVerb(string completename, string regex, string help, CLIVerb subverb, string displayname = "")
            ///CLIVerb(string completename, string regex, string help, string displayname = "")
            ///CLIVerb(string completename, string regex, string displayname = "")
            ///CLIVerb(string completename, string displayname = "")
            ///CLIVerb()
            CLIVerb newVerb = new CLIVerb("today", "^tod?a?y?$", "This is a description for using today", "today");
            myconsole.AddVerb(newVerb);
            newVerb = new CLIVerb("yesterday", "^yes?t?e?r?d?a?y?$", "This is a description for using yesterday", "yesterday");
            myconsole.AddVerb(newVerb);
            newVerb = new CLIVerb("this-week", "^th?i?s?-?w?e?e?k?$", "This is a description for using this-week", "this-week");
            myconsole.AddVerb(newVerb);
            newVerb = new CLIVerb("last-week", "^las?t?-?w?e?e?k?$", "This is a description for using last-week", "last-week");
            myconsole.AddVerb(newVerb);

            myconsole.Prompt = "myprompt>";
            bool exit = false;
          
            do
            {
                myconsole.StartAutoConsole();
            
                switch (myconsole.LastCaptured.ToLower())
                {
                    case "?":
                        int maxCMDLength = 0;
                        foreach (CLIVerb displayCommands in cmdtp.AutocompleteTree)
                        {
                            if (displayCommands.DisplayName.Length > maxCMDLength)
                                maxCMDLength = displayCommands.DisplayName.Length;
                        }
                        foreach (CLIVerb displayCommands in cmdtp.AutocompleteTree)
                        {
                            Console.WriteLine(displayCommands.DisplayName.PadLeft(maxCMDLength) + "\t" + displayCommands.Help);
                        }
                        Console.WriteLine();
                        break;
                    case "a?":
                        myconsole.PrintEverything();
                        break;
                    case "history":
                        myconsole.PrintHistory();
                        break;
                    case "exit":
                    case "quit":
                    case "q":
                    case "done":
                        exit = true;
                        break;
                    default:
                        break;
                }
              
            } while (!exit);
        }
    }
}
