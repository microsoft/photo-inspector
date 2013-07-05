/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;

namespace MagnifierApp.Pages
{
    public class PhotosPageViewModel
    {
        private const string LOCALS_PATH = @"\LocalImages";

        public class Photo
        {
            private string _localPath;
            private string _libraryPath;
            private Stream _thumbnail;

            public string LocalPath
            {
                get
                {
                    return _localPath;
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

            public Photo(string localPath, string libraryPath, Stream thumbnail)
            {
                _localPath = localPath;
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
                store.DeleteFile(photo.LocalPath);
            }
        }

        public void DeleteAllPhotos()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (var photo in Photos)
                {
                    store.DeleteFile(photo.LocalPath);
                }
            }

            Photos.Clear();
        }

        private void PopulatePhotos()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.DirectoryExists(LOCALS_PATH))
                {
                    var localFilenames = store.GetFileNames(LOCALS_PATH + @"\*");

                    using (var library = new MediaLibrary())
                    {
                        using (var pictures = library.Pictures)
                        {
                            foreach (var localFilename in localFilenames)
                            {
                                string localPath = LOCALS_PATH + @"\" + localFilename;

                                for (int i = 0; i < pictures.Count; i++)
                                {
                                    using (var picture = pictures[i])
                                    {
                                        var libraryPath = picture.GetPath();
                                        var libraryFilename = FilenameFromPath(libraryPath);

                                        if (localFilename == libraryFilename)
                                        {
                                            var thumbnail = picture.GetThumbnail();
                                            var photo = new Photo(localPath, libraryPath, thumbnail);

                                            Photos.Add(photo);

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string FilenameFromPath(string path)
        {
            var pathParts = path.Split('\\');

            return pathParts[pathParts.Length - 1];
        }
    }
}
