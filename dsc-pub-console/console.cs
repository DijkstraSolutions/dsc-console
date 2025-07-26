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
        public enum DebugLevel { None, Error, Warning, Informational, Everything };
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
            /// <summary>
            /// List of keys to ignore during input handling.
            /// </summary>
            public List<ConsoleKey> IgnoreKeys = new List<ConsoleKey>();
            /// <summary>
            /// Color configuration for console elements.
            /// </summary>
            public Colors ConsoleColors { get; set; } = new Colors();
            /// <summary>
            /// Command tree for autocomplete suggestions.
            /// </summary>
            public List<CLIWord> AutocompleteTree { get; set; } = new List<CLIWord>();
            /// <summary>
            /// Returns the trimmed string currently in the buffer.
            /// </summary>
            public string LastCaptured
            {
                get
                {
                    return this._builder.ToString().Trim();
                }
            }

            private int _InputOriginLeft = 0;
            private int _InputOriginTop = 0;
            private int _LastRenderLength = 0;

            /// <summary>
            /// Intended to expand all Words even if not completed by user input
            /// Any unrecognized input is untouched
            /// </summary>
            public string LastCompleteWordPhrase
            {
                get
                {
                    Queue<string> commandQueue = new Queue<string>(LastCaptured.Trim().Split(' '));
                    return ReturnAllMatch(commandQueue, AutocompleteTree).Trim();
                }
            }
            /// <summary>
            /// Prefix string to be displayed before user input.
            /// </summary>
            public string Prompt = "";
            /// <summary>
            /// Default Help Char is ?, you can change this
            /// </summary>
            public char SingleHelpChar = '?';
            /// <summary>
            /// List of reserved words that should not be added to command history.
            /// </summary>
            public List<string> ReservedList = new List<string>();

            private StringBuilder _builder = new StringBuilder();

            private List<string> _CommandList = new List<string>();
            /// <summary>
            /// Captures diagnostic or debug messages during input lifecycle.
            /// </summary
            public List<string> Messages { get; set; } = new List<string>();
            /// <summary>
            /// Gets the maximum command length encountered.
            /// </summary>
            public int MaxCommandLength => _MaxCommandLength;

            private int _CommandIndex = 0;
            private int _CursorRow = Console.CursorTop;
            private int _CursorColumn = Console.CursorLeft;
            private int _InsertAt = 0;
            private int _Position = 0;
            private int _BuilderIndex = 0;  // enable left/right arrow movement in the input line
            /// <summary>
            /// Wordosity level for debug information.
            /// </summary>
            public DebugLevel Debug { get; set; } = DebugLevel.Error;

            /// <summary>
            /// If true, displays the cursor position while typing.
            /// </summary>
            public bool DebugPosition = false;
            private int _MaxCommandLength = 0;
            /// <summary>
            /// Displays the current cursor position at the top of the console.
            /// </summary>
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
            /// <summary>
            /// Starts the interactive console interface with optional initial input buffer and debug mode.
            /// </summary>
            /// <param name="SetBuffer">Initial buffer content.</param>
            /// <param name="noCRLF">Suppresses the final newline if true.</param>
            /// <param name="debug">Sets the debug Wordosity level.</param>
            /// <returns>True if the console interaction completes successfully; false on exception.</returns>
            public bool StartAutoConsole(string SetBuffer = "", bool noCRLF = false, DebugLevel debug = DebugLevel.Error)
            {
                Debug = debug;
                //Debug = true;

                (_InputOriginLeft, _InputOriginTop) = Console.GetCursorPosition();

                this.IgnoreKeys.Add(ConsoleKey.F15); //ignore Caffine tool

                try
                {
                    ConsoleColors.Main.FG = Console.ForegroundColor;
                    ConsoleColors.Main.BG = Console.BackgroundColor;

                    _builder = new StringBuilder(SetBuffer);
                    _BuilderIndex = _builder.Length; //enable left/right arrow movement in the input line

                    SetConsoleColors(ConsoleColors.Main);

                    if (Debug > DebugLevel.Warning)
                        Console.WriteLine("Help Character is " + this.SingleHelpChar.ToString());

                    _CursorRow = Console.CursorTop;
                    _CursorColumn = Console.CursorLeft;

                    _Position = this.Prompt.Length + _builder.Length;

                    if (DebugPosition)
                    {
                        //advance the line to leave room for the counter
                        Console.WriteLine();
                    }

                    int savedLeft = Console.CursorLeft;
                    int savedTop = Console.CursorTop;

                    SetConsoleColors(ConsoleColors.Prompt);

                    Console.Write($"{Prompt}");

                    _InputOriginLeft = savedLeft;
                    _InputOriginTop = savedTop;

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

                    // Final redraw must show the entire buffer regardless of cursor position
                    int originalIndex = _BuilderIndex;
                    _BuilderIndex = _builder.Length;
                    ClearCurrentLine();           // Draw full line even if cursor was mid-buffer
                    _BuilderIndex = originalIndex;

                    if (Debug > DebugLevel.Informational)
                    {
                        Messages.Add($"[DBG] Final buffer snapshot before Enter: \"{_builder}\"");
                        Messages.Add($"[DBG] Cursor moved to input end: offset={_builder.Length}, totalPrompted={Prompt.Length + _builder.Length}");
                    }

                    //MoveCursorToInputEnd();       // Put cursor at end of true input
                    SetCursorToCurrentIndex();
                    Console.WriteLine();          // Newline
                    FlushFinalLine();             // Clear residue below if any

                    // - fixed consecutive history dupllicates ignored
                    string currentCommand = _builder.ToString()
                        .Replace("\r", "")  // strip carriage returns
                        .Replace("\n", "");

                    _builder.Clear();
                    _builder.Append(currentCommand);
                    _BuilderIndex = _builder.Length;

                    if (currentCommand.Length != 0 && IsNotReservedWord() && NotSingleHelpChar(capturedCharacter))
                    {
                        bool isDuplicateOfLast = _CommandList.Count > 0 && _CommandList[^1] == currentCommand;

                        if (!isDuplicateOfLast)
                        {
                            _CommandList.Add(currentCommand);
                        }

                        _InsertAt = _builder.Length;
                    }

                    if (Debug > DebugLevel.Informational)
                    {
                        SetConsoleColors(ConsoleColors.Debug);
                        Console.WriteLine();
                        Console.Write("<echo>" + _builder.ToString().Trim() + "</echo><length>" + _builder.ToString().Length + "</length>");
                        SetConsoleColors(ConsoleColors.Input);
                    }
                    this._CommandIndex = _CommandList.Count;

                    if (!noCRLF)
                        Console.WriteLine();

                    if (Debug > DebugLevel.Informational)
                    {
                        foreach (var rootWord in AutocompleteTree)
                            CollectDebugLogs(rootWord);
                    }

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
            /// <summary>
            /// Attempts to find a matching autocomplete term from the current buffer.
            /// </summary>
            private string ExtractMatch() //need to address multiple matches
            {
                //using the entire string, find the last match, search by space sep
                string[] matches = _builder.ToString().Split(' ', 3);
                bool foundMatch = false;

                List<CLIWord> SearchWords = new List<CLIWord>();
                SearchWords.AddRange(AutocompleteTree);

                if (_builder.Length > 0)
                {
                    if (matches.Length > 0)
                    {
                        int lastMatch = matches.Length - 1;

                        for (int i = 0; i < matches.Length; i++)
                        {
                            foreach (CLIWord eachWord in SearchWords)
                            {
                                if (eachWord.IsMatch(matches[i]) && i != lastMatch)
                                {
                                    foundMatch = true;
                                    SearchWords = new List<CLIWord>();
                                    SearchWords.AddRange(eachWord.CLISubWords);
                                    break;
                                }

                                if (eachWord.IsMatch(matches[i]) && i == lastMatch)
                                {
                                    foundMatch = true;

                                    if (Debug >= DebugLevel.Informational)
                                    {
                                        Messages.Add($"[DBG] Word matched: {eachWord.CompleteName}");
                                        Messages.AddRange(eachWord.GetDebugLog());
                                    }

                                    if (eachWord.CompleteName.StartsWith("<r/>")) // if CompleteName starts with <r/> return what matched.
                                        return matches[i]; //matched value return, task: allow to use group value matches combined with inline static values
                                    else
                                        return eachWord.CompleteName; //return static CompleteName
                                }
                            }

                            if (!foundMatch)
                                break;
                        }
                    }
                }

                return ("");
            }
            /// <summary>
            /// Recursively builds a complete command string from the autocomplete tree.
            /// </summary>
            private string ReturnAllMatch(Queue<string> Commands, List<CLIWord> Words)
            {
                StringBuilder result = new StringBuilder();

                if (Debug > DebugLevel.Warning)
                {
                    foreach (string command in Commands)
                    {
                        Console.WriteLine($"ReturnAllMatch(Command={command})");
                    }
                }

                if (Commands.Count > 0)
                {
                    string currentCommand = Commands.Peek();

                    foreach (CLIWord Word in Words)
                    {
                        if (Debug > DebugLevel.Warning)
                            Console.WriteLine($"Word={Word.CompleteName}");

                        if (Commands.Count > 0 && Word.IsMatch(currentCommand))
                        {
                            if (Debug > DebugLevel.Warning)
                                Console.WriteLine($"Match found: {Word.CompleteName}");

                            if (Debug >= DebugLevel.Informational)
                            {
                                Messages.Add($"[DBG] Matched {Word.CompleteName} from \"{currentCommand}\"");
                                Messages.AddRange(Word.GetDebugLog());
                            }

                            if (Word.CompleteName.StartsWith("<r/>"))
                                result.Append($"{currentCommand} ");
                            else
                                result.Append($"{Word.CompleteName} ");

                            Commands.Dequeue();

                            if (Commands.Count > 0)
                                result.Append(ReturnAllMatch(Commands, Word.CLISubWords));
                            else
                                return result.ToString();
                        }
                    }

                    if (result.Length == 0)
                    {
                        //return anything else that might be remaining
                        while (Commands.Count > 0)
                        {
                            result.Append(Commands.Dequeue());
                            if (Commands.Count > 0)
                            {
                                result.Append(" "); // Add a space between words
                            }
                        }
                    }
                }

                return result.ToString();
            }
            /// <summary>
            /// Checks whether the current buffer is not in the reserved list.
            /// </summary>
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
            
            private void CollectDebugLogs(CLIWord Word)
            {
                var logs = Word.GetDebugLog();
                foreach (var line in logs)
                {
                    Messages.Add($"[CLIWord Debug: {Word.CompleteName}] {line}");
                }

                foreach (var subWord in Word.CLISubWords)
                {
                    CollectDebugLogs(subWord);
                }
            }

            /// <summary>
            /// Prints the command history collected during the session.
            /// </summary>
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
            /// <summary>
            /// Prints the current autocomplete command tree.
            /// </summary>
            public void PrintCommandTree()
            {
                foreach (CLIWord printTree in this.AutocompleteTree)
                {
                    Console.WriteLine(printTree.Print());
                }
            }
            /// <summary>
            /// Prints both command tree and history.
            /// </summary>
            public void PrintEverything()
            {
                PrintCommandTree();
                PrintHistory();
            }
            /// <summary>
            /// Clears the current console line and redraws the prompt and buffer.
            /// </summary>
            //private void ClearCurrentLine()
            //{
            //    int windowWidth = Console.WindowWidth;
            //    int promptLength = this.Prompt.Length;
            //    int inputLength = _builder.Length;
            //    int totalLength = promptLength + inputLength;

            //    int previousLength = _LastRenderLength;
            //    _LastRenderLength = totalLength;

            //    // Total console columns used previously vs now
            //    int previousLineCount = (previousLength + _InputOriginLeft + windowWidth - 1) / windowWidth;
            //    int currentLineCount = (totalLength + _InputOriginLeft + windowWidth - 1) / windowWidth;
            //    int maxLines = Math.Max(previousLineCount, currentLineCount);

            //    if (Debug > DebugLevel.Informational)
            //    {
            //        Messages.Add($"[DBG] Redrawing input line. Prompt=\"{Prompt}\", Buffer=\"{_builder}\"");
            //        Messages.Add($"[DBG] Cursor offset={_BuilderIndex}, RenderWidth={_LastRenderLength}, WindowWidth={Console.WindowWidth}");
            //        Messages.Add($"[DBG] InputOriginTop={_InputOriginTop}, Console.Top={Console.CursorTop}");
            //    }

            //    // Clear all rows we might have used
            //    for (int i = 0; i < maxLines; i++)
            //    {
            //        int row = _InputOriginTop + i;
            //        if (row >= Console.BufferHeight) break;

            //        Console.SetCursorPosition(0, row);
            //        Console.Write(new string(' ', windowWidth));
            //    }

            //    // Reset to input start
            //    Console.SetCursorPosition(_InputOriginLeft, _InputOriginTop);

            //    // Redraw prompt
            //    SetConsoleColors(ConsoleColors.Prompt);
            //    Console.Write(this.Prompt);

            //    if (Debug > DebugLevel.Informational)
            //    {
            //        var visualized = new StringBuilder();
            //        foreach (char c in _builder.ToString())
            //        {
            //            if (c == '\r') visualized.Append("<CR>");
            //            else if (c == '\n') visualized.Append("<LF>");
            //            else visualized.Append(c);
            //        }
            //        Messages.Add($"[DBG] Visual buffer = [{visualized}]");
            //    }

            //    // Redraw buffer
            //    SetConsoleColors(ConsoleColors.Input);
            //    Console.Write(_builder.ToString());

            //    // Erase trailing characters from longer previous render
            //    int leftover = previousLength - totalLength;
            //    if (leftover > 0)
            //    {
            //        Console.Write(new string(' ', leftover));
            //    }

            //    // Update cursor position
            //    SetCursorToCurrentIndex();

            //    // Detect if buffer scrolled
            //    var (actualLeft, actualTop) = Console.GetCursorPosition();
            //    if (actualTop > _InputOriginTop + 1)
            //    {
            //        int delta = actualTop - (_InputOriginTop + 1);
            //        _InputOriginTop += delta;

            //        if (Debug > DebugLevel.Informational)
            //        {
            //            Messages.Add($"[DBG] Console auto-scrolled. Adjusting _InputOriginTop by {delta} → now {_InputOriginTop}");
            //        }
            //    }

            //    AdjustForConsoleScroll();

            //    // DEBUG: show buffer and tracking info (only if Debug = true)
            //    if (Debug > DebugLevel.Informational)
            //    {
            //        try
            //        {
            //            int debugRow = Console.CursorTop;
            //            int debugLeft = 0;

            //            Console.SetCursorPosition(debugLeft, debugRow + 1);
            //            SetConsoleColors(ConsoleColors.Debug);
            //            Messages.Add($"[DBG] builder.Length={_builder.Length}, _LastRenderLength={_LastRenderLength}, prompt={promptLength}, offset={_BuilderIndex}");
            //            SetConsoleColors(ConsoleColors.Input);
            //            //Console.SetCursorPosition(finalLeft, finalTop);
            //        }
            //        catch (Exception debugEx)
            //        {
            //            SetConsoleColors(ConsoleColors.Error);
            //            Console.SetCursorPosition(0, Console.CursorTop);
            //            Messages.Add($"[DBG ERROR] {debugEx.Message}");
            //            SetConsoleColors(ConsoleColors.Main);
            //        }
            //    }

            //}

            private void ClearCurrentLine()
            {
                int windowWidth = Console.WindowWidth;
                int promptLength = this.Prompt.Length;
                int inputLength = _builder.Length;
                int totalLength = promptLength + inputLength;

                int previousLength = _LastRenderLength;
                _LastRenderLength = totalLength;

                int previousLineCount = (previousLength + _InputOriginLeft + windowWidth - 1) / windowWidth;
                int currentLineCount = (totalLength + _InputOriginLeft + windowWidth - 1) / windowWidth;
                int maxLines = Math.Max(previousLineCount, currentLineCount);

                if (Debug > DebugLevel.Informational)
                {
                    Messages.Add($"[DBG] Redrawing input line. Prompt=\"{Prompt}\", Buffer=\"{_builder}\"");
                    Messages.Add($"[DBG] Cursor offset={_BuilderIndex}, RenderWidth={_LastRenderLength}, WindowWidth={windowWidth}");
                    Messages.Add($"[DBG] InputOriginTop={_InputOriginTop}, Console.Top={Console.CursorTop}");
                }

                // Clear all rows we might have used, with clamps
                for (int i = 0; i < maxLines; i++)
                {
                    int row = _InputOriginTop + i;
                    if (row < 0) continue;
                    if (row >= Console.BufferHeight) break;

                    Console.SetCursorPosition(0, row);
                    Console.Write(new string(' ', windowWidth));
                }

                // Reset to input start, clamped
                Console.SetCursorPosition(_InputOriginLeft, Math.Max(0, _InputOriginTop));

                // Redraw prompt
                SetConsoleColors(ConsoleColors.Prompt);
                Console.Write(this.Prompt);

                if (Debug > DebugLevel.Informational)
                {
                    var visualized = new StringBuilder();
                    foreach (char c in _builder.ToString())
                    {
                        if (c == '\r') visualized.Append("<CR>");
                        else if (c == '\n') visualized.Append("<LF>");
                        else visualized.Append(c);
                    }
                    Messages.Add($"[DBG] Visual buffer = [{visualized}]");
                }

                // Redraw buffer
                SetConsoleColors(ConsoleColors.Input);
                Console.Write(_builder.ToString());

                // Adjust for any scroll that occurred during the write
                AdjustForConsoleScroll();

                // Erase trailing characters from longer previous render
                int leftover = previousLength - totalLength;
                if (leftover > 0)
                {
                    Console.Write(new string(' ', leftover));
                }

                // Update cursor position
                SetCursorToCurrentIndex();

                if (Debug > DebugLevel.Informational)
                {
                    var (x, y) = Console.GetCursorPosition();
                    Messages.Add($"[DBG] Cursor updated: offset={promptLength + _BuilderIndex}, top={y}, left={x}");
                }

                if (Debug >= DebugLevel.Everything)
                {
                    var (x, y) = Console.GetCursorPosition();
                    Console.SetCursorPosition(x, y);
                    Console.Write('█'); // cursor marker for extreme debugging
                    Console.SetCursorPosition(x, y); // reset
                }
            }

            private void AdjustForConsoleScroll()
            {
                var (_, actualTop) = Console.GetCursorPosition();

                // Calculate expected cursor top assuming no scroll (at end of input)
                int offset = this.Prompt.Length + _builder.Length;
                int expectedCursorTop = _InputOriginTop + (_InputOriginLeft + offset - 1) / Console.WindowWidth;

                if (actualTop < expectedCursorTop)
                {
                    int delta = expectedCursorTop - actualTop;
                    _InputOriginTop -= delta;
                    _InputOriginTop = Math.Max(0, _InputOriginTop); // Clamp to prevent negative

                    if (Debug > DebugLevel.Informational)
                    {
                        Messages.Add($"[DBG] Console auto-scrolled up. ExpectedTop={expectedCursorTop}, ActualTop={actualTop}, Delta={delta}, New OriginTop={_InputOriginTop}");
                    }
                }
                else if (Debug > DebugLevel.Informational && actualTop != expectedCursorTop)
                {
                    Messages.Add($"[DBG] No scroll detected. ExpectedTop={expectedCursorTop}, ActualTop={actualTop}");
                }
            }

            private void SetCursorToCurrentIndex()
            {
                int promptLength = this.Prompt.Length;
                int cursorOffset = promptLength + _BuilderIndex;
                int rowIncrement = (_InputOriginLeft + cursorOffset - 1) / Console.WindowWidth;
                int finalTop = Math.Max(0, Math.Min(_InputOriginTop + rowIncrement, Console.BufferHeight - 1));
                int finalLeft = (_InputOriginLeft + cursorOffset) % Console.WindowWidth;

                Console.SetCursorPosition(finalLeft, finalTop);

                if (Debug > DebugLevel.Informational)
                {
                    Messages.Add($"[DBG] Cursor updated: offset={cursorOffset}, top={finalTop}, left={finalLeft}");
                }
            }

            private void FlushFinalLine()
            {
                int promptLength = Prompt.Length;
                int totalInputLength = promptLength + _builder.Length;
                int targetTop = _InputOriginTop + (_InputOriginLeft + totalInputLength - 1) / Console.WindowWidth;
                int flushLine = Math.Max(0, Math.Min(targetTop + 1, Console.BufferHeight - 1));

                Console.SetCursorPosition(0, flushLine);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, flushLine); // back to start of cleared line
                _LastRenderLength = 0;
            }

            /// <summary>
            /// Returns the length of the last word in the buffer.
            /// </summary>
            private int GetLastWordLength()
            {
                string[] Words = _builder.ToString().Trim().Split(' ', 3);

                if (Words.Length > 0)
                {
                    return Words[^1].Length;
                }
                else
                {
                    return 0;
                }
            }
            /// <summary>
            /// Determines whether a given key is not the single-character help trigger.
            /// </summary>
            private bool NotSingleHelpChar(System.ConsoleKeyInfo testChar)
            {
                return testChar.KeyChar != this.SingleHelpChar;
            }
            /// <summary>
            /// Moves the internal cursor one position to the left.
            /// </summary>
            private void MoveLeft()
            {
                //enable left/right arrow movement in the input line:
                if (_BuilderIndex > 0)
                    _BuilderIndex--;               
            }
            /// <summary>
            /// Moves the internal cursor one position to the right.
            /// </summary>
            private void MoveRight()
            {
                //_CursorRow++;

                //if (_CursorRow > _builder.ToString().Length + Prompt.Length)
                //    _CursorRow = _builder.ToString().Length + Prompt.Length;

                //enable left/right arrow movement in the input line:
                if (_BuilderIndex < _builder.Length)
                    _BuilderIndex++;
            }

            /// <summary>
            /// Processes a key input event and updates the buffer accordingly.
            /// </summary>
            private void HandleKeyInput(ConsoleKeyInfo keyInput)
            {
                switch (keyInput.Key)
                {
                    case ConsoleKey.Tab:
                        string match = ExtractMatch().Trim();

                        if (match != "")
                        {
                            Console.WriteLine();
                            int xchars = GetLastWordLength();
                            _builder.Length = (_builder.Length - xchars); //this replaces characters typed at this level of the Word
                            _builder.Append(match + " ");
                            _BuilderIndex = _builder.Length; //enable left/right arrow movement in the input line
                            _CursorRow = _builder.Length + Prompt.Length;
                        }

                        ClearCurrentLine();

                        break;

                    case ConsoleKey.Backspace:
                        if (_BuilderIndex > 0 && _builder.Length > 0)
                        {
                            _builder.Remove(_BuilderIndex - 1, 1);
                            _BuilderIndex--;
                            ClearCurrentLine();
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        if (_CommandIndex > 0)
                        {
                            _CommandIndex--;
                            _builder.Clear();
                            _BuilderIndex = _builder.Length; //enable left/right arrow movement in the input line
                            _builder.Append(_CommandList[_CommandIndex]);
                            _BuilderIndex = _builder.Length; //enable left/right arrow movement in the input line
                            _CursorRow = _builder.Length + Prompt.Length;

                            Console.SetCursorPosition(_InputOriginLeft, _InputOriginTop);
                        }

                        ClearCurrentLine();

                        break;

                    case ConsoleKey.DownArrow:
                        if (_CommandIndex < _CommandList.Count - 1)
                        {
                            _CommandIndex++;
                            _builder.Clear(); //enable left/right arrow movement in the input line
                            _BuilderIndex = _builder.Length;
                            _builder.Append(_CommandList[_CommandIndex]);
                            _BuilderIndex = _builder.Length; //enable left/right arrow movement in the input line
                            _CursorRow = _builder.Length + Prompt.Length;
                        }

                        ClearCurrentLine();
                        break;

                    case ConsoleKey.Escape:
                        _builder.Clear();
                        _BuilderIndex = _builder.Length; //enable left/right arrow movement in the input line
                        ClearCurrentLine();

                        break;
                    case ConsoleKey.Home:
                        _BuilderIndex = 0;
                        ClearCurrentLine();
                        break;

                    case ConsoleKey.End:
                        _BuilderIndex = _builder.Length;
                        ClearCurrentLine();
                        break;
                    case ConsoleKey.Delete:
                        if (_BuilderIndex < _builder.Length)
                        {
                            _builder.Remove(_BuilderIndex, 1);
                            ClearCurrentLine();
                        }
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
                        bool AddCharacter = true;

                        foreach (ConsoleKey checkIgnore in IgnoreKeys)
                        {
                            if(checkIgnore == keyInput.Key)
                            {
                                AddCharacter = false;
                            }
                        }

                        if (AddCharacter)
                        {
                            if (char.IsControl(keyInput.KeyChar) && keyInput.KeyChar != '\t')
                            {
                                if (Debug > DebugLevel.Informational)
                                    Messages.Add($"[DBG] Ignored control character: 0x{(int)keyInput.KeyChar:X2}");
                                break;
                            }
                            // enable left/right arrow movement in the input line:
                            _builder.Insert(_BuilderIndex, keyInput.KeyChar);
                            _BuilderIndex++;
                            ClearCurrentLine();
                        }
                        break;
                }
                
                if (keyInput.Key != ConsoleKey.Tab)
                    SetCursorToCurrentIndex();
            }
            /// <summary>
            /// Checks if the captured character is not Enter.
            /// </summary>
            private static bool EnterIsNotThe(ConsoleKeyInfo capturedCharacter)
            {
                return capturedCharacter.Key != ConsoleKey.Enter;
            }
            /// <summary>
            /// Adds a simple Word to the autocomplete tree.
            /// </summary>
            public void AddWord(string addWord)
            {
                CLIWord simpleWord = new CLIWord(addWord);

                this.AddWord(simpleWord);
            }
            /// <summary>
            /// Adds a new Word with regex and help text to the autocomplete tree.
            /// </summary>
            public void AddWord(string newWord, string newRegex, string newHelp)
            {
                CLIWord addCLIWord = new CLIWord(newWord, newRegex, newHelp);

                this.AddWord(addCLIWord);
            }
            /// <summary>
            /// Adds a custom CLIWord to the autocomplete tree.
            /// </summary>
            public void AddWord(CLIWord addCLIWord)
            {
                if (addCLIWord.CompleteName.Length > this._MaxCommandLength) { _MaxCommandLength = addCLIWord.CompleteName.Length; }

                this.AutocompleteTree.Add(addCLIWord);
            }
            /// <summary>
            /// Sets the console's foreground and background colors.
            /// </summary>
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
        /// <summary>
        /// Represents a command-line Word that supports autocomplete, help, and hierarchical subcommands.
        /// </summary>
        public class CLIWord : IComparable<CLIWord>
        {
            /// <summary>
            /// Full name of the command Word (e.g., "start", "run").
            /// </summary>
            public string CompleteName { get; set; } = "";
            /// <summary>Name shown in menus or help text. Defaults to <see cref="CompleteName"/> if unset.</summary>
            public string DisplayName { get; set; } = "";
            /// <summary>Example usage of the command, shown in help outputs.</summary>
            public string Usage { get; set; } = "";
            /// <summary>Group identifier used to cluster related Words in output or sorting.</summary>
            public int GroupID { get; set; } = 10;
            /// <summary>Relative order within its group for display or sorting.</summary>
            public int OrderID { get; set; } = 1;
            /// <summary>
            /// Regex examples:
            ///   Autocomplete for today:
            ///     ^tod?a?y?$
            ///     If you type at least to and press tab, it will autocomplete to today
            /// Simple way to use AutoComplete is just put the first couple of letters in the same Regex property such as: to
            /// </summary>
            public string Regex { get; set; } = "";
            /// <summary>Help string describing what this Word does.</summary>
            public string Help { get; set; } = "";
            /// <summary>Long-form description of the Word, potentially multiline.</summary>
            public string Description { get; set; } = "";
            /// <summary>Whether this Word should be hidden from help and autocomplete lists.</summary>
            public bool Hidden { get; set; } = false;
            //private int DebugCount = 0;
            private List<string> DebugLog = new List<string>();
            /// <summary>List of sub-Words that are children of this command.</summary>
            public List<CLIWord> CLISubWords { get; set; } = new List<CLIWord>();
            /// <summary>
            /// Initializes a CLIWord with full parameters and subWord list.
            /// </summary>
            /// <param name="fullName">The full command name.</param>
            /// <param name="regex">Regex pattern for matching input.</param>
            /// <param name="help">Help description.</param>
            /// <param name="subWords">Subcommands of this Word.</param>
            /// <param name="displayname">Optional display name override.</param>
            public CLIWord(string fullName, string regex, string help, List<CLIWord> subWords, string displayname = "")
            {
                CompleteName = fullName;
                Regex = regex;
                Help = help;
                CLISubWords = subWords;
                DisplayName = displayname;
                AddDebug("Initialized with completename,regex,help,subWords");
            }
            /// <summary>
            /// Initializes a CLIWord with one subWord.
            /// </summary>
            /// <param name="completename">The full command name.</param>
            /// <param name="regex">Regex pattern for matching input.</param>
            /// <param name="help">Help description.</param>
            /// <param name="subWord">Single subcommand.</param>
            /// <param name="displayname">Optional display name override.</param>
            public CLIWord(string completename, string regex, string help, CLIWord subWord, string displayname = "")
            {
                CompleteName = completename;
                Regex = regex;
                Help = help;
                DisplayName = displayname;
                CLISubWords.Add(subWord);
                AddDebug("Initialized with completename,regex,help,subWord");
            }
            /// <summary>
            /// Initializes a CLIWord with name, regex, help, and optional display name.
            /// </summary>
            /// <param name="completename">The full command name.</param>
            /// <param name="regex">Regex pattern for matching input.</param>
            /// <param name="help">Help description.</param>
            /// <param name="displayname">Optional display name override.</param>
            public CLIWord(string completename, string regex, string help, string displayname = "")
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
            /// <summary>
            /// Initializes a CLIWord with name and regex only, display name optional.
            /// </summary>
            /// <param name="completename">The full command name.</param>
            /// <param name="regex">Regex pattern for matching input.</param>
            /// <param name="displayname">Optional display name override.</param>
            public CLIWord(string completename, string regex, string displayname = "")
            {
                CompleteName = completename;
                if (displayname == "")
                    DisplayName = completename;
                else
                    DisplayName = displayname;
                Regex = regex;
                AddDebug("Initialized with completename,regex");
            }
            public CLIWord(string completename, string regex, string help, int OrderID, string displayname = "", bool Hidden = false, int GroupID = 10)
            {
                CompleteName = completename;
                Regex = regex;
                Help = help;
                if (displayname == "")
                    DisplayName = completename;
                else
                    DisplayName = displayname;
                this.OrderID = OrderID;
                this.GroupID = GroupID;
                this.Hidden = Hidden;
                AddDebug("Initialized completename, regex, help, OrderID, displayname, Hidden, GroupID");
            }
            //public CLIWord(string completename, int RegexStartMatchIndex, string help, int OrderID, string displayname = "", bool Hidden = false, int GroupID = 10)
            //{
            //    CompleteName = completename;
            //    Regex = BuildRegexWithIndex(completename, RegexStartMatchIndex);
            //    Help = help;
            //    if (displayname == "")
            //        DisplayName = completename;
            //    else
            //        DisplayName = displayname;
            //    this.OrderID = OrderID;
            //    this.GroupID = GroupID;
            //    this.Hidden = Hidden;
            //    AddDebug("Initialized completename, RegestStartMatchIndex, help, OrderID, displayname, Hidden, GroupID");
            //}
            public CLIWord(string completename, int RegexStartMatchIndex, string help, int OrderID, string AdditionalRegex = "", string displayname = "", bool Hidden = false, int GroupID = 10)
            {
                CompleteName = completename;
                Regex = string.IsNullOrWhiteSpace(AdditionalRegex)
                    ? BuildRegexWithIndex(completename, RegexStartMatchIndex)
                    : $"{BuildRegexWithIndex(completename, RegexStartMatchIndex)}|{AdditionalRegex}";

                Help = help;

                this.OrderID = OrderID;
                this.GroupID = GroupID;
                this.Hidden = Hidden;
                AddDebug("Initialized completename, RegestStartMatchIndex, AdditionalRegex, help, OrderID, displayname, Hidden, GroupID");
            }
            /// <summary>
            /// Initializes a CLIWord with sort and visibility metadata.
            /// </summary>
            /// <param name="completename">The full command name.</param>
            /// <param name="regex">Regex pattern.</param>
            /// <param name="help">Help string.</param>
            /// <param name="OrderID">Display order ID.</param>
            /// <param name="Hidden">Whether the Word is hidden.</param>
            /// <param name="GroupID">Group the Word belongs to.</param>
            public CLIWord(string completename, string regex, string help, int OrderID, bool Hidden = false, int GroupID = 10)
            {
                CompleteName = completename;
                DisplayName = completename;
                Help = help;
                Regex = regex;
                Usage = completename;
                this.OrderID = OrderID;
                this.GroupID = GroupID;
                this.Hidden = Hidden;
                AddDebug("Initialized with completename,regex,help,orderid,groupid");
            }
            /// <summary>
            /// Initializes a CLIWord with complete display and behavior configuration.
            /// </summary>
            /// <param name="completename">The full command name.</param>
            /// <param name="regex">Regex pattern.</param>
            /// <param name="help">Help string.</param>
            /// <param name="usage">Usage description.</param>
            /// <param name="OrderID">Display order.</param>
            /// <param name="Hidden">Whether hidden from menus.</param>
            /// <param name="GroupID">Word grouping ID.</param>
            public CLIWord(string completename, string regex, string help, string usage, int OrderID, bool Hidden = false, int GroupID = 10)
            {
                CompleteName = completename;
                DisplayName = completename;
                Help = help;
                Regex = regex;
                Usage = usage;
                this.OrderID = OrderID;
                this.GroupID = GroupID;
                this.Hidden = Hidden;
                AddDebug("Initialized with completename,regex,help,usage,orderid,groupid"); ;
            }

            public CLIWord(string completename, int RegexStartMatchIndex, string usage, int OrderID = 1, string displayname = "", string AdditionalRegex = "", string help = "", bool Hidden = false, int GroupID = 10)
            {
                CompleteName = completename;
                DisplayName = completename;
                Help = help;
                Regex = string.IsNullOrWhiteSpace(AdditionalRegex)
                    ? BuildRegexWithIndex(completename, RegexStartMatchIndex)
                    : $"{BuildRegexWithIndex(completename, RegexStartMatchIndex)}|{AdditionalRegex}";
                DisplayName = string.IsNullOrEmpty(displayname) ? completename : displayname;
                Usage = usage;
                this.OrderID = OrderID;
                this.GroupID = GroupID;
                this.Hidden = Hidden;
                AddDebug("Initialized with completename,regex,help,usage,orderid,groupid"); ;
            }
            /// <summary>
            /// Initializes a CLIWord with a name and optional display name.
            /// </summary>
            /// <param name="completename">The Word name.</param>
            /// <param name="displayname">Optional display name.</param>
            public CLIWord(string completename, string displayname = "")
            {
                CompleteName = completename;
                AddDebug("Initialized with completename only");
            }
            /// <summary>
            /// Default constructor.
            /// </summary>
            public CLIWord()
            {
                AddDebug("Initialized with no parameters");
            }

            private void AddDebug(string Message)
            {
                this.DebugLog.Add(Message);
            }

            public void ClearDebugLog()
            {
                this.DebugLog.Clear();
            }

            public string BuildRegexWithIndex(string input, int requiredIndex)
            {
                if (string.IsNullOrEmpty(input))
                    throw new ArgumentException("Input string cannot be null or empty.");

                if (requiredIndex < 0 || requiredIndex >= input.Length)
                    throw new ArgumentOutOfRangeException(nameof(requiredIndex), "Index must be within the bounds of the string.");

                // Start with required prefix
                string pattern = "^" + input.Substring(0, requiredIndex + 1);

                // Add nested optional groups
                for (int i = requiredIndex + 1; i < input.Length; i++)
                {
                    pattern += "(" + input[i];
                }

                // Close groups with ? to make them optional
                for (int i = requiredIndex + 1; i < input.Length; i++)
                {
                    pattern += ")?";
                }

                pattern += "$";
                return pattern;
            }

            /// <summary>
            /// Sets the Regex property after validation.
            /// </summary>
            /// <param name="newRegex">New regex pattern to assign.</param>
            /// <returns>True if valid, false otherwise.</returns>
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

            /// <summary>
            /// Returns the full debug log as a string.
            /// </summary>
            public List<string> GetDebugLog()
            {
                return this.DebugLog;
            }
            /// <summary>
            /// Returns a printable string representation of this Word and subWords.
            /// </summary>
            /// <param name="index">Indent level for nested printing. Default is 0.</param>
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

                if (CLISubWords.Count > 0)
                {
                    foreach (CLIWord cliSubWord in CLISubWords)
                    {
                        ReturnValue += cliSubWord.Print(index);
                    }
                }

                return ReturnValue;
            }
            /// <summary>
            /// Tests whether the given search string matches this Word's regex.
            /// </summary>
            /// <param name="search">The string to test.</param>
            /// <returns>True if matched.</returns>

            public bool IsMatch(string search)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(this.Regex))
                    {
                        Regex findMatch = new Regex(this.Regex, RegexOptions.IgnoreCase);
                        return findMatch.IsMatch(search);
                    }
                }
                catch (ArgumentException ex)
                {
                    // Add diagnostic log if needed
                    DebugLog.Add($"[Regex Error] Pattern=\"{this.Regex}\" Exception={ex.Message}\n");
                }

                // Fallback: full-word, case-insensitive match if regex failed or was empty
                return string.Equals(this.CompleteName, search, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Adds a subWord using just its name and optional display name.
            /// </summary>
            public void AddSubWord(string newSubWord, string newDisplayName = "")
            {
                CLIWord addWord = new CLIWord(completename: newSubWord, displayname: newDisplayName);
                CLISubWords.Add(addWord);
            }
            /// <summary>
            /// Adds a subWord using a name and regex pattern.
            /// </summary>
            public void AddSubWord(string newSubWord, string newRegex, string newDisplayName = "")
            {
                CLIWord addWord = new CLIWord(completename: newSubWord, regex: newRegex, displayname: newDisplayName);
                CLISubWords.Add(addWord);
            }
            /// <summary>
            /// Adds a subWord with help text and pattern.
            /// </summary>
            public void AddSubWord(string newSubWord, string newRegex, string newHelp, string newDisplayName = "")
            {
                CLIWord addWord = new CLIWord(completename: newSubWord, regex: newRegex, help: newHelp, displayname: newDisplayName);
                CLISubWords.Add(addWord);
            }
            /// <summary>
            /// Adds a subWord and attaches another Word as a nested child.
            /// </summary>
            public void AddSubWord(string newSubWord, string newRegex, string newHelp, CLIWord subWord, string newDisplayName = "")
            {
                CLIWord addWord = new CLIWord(completename: newSubWord, regex: newRegex, help: newHelp, subWord: subWord, displayname: newDisplayName);
                CLISubWords.Add(addWord);
            }
            /// <summary>
            /// Adds an existing CLIWord instance as a subWord.
            /// </summary>
            public void AddSubWord(CLIWord newSubWord)
            {
                CLISubWords.Add(newSubWord);
            }
            /// <summary>
            /// Compares the current instance with another CLIWord and returns an integer that indicates
            /// whether the current instance precedes, follows, or occurs in the same position as the other in the sort order.
            /// </summary>
            /// <param name="other">The CLIWord to compare to this instance.</param>
            /// <returns>
            /// A value that indicates the relative order:
            /// Less than 0 — this instance precedes <paramref name="other"/>.
            /// 0 — same position.
            /// Greater than 0 — this instance follows <paramref name="other"/>.
            /// </returns>
            public int CompareTo(CLIWord? other)
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
        }
    }
}