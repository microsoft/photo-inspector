/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.Linq;

namespace MagnifierApp.Pages
{
    public class PhotosPageViewModel
    {
        private const string LOCALS_PATH = @"\LocalImages";
        private const string ORIGINALS_PATH = @"\OriginalImages";

        public class Photo
        {
            private string _localPath;
            private string _originalPath;
            private string _libraryPath;
            private Stream _thumbnail;

            public string LocalPath
            {
                get
                {
                    return _localPath;
                }
            }

            public string OriginalPath
            {
                get
                {
                    return _originalPath;
                }
            }

            public string LibraryPath
            {
                get
                {
                    return _libraryPath;
                }
            }

            public BitmapImage Thumbnail
            {
                get
                {
                    _thumbnail.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.SetSource(_thumbnail);

                    return bitmap;
                }
            }

            public object Tag
            {
                get
                {
                    return this;
                }
            }

            public Photo(string localPath, string originalPath, string libraryPath, Stream thumbnail)
            {
                _localPath = localPath;
                _originalPath = originalPath;
                _libraryPath = libraryPath;
                _thumbnail = thumbnail;
            }
        }

        public ObservableCollection<Photo> Photos { get; private set; }

        public PhotosPageViewModel()
        {
            Photos = new ObservableCollection<Photo>();

            PopulatePhotos();
        }

        public void DeletePhoto(Photo photo)
        {
            Photos.Remove(photo);

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (photo.LocalPath != null)
                {
                    store.DeleteFile(photo.LocalPath);
                }

                if (photo.OriginalPath != null)
                {
                    store.DeleteFile(photo.OriginalPath);
                }
            }
        }

        public void DeleteAllPhotos()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (var photo in Photos)
                {
                    if (photo.LocalPath != null)
                    {
                        store.DeleteFile(photo.LocalPath);
                    }

                    if (photo.OriginalPath != null)
                    {
                        store.DeleteFile(photo.OriginalPath);
                    }
                }
            }

            Photos.Clear();
        }

        private void PopulatePhotos()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var library = new MediaLibrary())
                {
                    using (var pictures = library.Pictures)
                    {
                        for (int i = 0; i < pictures.Count; i++)
                        {
                            using (var picture = pictures[i])
                            {
                                var libraryPath = picture.GetPath();
                                var localPath = MatchLibraryPathWithLocalPath(libraryPath);
                                var originalPath = MatchPathWithOriginalPath(libraryPath);

                                if (localPath != null || originalPath != null)
                                {
                                    var thumbnail = picture.GetThumbnail();
                                    var photo = new Photo(localPath, originalPath, libraryPath, thumbnail);

                                    Photos.Add(photo);
                                }
                            }
                        }
                    }
                }
            }
        }

        private string MatchLibraryPathWithLocalPath(string libraryPath)
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

        private string MatchPathWithOriginalPath(string path)
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

        private static string FilenameFromPath(string path)
        {
            var pathParts = path.Split('\\');

            return pathParts[pathParts.Length - 1];
        }
    }
}
