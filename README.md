# dsc-pub-console

Provides a means to add "words" and nested "words" to your command line application.

This project will build a DLL that you can include in your project.

See [Example.cs](./Example.cs) on how to do an easy one level implementation.

To nest, you might want to work backwards, start with the AutoCompletes at the end of the command
and just do the words for that level, move back one level and add the subwords to that previous level

## Quick Example:

If I want to AutoComplete for command "today tomorrow yesterday" I would do it this way:

```c#
CLIWord newWord3 = new CLIWord("yesterday", "^yes?t?e?r?d?a?y?$", "This is a description for using yesterday", "yesterday");

CLIWord newWord2 = new CLIWord("tomorrow", "^to?m?o?r?r?o?w?$", "This is a description for using tomorrow", "tomorrow");
newWord2.AddSubWord(newWord3)

CLIWord newWord1 = new CLIWord("today", "^tod?a?y?$", "This is a description for using today", "today");
newWord1.AddSubWord(newWord2)

myconsole.AddWord(newWord1);
```

AutoComplete will only follow the appropriate AutoComplete for that respective "word" parent at the current level. This is
important because today and tomorrow above both can AutoComplete after to based on the regex.

## More complete and updated example:

Keep in mind, the regex above allows typos, this does not:

```c#
//Define the AutoConsole
dsc_public.console.AutoConsole cmd = new dsc_public.console.AutoConsole();

//This will help with defining words for your command line
CLIWord newWord = new CLIWord();
CLIWord newSubWord = new CLIWord();

//Create a new word called service order, make it so you need to type at least se OR -so before pressing tab will work,
  sets the help message and usage mesages. Controls the index number, whether to mark it hidden (still works with tab - auto complete).
  Sets the group number (you can group commands together on the same level)
newWord = new CLIWord("service-order", RegexStartMatchIndex: 1, AdditionalRegex: "^-so$", help: "Sets the Service Order number for the current context.", usage: "so [so ID] | service-order [so ID]", OrderID: order++, Hidden: false, GroupID: 20);

//Sets a subword to service-order, first one is a pur regex value (<r/> says return the match), its usage, help and command order.
newWord.AddSubWord(new CLIWord("<r/>", regex: @"^\d{6}$", usage: "123456", help: "Service order number", OrderID: order++));

//Like the rest but the the letter c is unique enough for auto complete to work
newWord.AddSubWord(new CLIWord("create", 0, usage: "service-order create", help: "Create service order", OrderID: order++));
newWord.AddSubWord(new CLIWord("find", 0, usage: "service-order find []", help: "Find service order from account number, or using name, find account numbers to search", OrderID: order++));

//Add the word(s) to the Auto Console
cmd.AddWord(newWord);


```
---
This just built the follow command:

 - service-order
   - [regex value returned if matched, in this case 6 digits]
   - create
   - find
---
Next you start the AutoConsole and when you press enter after input it will return the results to you to process

```c#
//Sort based on group IDs and then index
cmdtp.AutocompleteTree.Sort();

string MyPrompt = "#";

//Set a prompt
cmdtp.Prompt = $"{MyPrompt} ";

//Start the Auto Console

string UseBuffer = "If you want text to be pre-populated, you can use this or you can leave it out.";
bool exit = false;

do
{
  cmdtp.StartAutoConsole(UseBuffer);
  //data will come out mostly unmolested and untrimmed on purpose
  if (cmdtp.LastCompleteWordPhrase.ToLower().Trim() == "exit")
  {
    exit = true;
  }
} while (!exit)
```

The author, Walter Holm, wishes to acknowledge some of the ideas noted in this [gist](https://gist.github.com/benkoshy/7f6f28e158032534615773a9a1f73a10) by [Ben Koshy](https://github.com/benkoshy) as a crude inspiration. 

