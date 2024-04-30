// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2024 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace DSharpPlus.Net;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

using DSharpPlus.Entities;

/// <summary>
/// Represents a multipart HTTP request.
/// </summary>
internal readonly record struct MultipartRestRequest : IRestRequest
{
    /// <inheritdoc/>
    public string Url { get; init; }

    /// <summary>
    /// The method for this request.
    /// </summary>
    public HttpMethod Method { get; init; }

    /// <inheritdoc/>
    public string Route { get; init; }

    /// <inheritdoc/>
    public bool IsExemptFromGlobalLimit { get; init; }

    /// <summary>
    /// The headers for this request.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets the dictionary of values attached to this request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Values { get; init; }

    /// <summary>
    /// Gets the dictionary of files attached to this request.
    /// </summary>
    public IReadOnlyList<DiscordMessageFile> Files { get; init; }

    public HttpRequestMessage Build()
    {
        HttpRequestMessage request = new()
        {
            Method = Method,
            RequestUri = new($"{Endpoints.BASE_URI}/{Url}")
        };

        if (Headers is not null)
        {
            foreach (KeyValuePair<string, string> header in Headers)
            {
                request.Headers.Add(header.Key, Uri.EscapeDataString(header.Value));
            }
        }

        request.Headers.Add("Connection", "keep-alive");
        request.Headers.Add("Keep-Alive", "600");

        string boundary = "---------------------------" + DateTimeOffset.UtcNow.Ticks.ToString("x");

        MultipartFormDataContent content = new(boundary);

        if (Values is not null)
        {
            foreach (KeyValuePair<string, string> element in Values)
            {
                content.Add(new StringContent(element.Value), element.Key);
            }
        }

        if (Files is not null)
        {
            for (int i = 0; i < Files.Count; i++)
            {
                DiscordMessageFile current = Files[i];

                StreamContent file = new(current.Stream);

                if (current.ContentType is not null)
                {
                    file.Headers.ContentType = MediaTypeHeaderValue.Parse(current.ContentType);
                }

                string filename = current.FileType is null
                    ? current.FileName
                    : $"{current.FileName}.{current.FileType}";

                // do we actually need this distinction? it's been made since the beginning of time,
                // but it doesn't seem very necessary
                if (Files.Count > 1)
                {
                    content.Add(file, $"file{i + 1}", filename);
                }
                else
                {
                    content.Add(file, "file", filename);
                }
            }
        }

        request.Content = content;

        return request;
    }
}
