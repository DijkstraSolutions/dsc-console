# dsc-pub-console

Provides a means to add "verbs" and nested "verbs" to your command line application.

This project will build a DLL that you can include in your project.

See Example.cs on how to do an easy one level implementation.

To nest, you might want to work backwards, start with the AutoCompletes at the end of the command
and just do the verbs for that level, move back one level and add the subverbs to that previous level

For example:

If I want to AutoComplete for command "today tomorrow yesterday" I would do it this way:

```c#
CLIVerb newVerb3 = new CLIVerb("yesterday", "^yes?t?e?r?d?a?y?$", "This is a description for using yesterday", "yesterday");

CLIVerb newVerb2 = new CLIVerb("tomorrow", "^to?m?o?r?r?o?w?$", "This is a description for using tomorrow", "tomorrow");
newVerb2.AddSubVerb(newVerb3)

CLIVerb newVerb1 = new CLIVerb("today", "^tod?a?y?$", "This is a description for using today", "today");
newVerb1.AddSubVerb(newVerb2)

myconsole.AddVerb(newVerb1);
```

AutoComplete will only follow the appropriate AutoComplete for that respective verb parent at the current level. This is
important because today and tomorrow above both can AutoComplete after to based on the regex.

The author, Walter Holm, wishes to acknowledge some of the ideas noted in this [gist](https://gist.github.com/benkoshy/7f6f28e158032534615773a9a1f73a10) by [Ben Koshy](https://github.com/benkoshy) as a crude inspiration. 

