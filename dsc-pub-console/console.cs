using System.Data;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

/*
 * Adapted from code posted on stackexchange.com
 * Posted by: https://codereview.stackexchange.com/users/109745/benkoshy
 * Code reference: https://gist.github.com/benkoshy/7f6f28e158032534615773a9a1f73a10
 *         Author: https://github.com/benkoshy
 * Extended by Walter Holm from Dijkstra Solutions Company
 */

namespace dsc_public
{
    namespace console
    {
        public class ForeBack
        {
            public ConsoleColor FG { get; set; } = ConsoleColor.Gray;
            public ConsoleColor BG { get; set; } = ConsoleColor.Black;
        }
        public class Colors
        {
            public ForeBack Main { get; set; } = new ForeBack();
            public ForeBack Help { get; set; } = new ForeBack();
            public ForeBack Error { get; set; } = new ForeBack();
            public ForeBack Warning { get; set; } = new ForeBack();
            public ForeBack Prompt { get; set; } = new ForeBack();
            public ForeBack Input { get; set; } = new ForeBack();
            public ForeBack Debug { get; set; } = new ForeBack();

            public Colors()
            {
                Main.FG = ConsoleColor.Gray;
                Main.BG = ConsoleColor.Black;
                Help.FG = ConsoleColor.Yellow;
                Help.BG = ConsoleColor.Black;
                Error.FG = ConsoleColor.Cyan;
                Error.BG = ConsoleColor.Black;
                Warning.FG = ConsoleColor.Red;
                Warning.BG = ConsoleColor.Black;
                Prompt.FG = ConsoleColor.Gray;
                Prompt.BG = ConsoleColor.Black;
                Input.FG = ConsoleColor.Gray;
                Input.BG = ConsoleColor.Black;
                Debug.FG = ConsoleColor.Magenta;
                Debug.BG = ConsoleColor.Black;
            }
        }



        public class AutoConsole
        {
            public Colors ConsoleColors { get; set; } = new Colors();
            public List<CLIVerb> AutocompleteTree { get; set; } = new List<CLIVerb>();
            public string LastCaptured
            {
                get
                {
                    return this._builder.ToString().Trim();
                }
            }

            /// <summary>
            /// Intended to expand all verbs even if not completed by user input
            /// Any unrecognized input is untouched
            /// </summary>
            public string LastCompleteVerbPhrase
            {
                get
                {
                    Queue<string> commandQueue = new Queue<string>(LastCaptured.Trim().Split(' '));
                    return ReturnAllMatch(commandQueue, AutocompleteTree);
                }
            }

            public string Prompt = "";
            /// <summary>
            /// Default Help Char is ?, you can change this
            /// </summary>
            public char SingleHelpChar = '?';

            public List<string> ReservedList = new List<string>();

            private StringBuilder _builder = new StringBuilder();

            private List<string> _CommandList = new List<string>();

            public int MaxCommandLength
            {
                get
                {
                    return _MaxCommandLength;
                }
            }

            private int _CommandIndex = 0;
            private int _CursorRow = Console.CursorTop;
            private int _CursorColumn = Console.CursorLeft;
            private int _InsertAt = 0;
            private int _Position = 0;
            public bool Debug
            {
                get
                {
                    return _Debug;
                }
                set
                {
                    _Debug = value;
                }
            }
            private bool _Debug = false;
            public bool DebugPosition = false;
            private int _MaxCommandLength = 0;

