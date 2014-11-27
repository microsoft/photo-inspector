/*
 * Copyright © 2013-2014 Microsoft Mobile. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Utilities;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Nokia.Graphics.Imaging;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MagnifierApp.Models
{

    public enum PhotoOrigin
    {
        Camera,
        Library,
        Reframe
    }

    public class PhotoModel : INotifyPropertyChanged
    {
        #region Members

        private PhotoOrigin _origin;

        private const string TOMBSTONING_PATH = @"\Tombstoning\PhotoModel";
        private const string TOMBSTONING_IMAGE_FILENAME = @"image.jpg";
        private const string TOMBSTONING_ORIGINAL_FILENAME = @"original.jpg";
        private const string TOMBSTONING_PATH_KEY = "PhotoModel.Path";

        public const string HIGH_RESOLUTION_PHOTO_SUFFIX = "__highres";
        public const string REFRAME_PHOTO_SUFFIX = "__reframe";

        private static PhotoModel _instance = null;

        private Stream _image = null;

        private string _path = null;
        private string _originalPath = null;
        private PhotoOrigin _originalOrigin = PhotoOrigin.Library;
        private bool _saved;

        private Size _imageSize = new Size(0, 0);

        public static uint LibraryMaxBytes = 2 * 1024 * 1024; // 2 megabytes
        public static uint LibraryMaxArea = 5 * 1024 * 1024; // 5 megapixels
        public static Size LibraryMaxSize = new Size(4096, 4096); // Maximum texture size on WP8 is 4096x4096

        #endregion Members

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public const string ImagePropertyName = "Image";
        public const string PathPropertyName = "Path";

        #endregion

        #region Properties

        public static PhotoModel Singleton
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PhotoModel();
                }

                return _instance;
            }

            private set
            {
                if (_instance != value)
                {
                    _instance = value;
                }
            }
        }

        public Stream Image
        {
            get
            {
                return _image;
            }

            private set
            {
                if (_image != value)
                {
                    if (_image != null)
                    {
                        _image.Close();
                    }

                    _image = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(ImagePropertyName));
                    }
                }
                else if (value != null)
                {
                    value.Close();
                }
            }
        }

        public async Task FromLibraryPath(string path)
        {
            _saved = true;
            _origin = PhotoOrigin.Library;
            StorageFile file = null;

            // Find matching high resolution photo if available
            if (!path.Contains(HIGH_RESOLUTION_PHOTO_SUFFIX))
            {
                string highResolutionPath = path.Replace(".jpg", HIGH_RESOLUTION_PHOTO_SUFFIX + ".jpg");
                try
                {
                    file = await StorageFile.GetFileFromPathAsync(highResolutionPath);
                    Path = highResolutionPath;
                }
                catch (FileNotFoundException)
                {
                }
            }
            if (file == null)
            {
                file = await StorageFile.GetFileFromPathAsync(path);
                Path = path;
            }
            Image = await file.OpenStreamForReadAsync();
        }

        public void FromCameraRollPath(string path)
        {
            Task.Run(async () =>
            {
                await FromLibraryPath(path);
            }).Wait();
        }

        public void FromSavedPicturesPath(string path, string originalPath)
        {
            _originalPath = originalPath;

            Task.Run(async () =>
            {
                await FromLibraryPath(path);
            }).Wait();
        }

        public async Task FromToken(string token)
        {
            using (var library = new MediaLibrary())
            {
                using (var picture = library.GetPictureFromToken(token))
                {
                    await FromLibraryPath(picture.GetPath());
                }
            }
        }

        public void FromNewImage(Stream image, PhotoOrigin origin)
        {
            _saved = false;
            _origin = origin;

            if (_path != null)
            {
                _originalPath = _path;
                _originalOrigin = _origin;
            }
            else if (_image != null && _originalPath == null) // Re-framed unsaved photo, save original photo to temporary folder
            {
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var filename = "captured.jpg";
                _image.Position = 0;
                Task.Run(async () =>
                {
                    using (var stream = await localFolder.OpenStreamForWriteAsync(filename, CreationCollisionOption.ReplaceExisting))
                    {
                        await _image.CopyToAsync(stream);
                    }
                }).Wait();
                _originalPath = localFolder.Path + "\\" + filename;
            }

            Image = image;
            Path = null;
        }

        public bool IsModified()
        {
            return (_originalPath != null && (_originalPath != _path));
        }

        public bool IsSaved()
        {
            return _saved;
        }

        public string Path
        {
            get
            {
                return _path;
            }

            private set
            {
                if (_path != value)
                {
                    _path = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(PathPropertyName));
                    }
                }
            }
        }

        public string OriginalPath
        {
            get
            {
                return _originalPath;
            }

            private set
            {
                _originalPath = value;
            }
        }

        #endregion Properties

        #region Constructors and destructors

        ~PhotoModel()
        {
            if (_image != null)
            {
                _image.Close();
            }
        }

        #endregion Constructors and destructors

        #region Initialization methods

        public void Clear()
        {
            Image = null;
            Singleton = null;
        }

        #endregion Initialization methods

        #region Saving methods

        /// <summary>
        /// Asynchronously saves a low resolution version of the photo to MediaLibrary and if the photo is too large to be saved
        /// to MediaLibrary as is also saves the original high resolution photo to application's local storage so that the
        /// high resolution version is not lost.
        /// </summary>
        public async Task SaveAsync()
        {
            var filenameBase = "photoinspector";
            filenameBase += '_' + DateTime.UtcNow.Ticks.ToString();

            if (_origin == PhotoOrigin.Camera)
            {
                Path = await SaveToCameraRoll(_image, filenameBase);
            }
            else if (_origin == PhotoOrigin.Reframe)
            {
                if (_originalPath != null && _originalPath.Contains(Windows.Storage.ApplicationData.Current.LocalFolder.Path))
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(_originalPath);
                    var source = await file.OpenStreamForReadAsync();
                    var filename = filenameBase + HIGH_RESOLUTION_PHOTO_SUFFIX + @".jpg";
                    using (var target = await KnownFolders.CameraRoll.OpenStreamForWriteAsync(filename, CreationCollisionOption.GenerateUniqueName))
                    {
                        await source.CopyToAsync(target);
                    }
                    _originalPath = KnownFolders.CameraRoll.Path + "\\" + filename;
                }
                Path = await SaveToSavedPictures(_image, filenameBase);
            }
        }

        private async Task<string> SaveToCameraRoll(Stream cameraImage, string filenameBase)
        {
            string filename = filenameBase + ".jpg";
            StorageFolder storageFolder = KnownFolders.CameraRoll;
            AutoResizeConfiguration resizeConfiguration = null;
            var buffer = StreamToBuffer(cameraImage);

            // Store low resolution image
            using (var source = new BufferImageSource(buffer))
            {
                var info = await source.GetInfoAsync();

                if (info.ImageSize.Width * info.ImageSize.Height > LibraryMaxArea)
                {
                    var compactedSize = CalculateSize(info.ImageSize, LibraryMaxSize, LibraryMaxArea);
                    resizeConfiguration = new AutoResizeConfiguration(LibraryMaxBytes, compactedSize,
                        new Size(0, 0), AutoResizeMode.Automatic, 0, ColorSpace.Yuv420);
                    buffer = await Nokia.Graphics.Imaging.JpegTools.AutoResizeAsync(buffer, resizeConfiguration);
                }

                using (var library = new MediaLibrary())
                {
                    library.SavePictureToCameraRoll(filename, buffer.AsStream());
                }
            }

            // Store high resolution image
            if (resizeConfiguration != null)
            {
                filename = filenameBase + HIGH_RESOLUTION_PHOTO_SUFFIX + @".jpg";
                cameraImage.Position = 0;

                using (var stream = await storageFolder.OpenStreamForWriteAsync(filename, CreationCollisionOption.GenerateUniqueName))
                {
                    await cameraImage.CopyToAsync(stream);
                }
            }
            _saved = true;

            return storageFolder.Path + "\\" + filename;
        }

        private async Task<string> SaveToSavedPictures(Stream reframedImage, string filenameBase)
        {

            // Save re-framed photo to Saved Pictures
            StorageFolder storageFolder = KnownFolders.SavedPictures;

            string filename;
            if (_originalPath != null)
            {
                var split = _originalPath.Split(new char[] { '\\' });
                filename = split[split.Length - 1];
                filename = filename.Replace(HIGH_RESOLUTION_PHOTO_SUFFIX, "");
                filename = filename.Replace(REFRAME_PHOTO_SUFFIX, "");
                filename = filename.Replace(".jpg", "__reframe.jpg");

                var key = storageFolder.Path + "\\" + filename;
                Mapping.SetOriginal(key, _originalPath);
            }
            else
            {
                filename = filenameBase + ".jpg";
            }

            _image.Position = 0;

            using (var stream = await storageFolder.OpenStreamForWriteAsync(filename, CreationCollisionOption.GenerateUniqueName))
            {
                await _image.CopyToAsync(stream);
            }

            _saved = true;
            return storageFolder.Path + "\\" + filename;
        }


        #endregion Saving methods

        #region Tombstoning methods

        public void Tombstone()
        {

        }

        public void Untombstone()
        {

        }

        #endregion Tombstoning methods

        public async void RevertOriginal()
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(_originalPath);
            Stream stream = await file.OpenStreamForReadAsync();
            Image = stream;
            var tmp = _originalPath;
            _originalPath = null;
            _origin = _originalOrigin;
            _saved = tmp != null;
            Path = tmp;
        }

        #region Private methods

        private IBuffer StreamToBuffer(Stream stream)
        {
            var memoryStream = stream as MemoryStream;

            if (memoryStream == null)
            {
                using (memoryStream = new MemoryStream())
                {
                    stream.Position = 0;
                    stream.CopyTo(memoryStream);

                    try
                    {
                        // Some streams do not support flushing

                        stream.Flush();
                    }
                    catch (Exception)
                    {
                    }

                    return memoryStream.GetWindowsRuntimeBuffer();
                }
            }
            else
            {
                return memoryStream.GetWindowsRuntimeBuffer();
            }
        }

        private Stream BufferToStream(IBuffer buffer)
        {
            return buffer.AsStream();
        }

        /// <summary>
        /// Calculates a new size from originalSize so that the maximum area is maxArea
        /// and maximum size is maxSize. Aspect ratio is preserved.
        /// </summary>
        /// <param name="originalSize">Original size</param>
        /// <param name="maxArea">Maximum area</param>
        /// <param name="maxSize">Maximum size</param>
        /// <returns>Area in same aspect ratio fit to the limits set in maxArea and maxSize</returns>
        private Size CalculateSize(Size originalSize, Size maxSize, double maxArea)
        {
            // Make sure that the image does not exceed the maximum size

            var width = originalSize.Width;
            var height = originalSize.Height;
            
            if (width > maxSize.Width)
            {
                var scale = maxSize.Width / width;

                width = width * scale;
                height = height * scale;
            }

            if (height > maxSize.Height)
            {
                var scale = maxSize.Height / height;
                
                width = width * scale;
                height = height * scale;
            }
            
            // Make sure that the image does not exceed maximum area

            var originalPixels = width * height;

            if (originalPixels > maxArea)
            {
                var scale = Math.Sqrt(maxArea / originalPixels);

                width = originalSize.Width * scale;
                height = originalSize.Height * scale;
            }

            return new Size(width, height);
        }

        #endregion  Private methods
    }
}
