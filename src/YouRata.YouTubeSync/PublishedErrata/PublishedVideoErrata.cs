// Copyright (c) 2023 battleship-systems.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace YouRata.YouTubeSync.PublishedErrata
{
    /// <summary>
    /// Finds published errata from a bulletin file
    /// </summary>
    internal class PublishedVideoErrata
    {
        private readonly List<string> _errataEntries;
        private const string _timeValueDashes = "------------------------------------------------";

        public PublishedVideoErrata()
        {
            _errataEntries = new List<string>();
        }

        internal static PublishedVideoErrata BuildFromBulletin(string bulletin)
        {
            PublishedVideoErrata bulletinErrata = new PublishedVideoErrata();
            if (string.IsNullOrEmpty(bulletin)) return bulletinErrata;
            string zeroTimeValueMark = $"00 {_timeValueDashes}";
            int zeroTimeValueStart = bulletin.IndexOf(zeroTimeValueMark, StringComparison.InvariantCulture);
            if (zeroTimeValueStart <= 0) return bulletinErrata;
            // Remove the first time value mark
            string errataBulletinText = StripTimeValueMarks(bulletin.Remove(0, (zeroTimeValueStart + zeroTimeValueMark.Length)));
            bulletinErrata.ErrataEntries.AddRange(SplitErrataLines(errataBulletinText));
            return bulletinErrata;
        }

        private static List<string> SplitErrataLines(string rawBulletin)
        {
            List<string> errataLines = new List<string>();
            if (string.IsNullOrWhiteSpace(rawBulletin)) return errataLines;
            // Each line should represent a single bulletin
            string[] bulletinLines = rawBulletin.Split(Environment.NewLine);
            StringBuilder errataLineBuilder = new StringBuilder();
            foreach (string bulletinLine in bulletinLines)
            {
                string bulletinData = bulletinLine.Trim();
                // Each bulletin should have a time offset somewhere in the line
                if (Regex.IsMatch(bulletinData, @"\d{1,2}(:\d{2}){1,2}\b"))
                {
                    if (errataLineBuilder.Length > 0)
                    {
                        errataLines.Add(errataLineBuilder.ToString());
                        errataLineBuilder.Clear();
                    }
                    errataLineBuilder.Append(bulletinData);
                }
                else
                {
                    // A continuation of the previous line
                    if (errataLineBuilder.Length > 0)
                    {
                        errataLineBuilder.Append(" ");
                        errataLineBuilder.Append(bulletinData);
                    }
                }
            }
            // Add the last line if not already done
            if (errataLineBuilder.Length > 0)
            {
                errataLines.Add(errataLineBuilder.ToString());
            }
            return errataLines;
        }

        private static string StripTimeValueMarks(string timeMarkedBulletin)
        {
            return Regex.Replace(timeMarkedBulletin,
                @"(?m)^(\d{1,2}:)?\d{1,2}:\d{2}\s*-+\s*$", string.Empty);
        }

        public List<string> ErrataEntries => _errataEntries;
    }
}