            private void PrintDebugPosition()
            {
                var (saveLeft, saveTop) = Console.GetCursorPosition();
                int LeftPosition = (Console.WindowWidth - _Position.ToString().Length - saveLeft.ToString().Length - 4);

                Console.SetCursorPosition(0, 0);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(LeftPosition, 0);
                Console.Write($"[{saveLeft}][{_Position}]");
                Console.SetCursorPosition(saveLeft, saveTop);
            }
            public bool StartAutoConsole(string SetBuffer = "", bool noCRLF = false, bool debug = false)
            {
                _Debug = debug;

                try
                {
                    ConsoleColors.Main.FG = Console.ForegroundColor;
                    ConsoleColors.Main.BG = Console.BackgroundColor;

                    _builder = new StringBuilder(SetBuffer);

                    SetConsoleColors(ConsoleColors.Main);

                    if (debug)
                        Console.WriteLine("Help Character is " + this.SingleHelpChar.ToString());

                    _CursorRow = Console.CursorTop;
                    _CursorColumn = Console.CursorLeft;

                    _Position = this.Prompt.Length + _builder.Length;

                    if (DebugPosition)
                    {
                        //advance the line to leave room for the counter
                        Console.WriteLine();
                    }

                    SetConsoleColors(ConsoleColors.Prompt);

                    Console.Write($"{Prompt}");

                    SetConsoleColors(ConsoleColors.Input);

                    Console.Write($"{_builder}");

                    SetConsoleColors(ConsoleColors.Main);

                    if (DebugPosition)
                    {
                        SetConsoleColors(ConsoleColors.Debug);
                        PrintDebugPosition();
                        SetConsoleColors(ConsoleColors.Main);
                    }

                    ConsoleKeyInfo capturedCharacter = new ConsoleKeyInfo();

                    SetConsoleColors(ConsoleColors.Input);

                    while (EnterIsNotThe(capturedCharacter) && NotSingleHelpChar(capturedCharacter))
                    {
                        _Position = this.Prompt.Length + _builder.Length;
                        capturedCharacter = Console.ReadKey(intercept: true);
                        _Position = this.Prompt.Length + _builder.Length;
                        this.HandleKeyInput(capturedCharacter);
                        _Position = this.Prompt.Length + _builder.Length;
                        if (DebugPosition)
                        {
                            SetConsoleColors(ConsoleColors.Debug);
                            PrintDebugPosition();
                            SetConsoleColors(ConsoleColors.Input);
                        }
                    }

                    if (NotSingleHelpChar(capturedCharacter))
                        Console.Write(capturedCharacter.KeyChar);

                    if (_builder.ToString().Trim().Length != 0)
                    {
                        if (IsNotReservedWord() && NotSingleHelpChar(capturedCharacter))
                            this._CommandList.Add(_builder.ToString().Trim());
                        _InsertAt= _builder.Length;
                    }

                    if (debug)
                    {
                        SetConsoleColors(ConsoleColors.Debug);
                        Console.WriteLine();
                        Console.Write("<echo>" + _builder.ToString().Trim() + "</echo><length>" + _builder.ToString().Length + "</length>");
                        SetConsoleColors(ConsoleColors.Input);
                    }
                    this._CommandIndex = _CommandList.Count;

                    if (!noCRLF)
                        Console.WriteLine();

                    Console.ForegroundColor = ConsoleColors.Main.FG;
                    Console.BackgroundColor = ConsoleColors.Main.BG;
                }
                catch (Exception ex)
                {
                    SetConsoleColors(ConsoleColors.Error);
                    Console.Write(ex.ToString());
                    SetConsoleColors(ConsoleColors.Main);
                    return false;
                }

                return true;
            }
            private string ExtractMatch() //need to address multiple matches
            {
                //using the entire string, find the last match, search by space sep
                string[] matches = _builder.ToString().Split(' ', 3);
                bool foundMatch = false;

                List<CLIVerb> SearchVerbs = new List<CLIVerb>();
                SearchVerbs.AddRange(AutocompleteTree);

                if (_builder.Length > 0)
                {
                    if (matches.Length > 0)
                    {
                        int lastMatch = matches.Length - 1;

                        for (int i = 0; i < matches.Length; i++)
                        {
                            foreach (CLIVerb eachVerb in SearchVerbs)
                            {
                                if (eachVerb.IsMatch(matches[i]) && i != lastMatch)
                                {
                                    foundMatch = true;
                                    SearchVerbs = new List<CLIVerb>();
                                    SearchVerbs.AddRange(eachVerb.CLISubVerbs);
                                    break;
                                }

                                if (eachVerb.IsMatch(matches[i]) && i == lastMatch)
                                {
                                    foundMatch = true;
                                    if (eachVerb.CompleteName.StartsWith("<r/>"))
                                        return matches[i];
                                    else
                                        return eachVerb.CompleteName;
                                }
                            }

                            if (!foundMatch)
                                break;
                        }
                    }
                }

                return ("");
            }

