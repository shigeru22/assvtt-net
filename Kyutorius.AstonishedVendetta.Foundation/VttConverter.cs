// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

// Reference and regex from:
// https://github.com/jamiees2/ass-to-vtt/blob/3b9a3c7819edb769bc30f0aa48f8a181bee37018/index.js
// (c) jamiees2, licensed under MIT License

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Kyutorius.AstonishedVendetta.Foundation;

/// <summary>
/// Main class to convert ASS to VTT.
/// </summary>
public static class VttConverter
{
    /// <summary>
    /// Subtitle regular expression with grouping.
    /// </summary>
    private static readonly Regex REGEX_ASS_GROUPING = new Regex(@"Dialogue:\s\d,(\d+:\d\d:\d\d.\d\d),(\d+:\d\d:\d\d.\d\d),([^,]*),([^,]*),(?:[^,]*,){4}(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// New line regular expression.
    /// </summary>
    private static readonly Regex REGEX_NEW_LINE = new Regex(@"\\N", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    /// <summary>
    /// Soft break (also known as line feed, LF) regular expression.
    /// </summary>
    private static readonly Regex REGEX_SOFT_BREAK = new Regex(@"\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    /// <summary>
    /// Hard space (\h in ASS format) regular expression.
    /// </summary>
    private static readonly Regex REGEX_HARD_SPACE = new Regex(@"\\h", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    /// <summary>
    /// ASS styling regular expression.
    /// </summary>
    private static readonly Regex REGEX_STYLE = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Delegate type for callback on each line converted.
    /// </summary>
    /// <param name="message">Converted line.</param>
    public delegate void OnLineOutputCallback(string message);

    /// <summary>
    /// Converts a single line.
    /// </summary>
    /// <param name="input">Input ASS dialogue line (starting with "Dialogue: ").</param>
    /// <param name="disableStyles">Whether to disable styles embedded from the ASS subtitle.</param>
    /// <param name="lineNumber">Line number before timestamp. Set as null to disable.</param>
    /// <returns>Converted line. May be null if error.</returns>
    public static string? ConvertLine(string input, bool disableStyles = false, int? lineNumber = null)
    {
        // test for matching subtitle pattern

        Match capturedSubtitle = REGEX_ASS_GROUPING.Match(input);
        if (!capturedSubtitle.Success)
        {
            return null;
        }

        // retrieve each groups' contents

        string? lineStart = null;
        string? lineEnd = null;
        string? lineStyle = null;
        string? lineName = null;
        string? lineText = string.Empty;

        int groupsCount = capturedSubtitle.Groups.Count;
        for (int groupsIndex = 1; groupsIndex < groupsCount; groupsIndex++)
        {
            if (groupsIndex > 5)
            {
                break;
            }

            switch (groupsIndex)
            {
                case 1:
                    lineStart = capturedSubtitle.Groups[1].Value;
                    break;
                case 2:
                    lineEnd = capturedSubtitle.Groups[2].Value;
                    break;
                case 3:
                    lineStyle = capturedSubtitle.Groups[3].Value;
                    break;
                case 4:
                    lineName = capturedSubtitle.Groups[4].Value;
                    break;
                case 5:
                    lineText = capturedSubtitle.Groups[5].Value;
                    break;
            }
        }

        // process special characters
        // '&' should be processed first before '<' and '>'

        lineText = lineText.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        // process style information, since subtitles may contain any number of
        // tags, loop each of them if any

        string subStyle = string.Empty;
        string subPositionStyle = string.Empty;
        Stack<string> subTagsToClose = new Stack<string>();

        Match capturedStyle = REGEX_STYLE.Match(lineText);
        while (capturedStyle.Success)
        {
            string[] tagsToOpen = [];
            string styleReplacement = string.Empty;

            // check for empty tags
            if (capturedStyle.Groups.Count > 1 && string.IsNullOrWhiteSpace(capturedStyle.Groups[1].Value))
            {
                // split override commands

                string[] currentStyles = capturedStyle.Groups[1]
                    .Value
                    .Split(@"\");

                // TODO: continue styled processing
            }

            // loop next tag

            lineText = REGEX_STYLE.Replace(lineText, styleReplacement);
            capturedStyle = REGEX_STYLE.Match(lineText);
        }

        // replace whitespaces

        lineText = REGEX_NEW_LINE.Replace(lineText, "\r\n");
        lineText = REGEX_SOFT_BREAK.Replace(lineText, " ");
        lineText = REGEX_HARD_SPACE.Replace(lineText, "&nbsp;");

        // prepare vtt format

        StringBuilder sbRet = new StringBuilder();

        if (string.IsNullOrWhiteSpace(lineStart))
        {
            lineStart = "0:00:00.00";
        }
        if (string.IsNullOrWhiteSpace(lineEnd))
        {
            lineEnd = "0:00:00.00";
        }

        string finalLineStart = (lineStart.Split(":")[0].Length <= 1)
            ? $"0{lineStart}0"
            : $"{lineStart}0";
        string endPrependZero = (lineEnd.Split(":")[0].Length <= 1)
            ? $"0{lineEnd}0"
            : $"{lineEnd}0";

        // usage of separate appends is intended to preserve newline format
        if (lineNumber != null)
        {
            sbRet.Append($"{lineNumber}\r\n");
        }
        sbRet.Append($"0{lineStart}0 --> 0{lineEnd}0{subPositionStyle}\r\n");
        if (!string.IsNullOrWhiteSpace(lineName))
        {
            sbRet.Append($"<v {lineName}>");
        }
        else if (!string.IsNullOrWhiteSpace(lineStyle))
        {
            sbRet.Append($"<v {lineStyle}>");
        }
        sbRet.Append(lineText);
        while (subTagsToClose.Count > 0)
        {
            sbRet.Append($"</{subTagsToClose.Pop()}>");
        }
        sbRet.Append("\r\n");

        return sbRet.ToString();
    }
}
