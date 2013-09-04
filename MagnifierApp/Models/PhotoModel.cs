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
using Nokia.Graphics.Imaging;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace MagnifierApp.Models
{
    public class PhotoModel : INotifyPropertyChanged
    {
        #region Members

        private const string TOMBSTONING_PATH = @"\Tombstoning\PhotoModel";
        private const string TOMBSTONING_IMAGE_FILENAME = @"image.jpg";
        private const string TOMBSTONING_ORIGINAL_FILENAME = @"original.jpg";
        private const string TOMBSTONING_LIBRARYPATH_KEY = "PhotoModel.LibraryPath";
        private const string TOMBSTONING_LOCALPATH_KEY = "PhotoModel.LocalPath";
        private const string TOMBSTONING_ORIGINALPATH_KEY = "PhotoModel.OriginalPath";
        private const string TOMBSTONING_ORIGINALLIBRARYPATH_KEY = "PhotoModel.OriginalLibraryPath";

        private static PhotoModel _instance = null;

        private Stream _image = null;
        private Stream _original = null;
        private string _libraryPath = null;
        private string _localPath = null;
        private string _originalPath = null;
        private string _originalLibraryPath = null;
        private Size _imageSize = new Size(0, 0);

        #endregion Members

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public const string ImagePropertyName = "Image";
        public const string OriginalPropertyName = "Original";
        public const string LibraryPathPropertyName = "LibraryPath";
        public const string LocalPathPropertyName = "LocalPath";
        public const string OriginalPathPropertyName = "OriginalPath";

        #endregion

        #region Properties

        public static uint LibraryMaxBytes = 2 * 1024 * 1024; // 2 megabytes
        public static uint LibraryMaxArea = 5 * 1024 * 1024; // 5 megapixels
        public static Size LibraryMaxSize = new Size(4096, 4096); // Maximum texture size on WP8 is 4096x4096

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

        public Stream Original
        {
            get
            {
                return _original;
            }

            private set
            {
                if (_original != value)
                {
                    if (_original != null)
                    {
                        _original.Close();
                    }

                    _original = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(OriginalPropertyName));
                    }
                }
                else if (value != null)
                {
                    value.Close();
                }
            }
        }

        public string LibraryPath
        {
            get
            {
                return _libraryPath;
            }

            private set
            {
                if (_libraryPath != value)
                {
                    _libraryPath = value;

                    System.Diagnostics.Debug.WriteLine("PhotoModel.LibraryPath is \"" + _libraryPath + "\"");

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(LibraryPathPropertyName));
                    }
                }
            }
        }

        public string LocalPath
        {
            get
            {
                return _localPath;
            }

            private set
            {
                if (_localPath != value)
                {
                    _localPath = value;

                    System.Diagnostics.Debug.WriteLine("PhotoModel.LocalPath is \"" + _localPath + "\"");

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(LocalPathPropertyName));
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
                if (_originalPath != value)
                {
                    _originalPath = value;

                    System.Diagnostics.Debug.WriteLine("PhotoModel.OriginalPath is \"" + _originalPath + "\"");

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(OriginalPathPropertyName));
                    }
                }
            }
        }

        private string OriginalLibraryPath
        {
            get
            {
                return _originalLibraryPath;
            }

            set
            {
                if (_originalLibraryPath != value)
                {
                    _originalLibraryPath = value;
                }
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

            if (_original != null)
            {
                _original.Close();
            }
        }

        #endregion Constructors and destructors

        #region Initialization methods

        public void FromLibraryImage(string libraryPath, Stream image)
        {
            System.Diagnostics.Debug.Assert(libraryPath != null);
            System.Diagnostics.Debug.Assert(image != null);

            if (LibraryPath != libraryPath)
            {
                LibraryPath = libraryPath;
                LocalPath = Mapping.MatchLibraryPathWithLocalPath(libraryPath);

                if (LocalPath != null)
                {
                    image.Close();

                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        Image = store.OpenFile(LocalPath, FileMode.Open);
                    }
                }
                else
                {
                    Image = image;
                }

                OriginalPath = Mapping.MatchPathWithOriginalPath(libraryPath);

                if (OriginalPath != null)
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        Original = store.OpenFile(OriginalPath, FileMode.Open);
                    }
                }
            }
            else
            {
                image.Close();
            }
        }

        public void FromLibraryImage(string token)
        {
            System.Diagnostics.Debug.Assert(token != null);

            using (var library = new MediaLibrary())
            {
                using (var picture = library.GetPictureFromToken(token))
                {
                    var libraryPath = picture.GetPath();

                    if (LibraryPath != libraryPath)
                    {
                        LibraryPath = libraryPath;
                        LocalPath = Mapping.MatchLibraryPathWithLocalPath(libraryPath);

                        if (LocalPath != null)
                        {
                            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                Image = store.OpenFile(LocalPath, FileMode.Open);
                            }
                        }
                        else
                        {
                            Image = picture.GetImage();
                        }

                        OriginalPath = Mapping.MatchPathWithOriginalPath(libraryPath);

                        if (OriginalPath != null)
                        {
                            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                Original = store.OpenFile(OriginalPath, FileMode.Open);
                            }
                        }
                    }
                }
            }
        }

        public void FromLocalImage(string localPath, Stream image)
        {
            System.Diagnostics.Debug.Assert(localPath != null);
            System.Diagnostics.Debug.Assert(image != null);

            if (LocalPath != localPath)
            {
                LibraryPath = Mapping.MatchLocalPathWithLibraryPath(localPath);
                LocalPath = localPath;
                Image = image;

                OriginalPath = Mapping.MatchPathWithOriginalPath(localPath);

                if (OriginalPath != null)
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        Original = store.OpenFile(OriginalPath, FileMode.Open);
                    }
                }
            }
            else
            {
                image.Close();
            }
        }

        public void FromNewImage(Stream image)
        {
            System.Diagnostics.Debug.Assert(image != null);

            Image = image;
            Original = null;
            LibraryPath = null;
            LocalPath = null;
            OriginalPath = null;
        }

        public void FromNewCrop(Stream image)
        {
            System.Diagnostics.Debug.Assert(image != null);

            if (Original == null && _image != null)
            {
                var original = new MemoryStream();

                _image.Position = 0;
                _image.CopyTo(original);

                Original = original;
            }

            if (LibraryPath != null)
            {
                OriginalLibraryPath = LibraryPath;

                LibraryPath = null;
            }

            LocalPath = null;

            Image = image;
        }

        public void RevertOriginal()
        {
            if (OriginalLibraryPath == null)
            {
                var original = new MemoryStream();

                Original.Position = 0;
                Original.CopyTo(original);
                Original = null;

                Image = original;
            }
            else
            {
                var originalLibraryPathCopy = OriginalLibraryPath;

                OriginalLibraryPath = null;
                Original = null;

                using (var library = new MediaLibrary())
                {
                    using (var pictures = library.Pictures)
                    {
                        for (int i = 0; i < pictures.Count; i++)
                        {
                            using (var picture = pictures[i])
                            {
                                if (picture.GetPath() == originalLibraryPathCopy)
                                {
                                    PhotoModel.Singleton.FromLibraryImage(originalLibraryPathCopy, picture.GetImage());
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            Image = null;
            Original = null;
            LibraryPath = null;
            LocalPath = null;
            OriginalPath = null;
            OriginalLibraryPath = null;
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
            if (_image != null && _image.Length > 0 && LibraryPath == null)
            {
                var buffer = StreamToBuffer(_image);

                AutoResizeConfiguration resizeConfiguration = null;

                using (var editingSession = new EditingSession(buffer))
                {
                    if (editingSession.Dimensions.Width * editingSession.Dimensions.Height > LibraryMaxArea)
                    {
                        var compactedSize = CalculateSize(editingSession.Dimensions, LibraryMaxSize, LibraryMaxArea);

                        resizeConfiguration = new AutoResizeConfiguration(LibraryMaxBytes, compactedSize,
                            new Size(0, 0), AutoResizeMode.Automatic, 0, ColorSpace.Yuv420);
                    }
                }

                var filenameBase = "photoinspector";

                if (_original != null)
                {
                    if (_originalPath != null)
                    {
                        var originalPathParts = _originalPath.Split(new char[] { '_', '.' }, StringSplitOptions.RemoveEmptyEntries);

                        filenameBase += '_' + originalPathParts[1];
                    }
                    else
                    {
                        filenameBase += '_' + DateTime.UtcNow.Ticks.ToString();

                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (!store.DirectoryExists(Mapping.ORIGINALS_PATH))
                            {
                                store.CreateDirectory(Mapping.ORIGINALS_PATH);
                            }

                            var originalPath = Mapping.ORIGINALS_PATH + @"\" + filenameBase + @".jpg";

                            using (var file = store.CreateFile(originalPath))
                            {
                                _original.Position = 0;
                                _original.CopyTo(file);

                                file.Flush();

                                OriginalPath = originalPath;
                            }
                        }
                    }
                }

                filenameBase += '_' + DateTime.UtcNow.Ticks.ToString();

                if (resizeConfiguration != null)
                {
                    // Store high resolution original to application local storage

                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(Mapping.LOCALS_PATH))
                        {
                            store.CreateDirectory(Mapping.LOCALS_PATH);
                        }

                        var localPath = Mapping.LOCALS_PATH + @"\" + filenameBase + @".jpg";

                        using (var file = store.CreateFile(localPath))
                        {
                            _image.Position = 0;
                            _image.CopyTo(file);

                            file.Flush();

                            LocalPath = localPath;
                        }
                    }

                    // Compact the buffer for saving to the library

                    buffer = await Nokia.Graphics.Imaging.JpegTools.AutoResizeAsync(buffer, resizeConfiguration);
                }

                using (var libraryImage = buffer.AsStream())
                {
                    libraryImage.Position = 0;

                    using (var library = new MediaLibrary())
                    {
                        using (var picture = library.SavePictureToCameraRoll(filenameBase, libraryImage))
                        {
                            LibraryPath = picture.GetPath();
                        }
                    }
                }
            }
        }

        #endregion Saving methods

        #region Cleanup methods

        /// <summary>
        /// Goes through all locally saved photos and tries to find a match for them in the MediaLibrary.
        /// If a match is not found (photo has been deleted from the MediaLibrary) this routine deletes
        /// also the locally saved photo.
        /// </summary>
        public void CleanLocalStorage()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.DirectoryExists(Mapping.LOCALS_PATH))
                {
                    var localsArray = store.GetFileNames(Mapping.LOCALS_PATH + @"\*");

                    using (var library = new MediaLibrary())
                    {
                        using (var pictures = library.Pictures)
                        {
                            foreach (var localFilename in localsArray)
                            {
                                var found = false;

                                for (int i = 0; i < pictures.Count && !found; i++)
                                {
                                    using (var picture = pictures[i])
                                    {
                                        var libraryFilename = Mapping.FilenameFromPath(picture.GetPath());

                                        if (localFilename == libraryFilename)
                                        {
                                            found = true;
                                        }
                                    }
                                }

                                if (!found)
                                {
                                    var localPath = Mapping.LOCALS_PATH + @"\" + localFilename;

                                    store.DeleteFile(localPath);

                                    System.Diagnostics.Debug.WriteLine("PhotoModel.CleanLocalStorage deleted local \"" + localPath + "\"");

                                    if (LocalPath == localPath)
                                    {
                                        // current image was deleted from library

                                        LibraryPath = null;
                                        LocalPath = null;
                                    }
                                }
                            }
                        }
                    }
                }

                if (store.DirectoryExists(Mapping.ORIGINALS_PATH))
                {
                    var originalsArray = store.GetFileNames(Mapping.ORIGINALS_PATH + @"\*");

                    using (var library = new MediaLibrary())
                    {
                        using (var pictures = library.Pictures)
                        {
                            foreach (var originalFilename in originalsArray)
                            {
                                var found = false;

                                for (int i = 0; i < pictures.Count && !found; i++)
                                {
                                    using (var picture = pictures[i])
                                    {
                                        var libraryFilename = Mapping.FilenameFromPath(picture.GetPath());
                                        var libraryFilenameParts = libraryFilename.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

                                        if (libraryFilenameParts.Length == 3 && libraryFilenameParts[0] == @"photoinspector")
                                        {
                                            libraryFilename = libraryFilenameParts[0] + '_' + libraryFilenameParts[1] + @".jpg";
                                        }

                                        if (originalFilename == libraryFilename)
                                        {
                                            found = true;
                                        }
                                    }
                                }

                                if (!found)
                                {
                                    var originalPath = Mapping.ORIGINALS_PATH + @"\" + originalFilename;

                                    store.DeleteFile(originalPath);

                                    System.Diagnostics.Debug.WriteLine("PhotoModel.CleanLocalStorage deleted original \"" + originalPath + "\"");
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion Cleanup methods

        #region Tombstoning methods

        public void Tombstone()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(TOMBSTONING_PATH))
                {
                    store.CreateDirectory(TOMBSTONING_PATH);
                }

                var originalPath = TOMBSTONING_PATH + @"\" + TOMBSTONING_ORIGINAL_FILENAME;

                if (store.FileExists(originalPath))
                {
                    store.DeleteFile(originalPath);
                }

                if (_original != null && _original.Length > 0)
                {
                    using (var file = store.CreateFile(originalPath))
                    {
                        _original.Position = 0;
                        _original.CopyTo(file);

                        file.Flush();
                    }
                }

                var imagePath = TOMBSTONING_PATH + @"\" + TOMBSTONING_IMAGE_FILENAME;

                if (store.FileExists(imagePath))
                {
                    store.DeleteFile(imagePath);
                }

                if (_image != null && _image.Length > 0)
                {
                    using (var file = store.CreateFile(imagePath))
                    {
                        _image.Position = 0;
                        _image.CopyTo(file);

                        file.Flush();

                        if (LibraryPath != null)
                        {
                            IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_LIBRARYPATH_KEY] = LibraryPath;
                        }

                        if (LocalPath != null)
                        {
                            IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_LOCALPATH_KEY] = LocalPath;
                        }

                        if (OriginalPath != null)
                        {
                            IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_ORIGINALPATH_KEY] = OriginalPath;
                        }

                        if (OriginalLibraryPath != null)
                        {
                            IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_ORIGINALLIBRARYPATH_KEY] = OriginalLibraryPath;
                        }
                    }
                }
            }
        }

        public void Untombstone()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var originalPath = TOMBSTONING_PATH + @"\" + TOMBSTONING_ORIGINAL_FILENAME;

                if (store.FileExists(originalPath))
                {
                    using (var file = store.OpenFile(originalPath, FileMode.Open, FileAccess.Read))
                    {
                        var stream = new MemoryStream();

                        try
                        {
                            file.CopyTo(stream);
                            file.Flush();

                            Original = stream;
                        }
                        catch (Exception ex)
                        {
                            stream.Close();
                        }
                    }

                    store.DeleteFile(originalPath);
                }

                var imagePath = TOMBSTONING_PATH + @"\" + TOMBSTONING_IMAGE_FILENAME;

                if (store.FileExists(imagePath))
                {
                    using (var file = store.OpenFile(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        var stream = new MemoryStream();

                        try
                        {
                            file.CopyTo(stream);
                            file.Flush();

                            Image = stream;
                        }
                        catch (Exception ex)
                        {
                            stream.Close();
                        }
                    }

                    store.DeleteFile(imagePath);

                    if (IsolatedStorageSettings.ApplicationSettings.Contains(TOMBSTONING_LIBRARYPATH_KEY))
                    {
                        LibraryPath = IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_LIBRARYPATH_KEY] as string;
                        IsolatedStorageSettings.ApplicationSettings.Remove(TOMBSTONING_LIBRARYPATH_KEY);
                    }

                    if (IsolatedStorageSettings.ApplicationSettings.Contains(TOMBSTONING_LOCALPATH_KEY))
                    {
                        LocalPath = IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_LOCALPATH_KEY] as string;
                        IsolatedStorageSettings.ApplicationSettings.Remove(TOMBSTONING_LOCALPATH_KEY);
                    }

                    if (IsolatedStorageSettings.ApplicationSettings.Contains(TOMBSTONING_ORIGINALPATH_KEY))
                    {
                        OriginalPath = IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_ORIGINALPATH_KEY] as string;
                        IsolatedStorageSettings.ApplicationSettings.Remove(TOMBSTONING_ORIGINALPATH_KEY);
                    }

                    if (IsolatedStorageSettings.ApplicationSettings.Contains(TOMBSTONING_ORIGINALLIBRARYPATH_KEY))
                    {
                        OriginalLibraryPath = IsolatedStorageSettings.ApplicationSettings[TOMBSTONING_ORIGINALLIBRARYPATH_KEY] as string;
                        IsolatedStorageSettings.ApplicationSettings.Remove(TOMBSTONING_ORIGINALLIBRARYPATH_KEY);
                    }
                }
            }
        }

        #endregion Tombstoning methods

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
                    catch (Exception ex)
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
