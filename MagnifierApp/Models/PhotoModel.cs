/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

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
        private const string TOMBSTONING_IMAGE_FILENAME = @"image.jpeg";
        private const string TOMBSTONING_LIBRARYPATH_KEY = "PhotoModel.LibraryPath";
        private const string TOMBSTONING_LOCALPATH_KEY = "PhotoModel.LocalPath";
        private const string LOCALS_PATH = @"\LocalImages";

        private static PhotoModel _instance = null;

        private Stream _image = null;
        private string _libraryPath = null;
        private string _localPath = null;
        private Size _imageSize = new Size(0, 0);

        #endregion Members

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public const string ImagePropertyName = "Image";
        public const string LibraryPathPropertyName = "LibraryPath";
        public const string LocalPathPropertyName = "LocalPath";

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

                    System.Diagnostics.Debug.WriteLine("PhotoModel.LibraryPath is \"" + LibraryPath + "\"");

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

                    System.Diagnostics.Debug.WriteLine("PhotoModel.LocalPath is \"" + LocalPath + "\"");

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(LocalPathPropertyName));
                    }
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
                LocalPath = MatchLibraryPathWithLocalPath(libraryPath);

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
                        LocalPath = MatchLibraryPathWithLocalPath(libraryPath);

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
                LibraryPath = MatchLocalPathWithLibraryPath(localPath);
                LocalPath = localPath;
                Image = image;
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
            LibraryPath = null;
            LocalPath = null;
        }

        public void Clear()
        {
            Image = null;
            LibraryPath = null;
            LocalPath = null;
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

                var filenameBase = "photoinspector_" + DateTime.UtcNow.Ticks.ToString();

                if (resizeConfiguration != null)
                {
                    // Store high resolution original to application local storage

                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(LOCALS_PATH))
                        {
                            store.CreateDirectory(LOCALS_PATH);
                        }

                        var localPath = LOCALS_PATH + @"\" + filenameBase + @".jpg";

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
        public void CleanLocals()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.DirectoryExists(LOCALS_PATH))
                {
                    var array = store.GetFileNames(LOCALS_PATH + @"\*");

                    using (var library = new MediaLibrary())
                    {
                        using (var pictures = library.Pictures)
                        {
                            foreach (var localFilename in array)
                            {
                                var found = false;

                                for (int i = 0; i < pictures.Count && !found; i++)
                                {
                                    using (var picture = pictures[i])
                                    {
                                        var libraryFilename = FilenameFromPath(picture.GetPath());

                                        if (localFilename == libraryFilename)
                                        {
                                            found = true;
                                        }
                                    }
                                }

                                if (!found)
                                {
                                    var localPath = LOCALS_PATH + @"\" + localFilename;

                                    store.DeleteFile(localPath);

                                    System.Diagnostics.Debug.WriteLine("PhotoModel.CleanLocals deleted local \"" + localPath + "\"");

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

                var path = TOMBSTONING_PATH + @"\" + TOMBSTONING_IMAGE_FILENAME;

                if (store.FileExists(path))
                {
                    store.DeleteFile(path);
                }

                if (_image != null && _image.Length > 0)
                {
                    using (var file = store.CreateFile(path))
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
                    }
                }
            }
        }

        public void Untombstone()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var path = TOMBSTONING_PATH + @"\" + TOMBSTONING_IMAGE_FILENAME;

                if (store.FileExists(path))
                {
                    using (var file = store.OpenFile(path, FileMode.Open, FileAccess.Read))
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

                    store.DeleteFile(path);

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
                }
            }
        }

        #endregion Tombstoning methods

        #region Private methods

        /// <summary>
        /// Takes a full localPath to a file and returns the last localPath component.
        /// </summary>
        /// <param name="localPath">Path</param>
        /// <returns>Last component of the given localPath</returns>
        private static string FilenameFromPath(string path)
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

        private string MatchLocalPathWithLibraryPath(string localPath)
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