            private string ReturnAllMatch(Queue<string> Commands, List<CLIVerb> verbs)
            {
                StringBuilder result = new StringBuilder();

                this.Debug = true;

                if (this._Debug)
                {
                    Console.WriteLine("ReturnAllMatch(Debug Start)");
                    Console.WriteLine($"ReturnAllMatch(Result={result})");
                    foreach (string command in Commands)
                    {
                        Console.WriteLine($"ReturnAllMatch(Command={command})");
                    }
                    Console.WriteLine("ReturnAllMatch(Debug Complete)");
                }

                if (Commands.Count > 0)
                {
                    string currentCommand = Commands.Peek();

                    foreach (CLIVerb verb in verbs)
                    {
                        if (this._Debug)
                            Console.WriteLine($"verb={verb.CompleteName}");

                        if (Commands.Count > 0 && verb.IsMatch(currentCommand))
                        {
                            if (this._Debug)
                                Console.WriteLine($"Match found: {verb.CompleteName}");

                            result.Append($"{verb.CompleteName} ");
                            Commands.Dequeue();

                            if (Commands.Count > 0)
                            {
                                result.Append(ReturnAllMatch(Commands, verb.CLISubVerbs));
                            }
                            else
                            {
                                return result.ToString();
                            }
                        }
                    }

                    if (result.Length == 0)
                    {
                        result.Append(Commands.Dequeue());
                    }
                }

                return result.ToString();
            }

