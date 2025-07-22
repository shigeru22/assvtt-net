// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

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
}
