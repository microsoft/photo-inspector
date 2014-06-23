/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Windows.Storage;
using MagnifierApp.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace MagnifierApp.Utilities
{
    public class Mapping
    {
        public const string LOCALS_PATH = @"\LocalImages";
        public const string ORIGINALS_PATH = @"\OriginalImages";

        /// <summary>
        /// Takes a full localPath to a file and returns the last localPath component.
        /// </summary>
        /// <param name="localPath">Path</param>
        /// <returns>Last component of the given localPath</returns>
        public static string FilenameFromPath(string path)
        {
            var pathParts = path.Split('\\');

            return pathParts[pathParts.Length - 1];
        }

        public static void SetOriginal(string path, string original)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (!settings.Contains(path))
            {
                settings.Add(path, original);
            }
            else
            {
                settings[path] = original;
            }
            settings.Save();
        }

        public static string FindOriginal(string path)
        {
            if (path == null || !path.Contains(KnownFolders.SavedPictures.Path))
            {
                return null;
            }

            string runningNumberPattern = "\\s\\(\\d+\\).jpg";
            string replacement = ".jpg";
            Regex rgx = new Regex(runningNumberPattern);
            path = rgx.Replace(path, replacement);

            string originalPath = null;

            if (IsolatedStorageSettings.ApplicationSettings.Contains(path))
            {
                var value = IsolatedStorageSettings.ApplicationSettings[path] as string;
                Task.Run(async () =>
                {
                    try
                    {
                        await StorageFile.GetFileFromPathAsync(value);
                        originalPath = value;
                    }
                    catch (FileNotFoundException)
                    {
                        IsolatedStorageSettings.ApplicationSettings.Remove(path);
                    }
                }).Wait();
            }

            return originalPath;
        }

        public static bool HasCrop(string path)
        {
            foreach (var value in IsolatedStorageSettings.ApplicationSettings.Values)
            {
                if (path == (string)value)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
