/*
 * Copyright © 2013-2014 Microsoft Mobile. All rights reserved.
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

        /// <summary>
        /// Takes a MediaLibrary photo localPath and tries to find a local localPath for a high
        /// resolution version of the same photo.
        /// </summary>
        /// <param name="libraryPath">Path to a photo in MediaLibrary</param>
        /// <returns>Path to a local copy of the same photo</returns>
        public static string MatchLibraryPathWithLocalPath(string libraryPath)
        {
            var localPathCandidate = LOCALS_PATH + '\\' + FilenameFromPath(libraryPath);

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.FileExists(localPathCandidate))
                {
                    return null;
                }
            }

            return localPathCandidate;
        }

        public static string MatchLocalPathWithLibraryPath(string localPath)
        {
            var localFilename = FilenameFromPath(localPath);

            using (var library = new MediaLibrary())
            {
                using (var pictures = library.Pictures)
                {
                    for (int i = 0; i < pictures.Count; i++)
                    {
                        using (var picture = pictures[i])
                        {
                            var libraryPath = picture.GetPath();
                            var libraryFilename = FilenameFromPath(libraryPath);

                            if (localFilename == libraryFilename)
                            {
                                return libraryPath;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static string MatchPathWithOriginalPath(string path)
        {
            var originalFilename = FilenameFromPath(path);
            var originalFilenameParts = originalFilename.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            if (originalFilenameParts.Length == 3 && originalFilenameParts[0] == @"photoinspector")
            {
                originalFilename = originalFilenameParts[0] + '_' + originalFilenameParts[1] + @".jpg";
            }

            var originalPathCandidate = ORIGINALS_PATH + '\\' + originalFilename;

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.FileExists(originalPathCandidate))
                {
                    return null;
                }
            }

            return originalPathCandidate;
        }
    }
}