            private bool IsNotReservedWord(bool caseSensative = false)
            {
                bool returnValue = true;
                string compareSource;

                if (caseSensative)
                    compareSource = _builder.ToString().Trim();
                else
                    compareSource = _builder.ToString().Trim().ToLower();

                foreach (string checkIfReserved in ReservedList)
                {
                    if (caseSensative)
                    {
                        if (checkIfReserved == compareSource)
                            returnValue = false;
                    }
                    else
                    {
                        if (checkIfReserved.ToLower() == compareSource)
                            returnValue = false;
                    }
                }

                return returnValue;
            }
            public void PrintHistory()
            {
                if (_CommandList.Count > 0)
                {
                    Console.WriteLine("Previous Entries:");
                    foreach (string printList in this._CommandList)
                    {
                        Console.WriteLine("\t[" + printList + "]");
                    }
                }
            }
            public void PrintCommandTree()
            {
                foreach (CLIVerb printTree in this.AutocompleteTree)
                {
                    Console.WriteLine(printTree.Print());
                }
            }
            public void PrintEverything()
            {
                PrintCommandTree();
                PrintHistory();
            }
            private void ClearCurrentLine()
            {
                int cursorRow = Console.CursorTop;
                int TotalLength = _builder.ToString().Length + this.Prompt.Length + 1;
                int ClearLength = Console.WindowWidth;

                //Console.WriteLine(TotalLength);

                if (TotalLength > Console.WindowWidth)
                {
                    cursorRow--;
                    ClearLength = Console.WindowWidth * 2;
                }

                Console.SetCursorPosition(0, cursorRow);
                Console.Write(new string(' ', ClearLength));
                Console.SetCursorPosition(0, cursorRow);

                SetConsoleColors(ConsoleColors.Prompt);
                Console.Write(this.Prompt);
                SetConsoleColors(ConsoleColors.Input);
                Console.Write(_builder.ToString());

                int x = TotalLength;
                if (x < this.Prompt.Length)
                    x = this.Prompt.Length;

                //Console.SetCursorPosition(_CursorLocation, currentLine);
                //Console.SetCursorPosition(currentColumn, currentLine);
            }
            private int GetLastVerbLength()
            {
                string[] verbs = _builder.ToString().Trim().Split(' ', 3);

                if (verbs.Length > 0)
                {
                    return verbs[^1].Length;
                }
                else
                {
                    return 0;
                }
            }
            private bool NotSingleHelpChar(System.ConsoleKeyInfo testChar)
            {
                return testChar.KeyChar != this.SingleHelpChar;
            }
            private void MoveLeft()
            {
                _CursorRow--;

                if (_CursorRow < this.Prompt.Length)
                    _CursorRow = this.Prompt.Length;

               
            }
            private void MoveRight()
            {
                _CursorRow++;

                if (_CursorRow > _builder.ToString().Length + Prompt.Length)
                    _CursorRow = _builder.ToString().Length + Prompt.Length;
            }
            private void HandleKeyInput(ConsoleKeyInfo keyInput)
            {
                switch (keyInput.Key)
                {
                    case ConsoleKey.Tab:
                        string match = ExtractMatch().Trim();

                        if (match != "")
                        {
                            Console.WriteLine();
                            int xchars = GetLastVerbLength();
                            _builder.Length = (_builder.Length - xchars); //this replaces characters typed at this level of the verb
                            _builder.Append(match + " ");
                            _CursorRow = _builder.Length + Prompt.Length;
                        }

                        ClearCurrentLine();

                        break;

                    case ConsoleKey.Backspace:
                        if (_builder.ToString().Length > 0)
                        {
                            _builder.Remove(_builder.Length - 1, 1);
                        }

                        ClearCurrentLine();

                        break;

                    case ConsoleKey.UpArrow:
                        if (_CommandIndex > 0)
                        {
                            _CommandIndex--;
                            _builder.Clear();
                            _builder.Append(_CommandList[_CommandIndex]);
                            _CursorRow = _builder.Length + Prompt.Length;
                        }

                        ClearCurrentLine();

                        break;

                    case ConsoleKey.DownArrow:
                        if (_CommandIndex < _CommandList.Count - 1)
                        {
                            _CommandIndex++;
                            _builder.Clear();
                            _builder.Append(_CommandList[_CommandIndex]);
                            _CursorRow = _builder.Length + Prompt.Length;
                        }
                        //Console.WriteLine();
                        //Console.WriteLine(_CommandIndex);
                        ClearCurrentLine();
                        //Console.Write(_builder.ToString());

                        break;

                    case ConsoleKey.Escape:
                        _builder.Clear();
                        ClearCurrentLine();

                        break;

                    case ConsoleKey.RightArrow:
                        MoveRight();

                        ClearCurrentLine();

                        break;

                    case ConsoleKey.LeftArrow:
                        MoveLeft();

                        ClearCurrentLine();

                        break;

                    default:
                        // Alter the Builder
                        _builder.Append(keyInput.KeyChar);

                        // Print Reuslts
                        Console.Write(keyInput.KeyChar);
                        break;
                }
            }
            private static bool EnterIsNotThe(ConsoleKeyInfo capturedCharacter)
            {
                return capturedCharacter.Key != ConsoleKey.Enter;
            }
            public void AddVerb(string addVerb)
            {
                CLIVerb simpleVerb = new CLIVerb(addVerb);

                this.AddVerb(simpleVerb);
            }
            public void AddVerb(string newVerb, string newRegex, string newHelp)
            {
                CLIVerb addCLIVerb = new CLIVerb(newVerb, newRegex, newHelp);

                this.AddVerb(addCLIVerb);
            }
            public void AddVerb(CLIVerb addCLIVerb)
            {
                if (addCLIVerb.CompleteName.Length > this._MaxCommandLength) { _MaxCommandLength = addCLIVerb.CompleteName.Length; }

                this.AutocompleteTree.Add(addCLIVerb);
            }
            private bool SetConsoleColors(ForeBack newColors)
            {
                bool Result = false;

                try
                {
                    Console.ForegroundColor = newColors.FG;
                    Console.BackgroundColor = newColors.BG;

                    Result = true;
                }
                catch
                {
                    Result = false;
                }

                return Result;
            }
        }
        ///
        public class CLIVerb : IComparable<CLIVerb>
        {
            public string CompleteName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Usage { get; set; } = "";
            public int GroupID { get; set; } = 10;
            public int OrderID { get; set; } = 1;

