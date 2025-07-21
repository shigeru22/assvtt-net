// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

namespace Kyutorius.AstonishedVendetta.Foundation;

public static class Converter
{
    public static string? GetFirstUpperCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        return value[0].ToString().ToUpperInvariant();
    }
}
