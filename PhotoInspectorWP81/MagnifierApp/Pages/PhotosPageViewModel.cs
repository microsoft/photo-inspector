/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Models;
using MagnifierApp.Utilities;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace MagnifierApp.Pages
{
    public class PhotosPageViewModel
    {
        public class Photo
        {
            private string _path;
            private Stream _thumbnail;

            public string Path
            {
                get
                {
                    return _path;
                }
            }

            public string OriginalPath
            {
                get
                {
                    if (_path.Contains(KnownFolders.SavedPictures.Path))
                    {
                        return Mapping.FindOriginal(_path);
                    }
                    else
                    {
                        return null;
                    }
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
                    return (OriginalPath != null) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            public object Tag
            {
                get
                {
                    return this;
                }
            }

            public Photo(string path, Stream thumbnail)
            {
                _path = path;
                _thumbnail = thumbnail;
            }
        }

        public bool Initialized { get; private set; }

        public ObservableCollection<Photo> Photos { get; private set; }

        public PhotosPageViewModel()
        {
            Photos = new ObservableCollection<Photo>();

#pragma warning disable 4014
            PopulatePhotos();
        }

        public async Task DeletePhoto(Photo photo)
        {
            Photos.Remove(photo);
            try
            {
                if (!Mapping.HasCrop(photo.Path))
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(photo.Path);
                    await file.DeleteAsync();
                }

                if (photo.Path.Contains(KnownFolders.CameraRoll.Name))
                {
                    var lowResPath = photo.Path.Replace(PhotoModel.HIGH_RESOLUTION_PHOTO_SUFFIX, "");
                    StorageFile file = await StorageFile.GetFileFromPathAsync(lowResPath);
                    await file.DeleteAsync();
                }
            }
            catch (FileNotFoundException)
            { }
        }

        private async Task PopulatePhotos()
        {
            var highResolutionPhotoFiles = await KnownFolders.CameraRoll.GetFilesAsync(CommonFileQuery.DefaultQuery);
            var savedPhotoFiles = await KnownFolders.SavedPictures.GetFilesAsync(CommonFileQuery.DefaultQuery);
            var concatenated = highResolutionPhotoFiles.Concat(savedPhotoFiles).OrderBy(c => c.Name);

            Photos.Clear();

            foreach (var file in concatenated)
            {
                var path = file.Path;

                // Delete high resolution file form camera roll if low resolution file is not found
                bool delete = false;
                if (path.Contains(KnownFolders.CameraRoll.Path) &&
                    path.Contains(PhotoModel.HIGH_RESOLUTION_PHOTO_SUFFIX) &&
                    path.Contains("photoinspector") &&
                    !Mapping.HasCrop(path))
                {
                    try
                    {
                        var lowResolutionPath = path.Replace(PhotoModel.HIGH_RESOLUTION_PHOTO_SUFFIX, "");
                        var lowResolutionFile = await StorageFile.GetFileFromPathAsync(lowResolutionPath);
                    }
                    catch (FileNotFoundException)
                    {
                        delete = true;
                    }
                }
                if (delete)
                {
                    System.Diagnostics.Debug.WriteLine("Low resolution version of " + path + " not found. Deleting file.");
                    await file.DeleteAsync();
                }

                // Skip high resolution files in Camera Roll and files in Saved Pictures without "reframe" in name
                if ((path.Contains(KnownFolders.SavedPictures.Name) && !path.Contains(PhotoModel.REFRAME_PHOTO_SUFFIX))
                    || !path.Contains(".jpg")
                    || path.Contains(PhotoModel.HIGH_RESOLUTION_PHOTO_SUFFIX))
                    continue;

                // Get thumbnail
                StorageFile thumbnailFile = await StorageFile.GetFileFromPathAsync(path);
                var thumbnail = await thumbnailFile.GetThumbnailAsync(ThumbnailMode.ListView);
                var photo = new Photo(path, thumbnail.AsStream());

                Initialized = true;
                Photos.Add(photo);                
            }

            if (!Initialized)
            {
                Initialized = true;
                Photos.Clear();
            }
        }
    }
}
