/*
    WCount Cli
    Copyright (C) 2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using WCountCli.Logic;
using WCountLib.Abstractions.Logic;
using WCountLib.Logic;
using WCountLib.Counters;

IServiceCollection services = new ServiceCollection();

    services.AddSingleton<IWordCounter, WordCounter>();
services.AddSingleton<ICharacterCounter, CharacterCounter>();
services.AddSingleton<IByteCounter, ByteCounter>();
services.AddSingleton<ITextReaderLogic, TextReaderLogic>();

IServiceProvider serviceProvider = services.BuildServiceProvider();

Option<bool> wordOption = new("-w")
{
    Description = Resources.Arguments_WordCount_Description
};

Option<bool> lineOption = new("-l");
lineOption.Description = Resources.Arguments_LineCount_Description;

Option<bool> charOption = new("-m");
charOption.Description = Resources.Arguments_CharacterCount_Description;

Option<bool> byteOption = new("-c");
byteOption.Description = Resources.Arguments_ByteCount_Description;

Option<bool> verboseOption = new("-v");
verboseOption.Description = "Enable verbose output";

Argument<string[]> filesArgument = new("files");
filesArgument.Description = Resources.Arguments_FilePaths_Description;
filesArgument.Arity = ArgumentArity.ZeroOrMore;
filesArgument.Validators.Add(result =>
{
    if (result.Tokens.Count > 0 && result.Tokens.Select(t => t.Value).Any(f => !File.Exists(Path.GetFullPath(f))))
    {
        result.AddError("One or more files do not exist.");
    }
});

RootCommand rootCommand = new(Resources.App_Description);
rootCommand.Add(wordOption);
rootCommand.Add(lineOption);
rootCommand.Add(charOption);
rootCommand.Add(byteOption);
rootCommand.Add(verboseOption);
rootCommand.Add(filesArgument);

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
{
    CountSelection selection = CountSelection.None;

    if (parseResult.GetValue(lineOption)) selection |= CountSelection.Lines;
    if (parseResult.GetValue(wordOption)) selection |= CountSelection.Words;
    if (parseResult.GetValue(charOption)) selection |= CountSelection.Characters;
    if (parseResult.GetValue(byteOption)) selection |= CountSelection.Bytes;

    if (selection == CountSelection.None)
        selection = CountSelection.Default;

    bool verbose = parseResult.GetValue(verboseOption);
    string[] files = parseResult.GetValue(filesArgument) ?? [];

    ITextReaderLogic textReaderLogic = serviceProvider.GetRequiredService<ITextReaderLogic>();

    return await CountRunner.RunAsync(textReaderLogic, selection, files, Console.In,
        Console.Out, Console.Error, verbose, ct);
});

ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync(new InvocationConfiguration(), CancellationToken.None);