            /// <summary>
            /// Regex examples:
            ///   Autocomplete for today:
            ///     ^tod?a?y?$
            ///     If you type at least to and press tab, it will autocomplete to today
            /// Simple way to use AutoComplete is just put the first couple of letters in the same Regex property such as: to
            /// </summary>
            public string Regex { get; set; } = "";
            public string Help { get; set; } = "";
            public string Description { get; set; } = "";
            private int DebugCount = 0;
            private string DebugLog = "";
            public List<CLIVerb> CLISubVerbs { get; set; } = new List<CLIVerb>();
            
            public CLIVerb(string fullName, string regex, string help, List<CLIVerb> subverbs, string displayname = "")
            {
                CompleteName = fullName;
                Regex = regex;
                Help = help;
                CLISubVerbs = subverbs;
                DisplayName = displayname;
                AddDebug("Initialized with completename,regex,help,subverbs");
            }

            public CLIVerb(string completename, string regex, string help, CLIVerb subverb, string displayname = "")
            {
                CompleteName = completename;
                Regex = regex;
                Help = help;
                DisplayName = displayname;
                CLISubVerbs.Add(subverb);
                AddDebug("Initialized with completename,regex,help,subverb");
            }

            public CLIVerb(string completename, string regex, string help, string displayname = "")
            {
                CompleteName = completename;
                Regex = regex;
                Help = help;
                if (displayname == "")
                    DisplayName = completename;
                else
                    DisplayName = displayname;
                AddDebug("Initialized with completename,regex,help");
            }

            public CLIVerb(string completename, string regex, string displayname = "")
            {
                CompleteName = completename;
                if (displayname == "")
                    DisplayName = completename;
                else
                    DisplayName = displayname;
                Regex = regex;
                AddDebug("Initialized with completename,regex");
            }

            public CLIVerb(string completename, string regex, string help, int OrderID, int GroupID = 10)
            {
                CompleteName = completename;
                DisplayName = completename;
                Help = help;
                Regex = regex;
                Usage = completename;
                this.OrderID = OrderID;
                this.GroupID = GroupID;
                AddDebug("Initialized with completename,regex,help,orderid,groupid");
            }

            public CLIVerb(string completename, string regex, string help, string usage, int OrderID, int GroupID = 10)
            {
                CompleteName = completename;
                DisplayName = completename;
                Help = help;
                Regex = regex;
                Usage = usage;
                this.OrderID = OrderID;
                this.GroupID = GroupID;
                AddDebug("Initialized with completename,regex,help,usage,orderid,groupid"); ;
            }

            public CLIVerb(string completename, string displayname = "")
            {
                CompleteName = completename;
                AddDebug("Initialized with completename only");
            }

            public CLIVerb()
            {
                AddDebug("Initialized with no parameters");
            }

            private void AddDebug(string Message)
            {
                this.DebugLog += Message + "\r\n";
            }

            private void AddDebug(int AddCount)
            {
                this.DebugCount += AddCount;
            }

