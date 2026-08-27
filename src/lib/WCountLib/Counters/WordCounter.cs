/*
    WCountLib
    Copyright (C) 2024-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace WCountLib.Counters;

/// <summary>
/// 
/// </summary>
public class WordCounter : IWordCounter
{
    private int CountWordsWorkerSegment(string input)
    {
        ArgumentNullException.ThrowIfNull(input);


        string[] strings = input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        // Count whitespace-separated tokens - match classic wc behaviour.
        return strings.Length;
    }
    
    private int CountWordsWorkerSegment(char[] input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Fall back to the string-based worker for correctness; avoids relying on SplitBy semantics here.
        return CountWordsWorkerSegment(new string(input));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public int CountWords(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return CountWordsWorkerSegment(text);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public int CountWords(char[] source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return CountWordsWorkerSegment(source);
    }
}