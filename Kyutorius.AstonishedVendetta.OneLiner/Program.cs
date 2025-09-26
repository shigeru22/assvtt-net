// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

using System.CommandLine;
using System.CommandLine.Parsing;
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
        // TODO: clean up logging

        if (context.IsVerbose)
        {
            Console.WriteLine($"Checking input file extension: {context.InputFile}");
        }

        FileExtensions extInput;
        if (context.InputFile.EndsWith(".ass"))
        {
            extInput = FileExtensions.SUBSTATION_ALPHA;
        }
        else if (context.InputFile.EndsWith(".srt"))
        {
            extInput = FileExtensions.SUBRIP;
        }
        else
        {
            Console.Error.WriteLine($"Invalid input file extension: {context.InputFile}");
            return 1;
        }

        string inputFilePath = Path.GetFullPath(context.InputFile);
        string outputFilePath = Path.GetFullPath(context.OutputFile);

        if (context.IsVerbose)
        {
            Console.WriteLine($"Input file path: {inputFilePath}");
            Console.WriteLine($"Output file path: {outputFilePath}");
        }

        // create file streams

        FileStream fsInput;
        try
        {
            fsInput = new FileStream(inputFilePath, FileMode.Open);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Input file not found: {context.InputFile}");
            return 2;
        }
        if (context.IsVerbose)
        {
            Console.WriteLine("FileStream created for inputfilePath.");
        }
        FileStream fsOutput = new FileStream(outputFilePath,
            FileMode.Create,
            FileAccess.Write);
        if (context.IsVerbose)
        {
            Console.WriteLine("FileStream created for outputfilePath.");
        }

        // convert

        if (context.IsVerbose)
        {
            Console.WriteLine("Converting stream.");
        }

        switch (extInput)
        {
            case FileExtensions.SUBSTATION_ALPHA:
                await VttConverter.ConvertAssStreamAsync(new StreamReader(fsInput), new StreamWriter(fsOutput));
                break;
            case FileExtensions.SUBRIP:
                await VttConverter.ConvertSrtStreamAsync(new StreamReader(fsInput), new StreamWriter(fsOutput));
                break;
        }

        // no errors, return

        if (context.IsVerbose)
        {
            Console.WriteLine("No errors. Returning error code 0.");
        }

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
        Option<string> optVerbose = new("-v", "--verbose")
        {
            Required = false,
            Arity = ArgumentArity.Zero,
            Description = "Enable verbose output"
        };

        ret.Add(optInputFile);
        ret.Add(optOutputFile);
        ret.Add(optVerbose);

        // implement actions for parsed result

        ret.SetAction(pr =>
        {
            string inputFile = pr.GetRequiredValue(optInputFile);
            string outputFile = pr.GetRequiredValue(optOutputFile);
            OptionResult? isVerbose = pr.GetResult(optVerbose);

            ApplicationContext context = new ApplicationContext()
            {
                InputFile = inputFile,
                OutputFile = outputFile,
                IsVerbose = isVerbose != null
            };

            return Run(context);
        });

        return ret;
    }
}
