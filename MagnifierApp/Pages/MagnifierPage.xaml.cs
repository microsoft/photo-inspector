/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Models;
using MagnifierApp.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Nokia.Graphics.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MagnifierApp
{
    public partial class MagnifierPage : PhoneApplicationPage
    {
        private const double LENSE_OFFSET = 150.0;
        private const double DIGITAL_MAGNIFICATION = 1.0;

        private TranslateTransform _lenseTransform = new TranslateTransform();
        private CompositeTransform _lowResolutionBrushTransform = new CompositeTransform();
        private BitmapImage _lowResolutionBitmap = new BitmapImage();
        private WriteableBitmap _highResolutionCropBitmap = null;
        private PhotoChooserTask _photoChooserTask = new PhotoChooserTask();
        private ShareMediaTask _shareMediaTask = new ShareMediaTask();
        private ApplicationBarIconButton _saveButton = null;
        private Point _lastLenseCenterForRendering = new Point(0, 0);
        private bool _renderingLenseContent = false;
        private bool _saving = false;
        private bool _editor = false;
        private ApplicationBarMenuItem _revertMenuItem = null;

        private BufferImageSource _source = null;
        private ImageProviderInfo _info = null;
        private WriteableBitmapRenderer _renderer = null;
        private CropFilter _cropFilter = null;
        private FilterEffect _filterEffect = null;

        public MagnifierPage()
        {
            InitializeComponent();

            _photoChooserTask.Completed += PhotoChooserTask_Completed;

            PhotoModel.Singleton.PropertyChanged += PhotoModel_PropertyChanged;

            // Save button

            _saveButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Assets/Icons/save.png", UriKind.Relative),
                Text = AppResources.MagnifierPage_SaveButton_Text
            };

            _saveButton.Click += SaveButton_Click;

            ApplicationBar.Buttons.Add(_saveButton);

            // Crop button

            var cropButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Assets/Icons/crop.png", UriKind.Relative),
                Text = AppResources.MagnifierPage_CropButton_Text
            };

            cropButton.Click += CropButton_Click;

            ApplicationBar.Buttons.Add(cropButton);

            // Info button

            var infoButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Assets/Icons/info.png", UriKind.Relative),
                Text = AppResources.MagnifierPage_InfoButton_Text
            };

            infoButton.Click += InfoButton_Click;

            ApplicationBar.Buttons.Add(infoButton);

            // Share button

            var shareButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Assets/Icons/share.png", UriKind.Relative),
                Text = AppResources.MagnifierPage_ShareButton_Text
            };

            shareButton.Click += ShareButton_Click;

            ApplicationBar.Buttons.Add(shareButton);

            // Lense is positioned to current touch point with _lenseTransform
            Lense.RenderTransform = _lenseTransform;

            // PreviewImage displays the whole photo fitted into the ContentPanel
            PreviewImage.Source = _lowResolutionBitmap;

            // LowResolutionCropBrush is used to quickly show the loupe in current touch point
            LowResolutionCropBrush.ImageSource = _lowResolutionBitmap;
            LowResolutionCropBrush.Transform = _lowResolutionBrushTransform;

            // Create bitmap for HighResolutionCropImage taking into account the actual screen resolution
            var screenScaleFactor = App.Current.Host.Content.ScaleFactor / 100.0;
            var bitmapWidth = (int)(LenseContent.Width * screenScaleFactor);
            var bitmapHeight = (int)(LenseContent.Height * screenScaleFactor);

            _highResolutionCropBitmap = new WriteableBitmap(bitmapWidth, bitmapHeight);

            HighResolutionCropImage.Source = _highResolutionCropBitmap;
            HighResolutionCropImage.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, LenseContent.Width, LenseContent.Height),
                RadiusX = 360,
                RadiusY = 360
            };
        }

        private void PhotoModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PhotoModel.LocalPathPropertyName ||
                e.PropertyName == PhotoModel.LibraryPathPropertyName ||
                e.PropertyName == PhotoModel.OriginalPathPropertyName)
            {
                // Source information for photo may have changed, update information panel
                SetupInformationPanel();
            }
            else if (e.PropertyName == PhotoModel.ImagePropertyName)
            {
                EndSession();

                if (PhotoModel.Singleton.Image != null)
                {
                    BeginSession(PhotoModel.Singleton.Image);
                }
            }

            // Disable save button if photo already exists in library
            _saveButton.IsEnabled = PhotoModel.Singleton.LibraryPath == null;

            // Enable revert to original button if there is an original to revert to
            _revertMenuItem.IsEnabled = PhotoModel.Singleton.Original != null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                var queryString = this.NavigationContext.QueryString;

                string token = null;

                if (queryString.ContainsKey("token"))
                {
                    _editor = true;

                    token = queryString["token"];
                }
                else if (queryString.ContainsKey("FileId"))
                {
                    _editor = true;

                    token = queryString["FileId"];
                }
                else if (queryString.ContainsKey("editor"))
                {
                    _editor = true;
                }

                if (!_editor)
                {
                    // Camera menu item

                    var cameraMenuItem = new ApplicationBarMenuItem()
                    {
                        Text = AppResources.MagnifierPage_CameraMenuItem_Text
                    };

                    cameraMenuItem.Click += CameraMenuItem_Click;

                    ApplicationBar.MenuItems.Add(cameraMenuItem);

                    // Gallery menu item

                    var galleryMenuItem = new ApplicationBarMenuItem()
                    {
                        Text = AppResources.MagnifierPage_GalleryMenuItem_Text
                    };

                    galleryMenuItem.Click += GalleryMenuItem_Click;

                    ApplicationBar.MenuItems.Add(galleryMenuItem);

                    // Local photos menu item

                    var photosMenuItem = new ApplicationBarMenuItem()
                    {
                        Text = AppResources.MagnifierPage_PhotosMenuItem_Text
                    };

                    photosMenuItem.Click += PhotosMenuItem_Click;

                    ApplicationBar.MenuItems.Add(photosMenuItem);
                }

                // Revert menu item

                _revertMenuItem = new ApplicationBarMenuItem()
                {
                    Text = AppResources.MagnifierPage_RevertMenuItem_Text
                };

                _revertMenuItem.Click += RevertMenuItem_Click;

                ApplicationBar.MenuItems.Add(_revertMenuItem);

                // About menu item

                var aboutMenuItem = new ApplicationBarMenuItem()
                {
                    Text = AppResources.MagnifierPage_AboutMenuItem_Text
                };

                aboutMenuItem.Click += AboutMenuItem_Click;

                ApplicationBar.MenuItems.Add(aboutMenuItem);

                if (token != null)
                {
                    PhotoModel.Singleton.FromLibraryImage(token);
                }
            }

            _saveButton.IsEnabled = PhotoModel.Singleton.LibraryPath == null;
            _revertMenuItem.IsEnabled = PhotoModel.Singleton.Original != null;

            BeginSession(PhotoModel.Singleton.Image);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_saving && e.IsCancelable)
            {
                e.Cancel = true;
            }

            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            EndSession();

            if (e.NavigationMode == NavigationMode.Back)
            {
                PhotoModel.Singleton.Clear();
            }
        }

        private void BeginSession(Stream image)
        {
            // Initialize session with image

            using (var memoryStream = new MemoryStream())
            {
                image.Position = 0;
                image.CopyTo(memoryStream);

                try
                {
                    // Some streams do not support flushing

                    image.Flush();
                }
                catch (Exception ex)
                {
                }

                memoryStream.Position = 0;

                // Initialize image source

                _source = new BufferImageSource(memoryStream.GetWindowsRuntimeBuffer());

                // Get image info

                Task.Run(async () => { _info = await _source.GetInfoAsync(); }).Wait();

                // Create crop filter effect

                _cropFilter = new CropFilter();

                _filterEffect = new FilterEffect(_source)
                {
                    Filters = new List<IFilter>() { _cropFilter }
                };

                Task.Run(async () => { await _filterEffect.PreloadAsync(); }).Wait();

                // Create renderer

                _renderer = new WriteableBitmapRenderer(_filterEffect, _highResolutionCropBitmap);
            }

            // Set _lowResolutionBitmap decoding to a quite low resolution and initialize it with image
            if (_info.ImageSize.Width >= _info.ImageSize.Height)
            {
                _lowResolutionBitmap.DecodePixelWidth = 1536;
                _lowResolutionBitmap.DecodePixelHeight = 0;
            }
            else
            {
                _lowResolutionBitmap.DecodePixelWidth = 0;
                _lowResolutionBitmap.DecodePixelHeight = 1536;
            }

            image.Position = 0;

            _lowResolutionBitmap.SetSource(image);

            // Set LowResolutionCropBrush scaling so that it matches with the pixel perfect HighResolutionCropImage renderings
            var screenScaleFactor = App.Current.Host.Content.ScaleFactor / 100.0;
            var lowResolutionToHighResolutionCropScale = _info.ImageSize.Width / _lowResolutionBitmap.PixelWidth / screenScaleFactor * DIGITAL_MAGNIFICATION;

            _lowResolutionBrushTransform.ScaleX = lowResolutionToHighResolutionCropScale;
            _lowResolutionBrushTransform.ScaleY = lowResolutionToHighResolutionCropScale;

            // Show photo information in InformationTextBlock
            SetupInformationPanel();
        }

        private void EndSession()
        {
            if (_renderer != null)
            {
                _renderer.Dispose();
                _renderer = null;
            }
            if (_filterEffect != null)
            {
                _filterEffect.Dispose();
                _filterEffect = null;
            }

            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            _cropFilter = null;
            _info = null;
        }

        private void SetupInformationPanel()
        {
            if (PhotoModel.Singleton.LocalPath != null && PhotoModel.Singleton.OriginalPath != null && PhotoModel.Singleton.LibraryPath != null)
            {
                InformationTextBlock.Text = AppResources.MagnifierPage_InformationTextBlock_LocalAndOriginalAndLibraryText;
            }
            else if (PhotoModel.Singleton.OriginalPath != null && PhotoModel.Singleton.LibraryPath != null)
            {
                InformationTextBlock.Text = AppResources.MagnifierPage_InformationTextBlock_OriginalAndLibraryText;
            }
            else if (PhotoModel.Singleton.LocalPath != null && PhotoModel.Singleton.LibraryPath != null)
            {
                InformationTextBlock.Text = AppResources.MagnifierPage_InformationTextBlock_LocalAndLibraryText;
            }
            else if (PhotoModel.Singleton.LibraryPath != null)
            {
                InformationTextBlock.Text = AppResources.MagnifierPage_InformationTextBlock_LibraryText;
            }
            else
            {
                InformationTextBlock.Text = AppResources.MagnifierPage_InformationTextBlock_UnsavedText;
            }

            if (_source != null)
            {
                ResolutionTextBlock.Text = String.Format("{0} x {1}", _info.ImageSize.Width, _info.ImageSize.Height);
            }
            else
            {
                ResolutionTextBlock.Text = "";
            }
        }

        private void Image_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            InformationPanel.Visibility = Visibility.Collapsed;
            Lense.Visibility = Visibility.Visible;

            Magnificate(e.ManipulationOrigin);
        }

        private void Image_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            Magnificate(e.ManipulationOrigin);
        }

        private void Image_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            HighResolutionCropImage.Visibility = Visibility.Collapsed;
            Lense.Visibility = Visibility.Collapsed;
            InformationPanel.Visibility = Visibility.Visible;
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (!_saving)
            {
                _saving = true;

                ProgressBar.IsIndeterminate = true;
                ProgressBar.Visibility = Visibility.Visible;

                try
                {
                    await PhotoModel.Singleton.SaveAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(AppResources.MagnifierPage_SavePhotoErrorMessageBox_Text,
                        AppResources.MagnifierPage_SavePhotoErrorMessageBox_Caption, MessageBoxButton.OK);
                }

                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;

                _saving = false;
            }
        }

        private async void ShareButton_Click(object sender, EventArgs e)
        {
            if (!_saving)
            {
                _saving = true;

                ProgressBar.IsIndeterminate = true;
                ProgressBar.Visibility = Visibility.Visible;

                try
                {
                    await PhotoModel.Singleton.SaveAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(AppResources.MagnifierPage_SavePhotoErrorMessageBox_Text,
                        AppResources.MagnifierPage_SavePhotoErrorMessageBox_Caption, MessageBoxButton.OK);
                }

                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;

                _saving = false;

                _shareMediaTask.FilePath = PhotoModel.Singleton.LibraryPath;
                _shareMediaTask.Show();
            }
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/InfoPage.xaml", UriKind.Relative));
        }

        private void CropButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/CropPage.xaml", UriKind.Relative));
        }

        private void CameraMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/ViewfinderPage.xaml?picker", UriKind.Relative));
        }
        
        private void GalleryMenuItem_Click(object sender, EventArgs e)
        {
            _photoChooserTask.Show();
        }

        private void PhotosMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/PhotosPage.xaml?picker", UriKind.Relative));
        }

        private void RevertMenuItem_Click(object sender, EventArgs e)
        {
            PhotoModel.Singleton.RevertOriginal();
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
        }

        private void PhotoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                if (e.ChosenPhoto.CanRead && e.ChosenPhoto.Length > 0)
                {
                    PhotoModel.Singleton.FromLibraryImage(e.OriginalFileName, e.ChosenPhoto);

                    _lowResolutionBitmap.SetSource(PhotoModel.Singleton.Image);
                }
                else
                {
                    var result = MessageBox.Show(AppResources.MagnifierPage_GalleryReadErrorMessageBox_Text,
                        AppResources.MagnifierPage_GalleryReadErrorMessageBox_Caption, MessageBoxButton.OKCancel);

                    if (result.HasFlag(MessageBoxResult.OK))
                    {
                        _photoChooserTask.Show();
                    }
                }
            }
        }

        private void Magnificate(Point center)
        {
            // Clamp touch center point to image area
            var clampedCenter = new Point();
            clampedCenter.X = Math.Max(0, center.X);
            clampedCenter.X = Math.Min(clampedCenter.X, PreviewImage.ActualWidth);
            clampedCenter.Y = Math.Max(0, center.Y);
            clampedCenter.Y = Math.Min(clampedCenter.Y, PreviewImage.ActualHeight);

            // Find a lense center x coordinate so that the lense fits in the content panel
            var lenseMinX = Lense.Width / 2;
            var lenseMaxX = ContentPanel.ActualWidth - Lense.Width / 2;

            var lenseCandidateX = clampedCenter.X + (ContentPanel.ActualWidth - PreviewImage.ActualWidth) / 2;
            lenseCandidateX = Math.Max(lenseMinX, lenseCandidateX);
            lenseCandidateX = Math.Min(lenseCandidateX, lenseMaxX);

            _lenseTransform.X = lenseCandidateX;

            // Find a lense center y coordinate so that the lense fits in the content panel
            var lenseMinY = Lense.Height / 2;
            var lenseMaxY = ContentPanel.ActualHeight - Lense.Height / 2;

            var lenseCandidateY = clampedCenter.Y + (ContentPanel.ActualHeight - PreviewImage.ActualHeight) / 2 - LENSE_OFFSET;
            lenseCandidateY = Math.Max(lenseMinY, lenseCandidateY);
            lenseCandidateY = Math.Min(lenseCandidateY, lenseMaxY);

            _lenseTransform.Y = lenseCandidateY;

            // Scale between the rendered image element and the bitmap displayed in it
            var previewToLowResolutionCropScale = _lowResolutionBitmap.PixelWidth / PreviewImage.ActualWidth;

            // Adjust scale transform coordinate and translate image brush coordinate so that the correct low resolution image area is displayed in the lense
            _lowResolutionBrushTransform.CenterX = clampedCenter.X * previewToLowResolutionCropScale;
            _lowResolutionBrushTransform.CenterY = clampedCenter.Y * previewToLowResolutionCropScale;
            _lowResolutionBrushTransform.TranslateX = -clampedCenter.X * previewToLowResolutionCropScale + LenseContent.Width / 2;
            _lowResolutionBrushTransform.TranslateY = -clampedCenter.Y * previewToLowResolutionCropScale + LenseContent.Height / 2;

            if (_lowResolutionBrushTransform.ScaleX > 1)
            {
                // _lowResolutionBitmap is scaled up for the lense, so start rendering a higher resolution crop image
                RenderLenseContentAsync(clampedCenter);
            }
        }

        private async void RenderLenseContentAsync(Point center)
        {
            _lastLenseCenterForRendering = center;

            if (!_renderingLenseContent)
            {
                _renderingLenseContent = true;

                HighResolutionCropImage.Visibility = Visibility.Collapsed;

                do
                {
                    center = _lastLenseCenterForRendering;

                    // Scale between the rendered image element and the bitmap displayed in it
                    var previewToHighResolutionCropScale = _info.ImageSize.Width / PreviewImage.ActualWidth;
                    var screenScaleFactor = App.Current.Host.Content.ScaleFactor / 100.0;

                    // Find crop area top left coordinate in the actual high resolution image
                    var topLeftX = center.X * previewToHighResolutionCropScale - LenseContent.Width / 2 * screenScaleFactor / DIGITAL_MAGNIFICATION;
                    var topLeftY = center.Y * previewToHighResolutionCropScale - LenseContent.Height / 2 * screenScaleFactor / DIGITAL_MAGNIFICATION;

                    // Find crop area bottom right coordinate in the actual high resolution image
                    var bottomRightX = center.X * previewToHighResolutionCropScale + LenseContent.Width / 2 * screenScaleFactor / DIGITAL_MAGNIFICATION;
                    var bottomRightY = center.Y * previewToHighResolutionCropScale + LenseContent.Height / 2 * screenScaleFactor / DIGITAL_MAGNIFICATION;

                    var topLeft = new Windows.Foundation.Point(topLeftX, topLeftY);
                    var bottomRight = new Windows.Foundation.Point(bottomRightX, bottomRightY);

                    _cropFilter.CropArea = new Windows.Foundation.Rect(topLeft, bottomRight);

                    await _renderer.RenderAsync();
                }
                while (_lastLenseCenterForRendering != center);

                _highResolutionCropBitmap.Invalidate();

                HighResolutionCropImage.Visibility = Visibility.Visible;

                _renderingLenseContent = false;
            }
        }
    }
}