// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

using System.CommandLine;
using Kyutorius.AstonishedVendetta.Foundation;

namespace Kyutorius.AstonishedVendetta.OneLiner;

public class Program
{
    private static readonly RootCommand ROOT_COMMAND_ARGS = BuildRootCommandArgs();

    public static void Main(string[] args)
    {
        ParseResult parseResult = ROOT_COMMAND_ARGS.Parse(args);
        Environment.Exit(parseResult.Invoke());
    }

    private static async Task<int> Run(ApplicationContext context)
    {
        // create file streams

        FileStream fsInput = new FileStream(context.InputFile, FileMode.Open);
        FileStream fsOutput = new FileStream(context.OutputFile,
            FileMode.Create,
            FileAccess.Write);

        // convert

        await VttConverter.ConvertStreamAsync(new StreamReader(fsInput), new StreamWriter(fsOutput));

        // no errors, return

        return 0;
    }

    private static RootCommand BuildRootCommandArgs()
    {
        // create RootCommand instance

        RootCommand ret = new RootCommand("Converts ASS input file to a VTT output file.");

        // add options

        Option<string> optInputFile = new("-i", "--input")
        {
            Required = true,
            Description = "Input file"
        };
        Option<string> optOutputFile = new("-o", "--output")
        {
            Required = true,
            Description = "Output file"
        };

        ret.Add(optInputFile);
        ret.Add(optOutputFile);

        // implement actions for parsed result

        ret.SetAction(pr =>
        {
            string inputFile = pr.GetRequiredValue(optInputFile);
            string outputFile = pr.GetRequiredValue(optOutputFile);

            ApplicationContext context = new ApplicationContext()
            {
                InputFile = inputFile,
                OutputFile = outputFile
            };

            Task.Run(async () => await Run(context));
        });

        return ret;
    }
}
