/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Utilities;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MagnifierApp.Pages
{
    public class PhotosPageViewModel
    {
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

            public Visibility CropIndicatorVisibility
            {
                get
                {
                    return _originalPath == null ? Visibility.Collapsed : Visibility.Visible;
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
            Photos.Clear();

            var newPhotosCollection = new ObservableCollection<Photo>();

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
                                var localPath = Mapping.MatchLibraryPathWithLocalPath(libraryPath);
                                var originalPath = Mapping.MatchPathWithOriginalPath(libraryPath);

                                if (localPath != null || originalPath != null)
                                {
                                    var thumbnail = picture.GetThumbnail();
                                    var photo = new Photo(localPath, originalPath, libraryPath, thumbnail);

                                    newPhotosCollection.Add(photo);
                                }
                            }
                        }
                    }
                }
            }

            Func<Photo, string> keySelector = (p) =>
            {
                return Mapping.FilenameFromPath(p.LibraryPath);
            };

            var orderedEnumerable = newPhotosCollection.OrderBy(keySelector);

            foreach (var photo in orderedEnumerable)
            {
                Photos.Add(photo);
            }
        }
    }
}
