// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

using Kyutorius.AstonishedVendetta.Foundation;

namespace Kyutorius.AstonishedVendetta.Tests;

public class ConverterTests
{
    [Fact]
    public void GetFirstUpperCaseTests()
    {
        string strTest = "the quick brown fox jumps over the lazy dog.";
        string strTest2 = string.Empty;
        Assert.Equal("T", Converter.GetFirstUpperCase(strTest));
        Assert.Null(Converter.GetFirstUpperCase(strTest2));
    }
}
