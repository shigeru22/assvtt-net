// Copyright (c) shigeru22. Licensed under the MIT license.
// See LICENSE in the repository root for details.

// Reference and regex from:
// https://github.com/jamiees2/ass-to-vtt/blob/3b9a3c7819edb769bc30f0aa48f8a181bee37018/index.js
// (c) jamiees2, licensed under MIT License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kyutorius.AstonishedVendetta.Foundation;

/// <summary>
/// Main ASS to VTT converter class.
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

    private static readonly Regex REGEX_STYLING_TAG = new Regex(@"[a-zA-Z]+", RegexOptions.Compiled);
    private static readonly Regex REGEX_B_TAG = new Regex(@"b(\d{3})", RegexOptions.Compiled);

    private static readonly string[] TEXT_DECORATION_TAGS = ["b", "i", "u", "s"];

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
    public static string? ConvertLine(string? input, bool disableStyles = false, int? lineNumber = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

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

        string subStyle = string.Empty; // TODO: unused?
        StringBuilder subPositionStyle = new StringBuilder();
        Stack<string> subTagsToClose = new Stack<string>();

        Match capturedStyle = REGEX_STYLE.Match(lineText);
        while (capturedStyle.Success)
        {
            List<string> subTagsToOpen = [];
            StringBuilder styleReplacement = new StringBuilder();

            // check for empty tags
            if (capturedStyle.Groups.Count > 1 && !string.IsNullOrWhiteSpace(capturedStyle.Groups[1].Value))
            {
                // split override commands

                string[] currentStyles = capturedStyle.Groups[1]
                    .Value
                    .Split(@"\");

                // for each commands, extract tag name, and check its attribute

                int currentStylesLength = currentStyles.Length;
                for (int currentStylesIndex = 1; currentStylesIndex < currentStylesLength; currentStylesIndex++)
                {
                    string currentStyle = currentStyles[currentStylesIndex];

                    Match capturedTag = REGEX_STYLING_TAG.Match(currentStyle);
                    if (capturedTag.Success && !string.IsNullOrWhiteSpace(capturedTag.Groups[0].Value))
                    {
                        string currentTag = capturedTag.Groups[0].Value;

                        // TODO: handle AsSpan() out of range exceptions
                        if (string.Equals(currentTag, "an") && int.TryParse(currentStyle.AsSpan(2, 3), out int targetNewPosition))
                        {
                            // new position commands, assume bottom center
                            // position as the default

                            double lineCalculation = Math.Floor((double)(targetNewPosition - 1) / 3);
                            int alignmentCalculation = targetNewPosition % 3;

                            if (lineCalculation == 1.0)
                            {
                                subPositionStyle.Append(" line:50%");
                            }
                            else if (lineCalculation == 2.0)
                            {
                                subPositionStyle.Append(" line:0");
                            }

                            if (alignmentCalculation == 1)
                            {
                                subPositionStyle.Append(" align:start");
                            }
                            else if (alignmentCalculation == 2)
                            {
                                subPositionStyle.Append(" align:end");
                            }
                        }
                        else if (string.Equals(currentTag, "a") && int.TryParse(currentStyle.AsSpan(1, 2), out int targetLegacyPosition))
                        {
                            // legacy position command

                            int alignmentCalculation = (targetLegacyPosition - 1) % 4;

                            if (targetLegacyPosition > 8)
                            {
                                subPositionStyle.Append(" line:50%");
                            }
                            else if (targetLegacyPosition > 4)
                            {
                                subPositionStyle.Append(" line:0");
                            }

                            if (alignmentCalculation == 0)
                            {
                                subPositionStyle.Append(" align:start");
                            }
                            else if (alignmentCalculation == 2)
                            {
                                subPositionStyle.Append(" align:end");
                            }
                        }
                        else if (currentTag.Length == 1)
                        {
                            if (TEXT_DECORATION_TAGS.Contains(currentTag))
                            {
                                // check simple text decoration commands to be
                                // mapped into its WebVTEGEquivalents
                                // note that strikethrough (\s) is not supported
                                // in WebVTT
                                // also, b-tag should be treated as an on-off
                                // flag

                                bool hasAttributeDisabledFlag = currentStyle.Length >= 2 &&
                                    int.TryParse(currentStyle.AsSpan(1, 1), out int attributeFlag) &&
                                    attributeFlag == 0;
                                Match capturedBoldDisabledTag = REGEX_B_TAG.Match(currentStyle);
                                bool hasBoldDisabledFlag = capturedBoldDisabledTag.Success &&
                                    capturedBoldDisabledTag.Groups.Count >= 2 &&
                                    int.TryParse(capturedBoldDisabledTag.Groups[1].Value, out int boldDisabledFlag) &&
                                    boldDisabledFlag < 500;
                                if (hasAttributeDisabledFlag || hasBoldDisabledFlag)
                                {
                                    // close the tag

                                    if (subTagsToClose.Contains(currentTag))
                                    {
                                        // nothing to be done if the tag hasn't
                                        // been opened yet
                                        // also tags must be nested, this also
                                        // ensured the tag nested inside the tag
                                        // being closed are also closed, then
                                        // opened again once the current tag is
                                        // closed

                                        while (subTagsToClose.Count > 0)
                                        {
                                            string currentClosingTag = subTagsToClose.Pop();
                                            styleReplacement.Append($"</{currentClosingTag}>");
                                            if (!string.Equals(currentClosingTag, currentTag))
                                            {
                                                subTagsToOpen.Add(currentClosingTag);
                                            }
                                            else
                                            {
                                                // no need to close the tags
                                                // that the current tag is
                                                // nested within

                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // open the tag

                                    if (!subTagsToClose.Contains(currentTag))
                                    {
                                        // nothing needs to be done if the tag
                                        // is already open, else place the tag
                                        // at the bottom of the stack of the
                                        // tags being opened

                                        subTagsToOpen.Insert(0, currentTag);
                                    }
                                }
                            }
                            else if (string.Equals(currentTag, "r"))
                            {
                                // reset override tags by closing all open tags
                                // TODO: 'r' could also be used to switch styles

                                while (subTagsToClose.Count > 0)
                                {
                                    styleReplacement.Append($"</{subTagsToClose.Pop()}>");
                                }
                            }
                        }

                        // insert open tags for tags in the to-open list

                        for (int subTagsToOpenIndex = subTagsToOpen.Count - 1; subTagsToOpenIndex >= 0; subTagsToOpenIndex--)
                        {
                            string currentOpeningTag = subTagsToOpen[subTagsToOpenIndex];
                            styleReplacement.Append($"<{currentOpeningTag}>");
                            subTagsToClose.Push(currentOpeningTag);
                        }
                        subTagsToOpen.Clear();
                    }
                }
            }

            // replace override tags and loop next tag

            lineText = REGEX_STYLE.Replace(lineText,
                styleReplacement.ToString(),
                1);
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

    /// <summary>
    /// Converts ASS input stream to VTT through the output stream.
    /// </summary>
    /// <param name="input">Input ASS stream.</param>
    /// <param name="output">Output stream.</param>
    /// <param name="callback">
    /// Callback to be invoked on each line processed.
    /// </param>
    /// <returns>Awaitable task for processing.</returns>
    public static async Task ConvertStreamAsync(StreamReader input, StreamWriter output, OnLineOutputCallback? callback = null)
    {
        bool hasHeaderPrinted = false;
        int currentVttLine = 1;

        while (!input.EndOfStream)
        {
            string? currentLine = await input.ReadLineAsync();
            string? convertedLine = ConvertLine(currentLine);

            if (!string.IsNullOrWhiteSpace(convertedLine))
            {
                if (!hasHeaderPrinted)
                {
                    await output.WriteAsync("WEBVTT\r\n\r\n");
                    await output.FlushAsync();

                    hasHeaderPrinted = true;
                }

                await output.WriteAsync($"{currentVttLine}\r\n{convertedLine}");
                if (!input.EndOfStream)
                {
                    await output.WriteAsync("\r\n");
                }

                await output.FlushAsync();

                callback?.Invoke(convertedLine);

                currentVttLine++;
            }
        }
    }

    /// <summary>
    /// Converts ASS string input with the specified encoding to VTT string.
    /// This processes the input using MemoryStream, thus required to specify an
    /// encoding.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="encoding">String encoding type.</param>
    /// <param name="printHeader">Whether to output a WebVTT header.</param>
    /// <param name="callback">
    /// Callback to be invoked on each line processed.
    /// </param>
    /// <returns>
    /// Awaitable task for processing, returning converted string.
    /// </returns>
    public static async Task<string> ConvertStringAsync(string input, Encoding encoding, bool printHeader = false, OnLineOutputCallback? callback = null)
    {
        bool hasHeaderPrinted = false;
        int currentVttLine = 1;

        MemoryStream inputStream = new MemoryStream(encoding.GetBytes(input));
        StreamReader inputStreamReader = new StreamReader(inputStream);

        StringBuilder sbRet = new StringBuilder();

        while (!inputStreamReader.EndOfStream)
        {
            string? currentLine = await inputStreamReader.ReadLineAsync();
            string? convertedLine = ConvertLine(currentLine);

            if (!string.IsNullOrWhiteSpace(convertedLine))
            {
                if (!hasHeaderPrinted && printHeader)
                {
                    sbRet.Append("WEBVTT\r\n\r\n");
                    hasHeaderPrinted = true;
                }

                sbRet.Append($"{currentVttLine++}\r\n{convertedLine}");
                if (!inputStreamReader.EndOfStream)
                {
                    sbRet.Append("\r\n");
                }

                callback?.Invoke(convertedLine);
            }
        }

        return sbRet.ToString();
    }
}
