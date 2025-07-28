// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kyutorius.AstonishedVendetta.Foundation;

namespace Kyutorius.AstonishedVendetta.Tests;

[Collection("VttConverter Tests")]
public class VttConverterTests
{
    // single line 1 - with style
    private const string SINGLE_LINE_TEST_1 = "Dialogue: 0,0:00:02.00,0:00:05.00,Style1,,0,0,0,,This is the first subtitle.";
    private const string SINGLE_LINE_RESULT_1 = "00:00:02.000 --> 00:00:05.000\r\n<v Style1>This is the first subtitle.\r\n";

    // single line 2 - with name
    private const string SINGLE_LINE_TEST_2 = "Dialogue: 0,0:00:02.00,0:00:05.00,,John,0,0,0,,This is the first subtitle.";
    private const string SINGLE_LINE_RESULT_2 = "00:00:02.000 --> 00:00:05.000\r\n<v John>This is the first subtitle.\r\n";

    // single line 3 - without style or name
    private const string SINGLE_LINE_TEST_3 = "Dialogue: 0,0:00:02.00,0:00:05.00,,,0,0,0,,This is the first subtitle.";
    private const string SINGLE_LINE_RESULT_3 = "00:00:02.000 --> 00:00:05.000\r\nThis is the first subtitle.\r\n";

    // single line with override styles
    private const string SINGLE_LINE_STYLED_TEST_1 = @"Dialogue: 0,0:00:02.00,0:00:05.00,,,0,0,0,,This is the {\b1}first{\b0} subtitle.";
    private const string SINGLE_LINE_STYLED_RESULT_1 = "00:00:02.000 --> 00:00:05.000\r\nThis is the <b>first</b> subtitle.\r\n";

    // multiple lines 1 - without header
    private const string MULTIPLE_LINE_TEST_1 = "Dialogue: 0,0:00:02.00,0:00:05.00,,,0,0,0,,This is the {\\b1}first{\\b0} subtitle.\r\nDialogue: 0,0:00:06.00,0:00:10.00,Bold,,0,0,0,,Here is a bold subtitle.\r\nDialogue: 0,0:00:12.00,0:00:16.00,Default,,0,0,0,,This is another {\\b1}Bold{\\b0} line of text.\r\n";
    private const string MULTIPLE_LINE_RESULT_1 = "1\r\n00:00:02.000 --> 00:00:05.000\r\nThis is the <b>first</b> subtitle.\r\n\r\n2\r\n00:00:06.000 --> 00:00:10.000\r\n<v Bold>Here is a bold subtitle.\r\n\r\n3\r\n00:00:12.000 --> 00:00:16.000\r\n<v Default>This is another <b>Bold</b> line of text.\r\n";

    // multiple lines 2 - without header
    private const string MULTIPLE_LINE_TEST_2 = "Dialogue: 0,0:00:02.00,0:00:05.00,,,0,0,0,,This is the {\\b1}first{\\b0} subtitle.\r\nDialogue: 0,0:00:06.00,0:00:10.00,Bold,,0,0,0,,Here is a bold subtitle.\r\nDialogue: 0,0:00:12.00,0:00:16.00,Default,,0,0,0,,This is another {\\b1}Bold{\\b0} line of text.\r\n";
    private const string MULTIPLE_LINE_RESULT_2 = "WEBVTT\r\n\r\n1\r\n00:00:02.000 --> 00:00:05.000\r\nThis is the <b>first</b> subtitle.\r\n\r\n2\r\n00:00:06.000 --> 00:00:10.000\r\n<v Bold>Here is a bold subtitle.\r\n\r\n3\r\n00:00:12.000 --> 00:00:16.000\r\n<v Default>This is another <b>Bold</b> line of text.\r\n";

    // file processing
    private const string FILE_TEST_EXPECTED_FILE_NAME = "0-expected.vtt";
    private const string FILE_TEST_INPUT_FILE_NAME = "1-input.ass";
    private const string FILE_TEST_OUTPUT_FILE_NAME = "2-output.vtt";


    [Fact(DisplayName = "Single line tests (simple)")]
    public void SingleLineTests()
    {
        string? result1 = VttConverter.ConvertLine(SINGLE_LINE_TEST_1);
        Assert.Equal(SINGLE_LINE_RESULT_1, result1);
        string? result2 = VttConverter.ConvertLine(SINGLE_LINE_TEST_2);
        Assert.Equal(SINGLE_LINE_RESULT_2, result2);
        string? result3 = VttConverter.ConvertLine(SINGLE_LINE_TEST_3);
        Assert.Equal(SINGLE_LINE_RESULT_3, result3);
    }

    [Fact(DisplayName = "Single line test (with override styles)")]
    public void SingleLineTestWithOverrideStyles()
    {
        string? result1 = VttConverter.ConvertLine(SINGLE_LINE_STYLED_TEST_1);
        Assert.Equal(SINGLE_LINE_STYLED_RESULT_1, result1);
    }

    [Fact(DisplayName = "Multiple line tests")]
    public async Task MultipleLineTests()
    {
        string? result1 = await VttConverter.ConvertStringAsync(MULTIPLE_LINE_TEST_1, Encoding.UTF8);
        Assert.Equal(MULTIPLE_LINE_RESULT_1, result1);
        string? result2 = await VttConverter.ConvertStringAsync(MULTIPLE_LINE_TEST_2,
            Encoding.UTF8,
            true);
        Assert.Equal(MULTIPLE_LINE_RESULT_2, result2);
    }

    [Fact(DisplayName = "File processing tests")]
    public async Task FileProcessingTests()
    {
        char pathSeparator = Path.DirectorySeparatorChar;
        string resourcesDirectory = $"{Directory.GetCurrentDirectory()}{pathSeparator}Resources";

        string expectedFilePath = $"{resourcesDirectory}{pathSeparator}{FILE_TEST_EXPECTED_FILE_NAME}";
        string inputFilePath = $"{resourcesDirectory}{pathSeparator}{FILE_TEST_INPUT_FILE_NAME}";
        string outputFilePath = $"{resourcesDirectory}{pathSeparator}{FILE_TEST_OUTPUT_FILE_NAME}";

        FileStream fsInput = new FileStream(inputFilePath, FileMode.Open);
        FileStream fsOutput = new FileStream(outputFilePath,
            FileMode.Create,
            FileAccess.Write);

        await VttConverter.ConvertStreamAsync(new StreamReader(fsInput), new StreamWriter(fsOutput));

        fsInput.Close();
        fsOutput.Close();

        FileStream fsExpected = new FileStream(expectedFilePath, FileMode.Open);
        fsOutput = new FileStream(outputFilePath, FileMode.Open);

        string expected = await new StreamReader(fsExpected).ReadToEndAsync();
        string result = await new StreamReader(fsOutput).ReadToEndAsync();

        fsExpected.Close();
        fsOutput.Close();

        Assert.Equal(expected, result);
    }
}
