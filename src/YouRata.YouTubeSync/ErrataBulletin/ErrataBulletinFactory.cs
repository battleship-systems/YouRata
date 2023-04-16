// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using YouRata.Common.Configuration.ErrataBulletin;

namespace YouRata.YouTubeSync.ErrataBulletin;

/// <summary>
/// Static methods for creating video errata bulletins
/// </summary>
internal static class ErrataBulletinFactory
{
    public static ErrataBulletinBuilder CreateBuilder(Video video, ErrataBulletinConfiguration config)
    {
        string videoTitle = WebUtility.HtmlDecode(video.Snippet.Title) ?? "Unknown Video";
        TimeSpan contentDuration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
        ErrataBulletinBuilder bulletinBuilder = new ErrataBulletinBuilder(config, videoTitle, video.Id, contentDuration);
        return bulletinBuilder;
    }
}