            public bool SetRegex(string newRegex)
            {
                bool ReturnValue = true;

                try
                {
                    Regex _regex = new Regex(newRegex, RegexOptions.IgnoreCase); //if the regex match string is invalid, this will throw an error
                    this.Regex = newRegex;
                }
                catch
                {
                    ReturnValue = false;
                }

                return ReturnValue;
            }

            public int GetDebugCount()
            {
                return this.DebugCount;
            }

            public string GetDebugLog()
            {
                return this.DebugLog;
            }

            public string Print(int index = 0)
            {
                string ReturnValue = "";

                AddDebug(this.CompleteName + ":" + index);
                string numTab = new string(' ', index);

                if (CompleteName == "" || CompleteName == "<r/>")
                {
                    ReturnValue = numTab + this.DisplayName + "\r\n";
                }
                else
                {
                    ReturnValue = numTab + this.CompleteName + "\r\n";
                }

                index++;

                if (CLISubVerbs.Count > 0)
                {
                    foreach (CLIVerb cliSubVerb in CLISubVerbs)
                    {
                        ReturnValue += cliSubVerb.Print(index);
                    }
                }

                return ReturnValue;
            }

            public bool IsMatch(string search)
            {
                Regex findMatch = new Regex(this.Regex, RegexOptions.IgnoreCase);
                if (findMatch.Match(search).Success)
                {
                    return true;
                }

                return false;
            }

            public void AddSubVerb(string newSubVerb, string newDisplayName = "")
            {
                CLIVerb addVerb = new CLIVerb(completename: newSubVerb, displayname: newDisplayName);
                CLISubVerbs.Add(addVerb);
            }

            public void AddSubVerb(string newSubVerb, string newRegex, string newDisplayName = "")
            {
                CLIVerb addVerb = new CLIVerb(completename: newSubVerb, regex: newRegex, displayname: newDisplayName);
                CLISubVerbs.Add(addVerb);
            }

            public void AddSubVerb(string newSubVerb, string newRegex, string newHelp, string newDisplayName = "")
            {
                CLIVerb addVerb = new CLIVerb(completename: newSubVerb, regex: newRegex, help: newHelp, displayname: newDisplayName);
                CLISubVerbs.Add(addVerb);
            }

            public void AddSubVerb(string newSubVerb, string newRegex, string newHelp, CLIVerb subVerb, string newDisplayName = "")
            {
                CLIVerb addVerb = new CLIVerb(completename: newSubVerb, regex: newRegex, help: newHelp, subverb: subVerb, displayname: newDisplayName);
                CLISubVerbs.Add(addVerb);
            }

            public void AddSubVerb(CLIVerb newSubVerb)
            {
                CLISubVerbs.Add(newSubVerb);
            }

            public int CompareTo(CLIVerb other)
            {
                if (other == null) return 1;

                int groupComparison = this.GroupID.CompareTo(other.GroupID);
                if (groupComparison != 0)
                {
                    return groupComparison;
                }
                else
                {
                    return this.OrderID.CompareTo(other.OrderID);
                }
            }

            /*
            public bool FindVerbMatch(string search)
            {
                bool returnValue = false;  //default to false

                //Console.WriteLine();
                //Console.WriteLine(this.Verb);

                switch (this.FullName)
                {
                    case string test when test.StartsWith("<r/>"):

                        //Console.Beep();

                        //Console.WriteLine();
                        //Console.WriteLine("Matching on regex");
                        string pattern = this.FullName[4..];
                        //Console.WriteLine(pattern);
                        Regex findMatch = new Regex(pattern, RegexOptions.IgnoreCase);
                        if (findMatch.Match(search).Success)
                        {
                            returnValue = true;
                        }

                        break;

                    case string test when test.StartsWith(search.Trim(), StringComparison.CurrentCultureIgnoreCase):

                        returnValue = true;

                        break;
                }

                //Startswith will match on Empty string due to every string starts with empty. This should always be false.
                if (search == String.Empty)
                    returnValue = false;

                return returnValue;
            }
            */
        }
    }
}