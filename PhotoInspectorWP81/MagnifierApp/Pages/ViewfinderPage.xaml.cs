/*
 * Copyright © 2013-2014 Microsoft Mobile. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Models;
using MagnifierApp.Resources;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Phone.Media.Capture;

// Suppress warnings from using old Capture API 
#pragma warning disable 0618

namespace MagnifierApp.Pages
{
    public partial class ViewfinderPage : PhoneApplicationPage
    {
        private const CameraSensorLocation SENSOR_LOCATION = CameraSensorLocation.Back;

        private PhotoCaptureDevice _device = null;
        private PhotoChooserTask _photoChooserTask = new PhotoChooserTask();
        private FlashState _flashState = FlashState.Auto;
        private ApplicationBarIconButton _flashButton = null;
        private bool _focusing = false;
        private bool _capturing = false;
        private bool _capture = false;
        private PhotoResult _photoResult = null;
        private bool _picker = false;
        private bool _lens = false;

        #region Properties

        #endregion

        public ViewfinderPage()
        {
            InitializeComponent();

            _photoChooserTask.Completed += PhotoChooserTask_Completed;

            // Flash toggle button

            if (PhotoCaptureDevice.GetSupportedPropertyValues(SENSOR_LOCATION, KnownCameraPhotoProperties.FlashMode).Count > 1)
            {
                _flashButton = new ApplicationBarIconButton()
                {
                    IconUri = new Uri("/Assets/Icons/flash_auto.png", UriKind.Relative),
                    Text = AppResources.ViewfinderPage_FlashButton_Text
                };

                _flashButton.Click += FlashButton_Click;

                ApplicationBar.Buttons.Add(_flashButton);

                ApplicationBar.IsVisible = true;
            }
        }

        ~ViewfinderPage()
        {
            if (_device != null)
            {
                UninitializeCamera();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                if (NavigationContext.QueryString.ContainsKey("picker"))
                {
                    _picker = true;
                }
                else
                {
                    if (NavigationContext.QueryString.ContainsKey("lense"))
                    {
                        _lens = true;
                    }
                    else
                    {
                        // Gallery button

                        var galleryButton = new ApplicationBarIconButton()
                        {
                            IconUri = new Uri("/Assets/Icons/folder.png", UriKind.Relative),
                            Text = AppResources.ViewfinderPage_GalleryButton_Text
                        };

                        galleryButton.Click += PickPhotoButton_Click;

                        ApplicationBar.Buttons.Add(galleryButton);
                    }

                    // About menu item

                    var aboutMenuItem = new ApplicationBarMenuItem()
                    {
                        Text = AppResources.ViewFinderPage_AboutMenuItem_Text
                    };

                    aboutMenuItem.Click += AboutMenuItem_Click;

                    ApplicationBar.MenuItems.Add(aboutMenuItem);

                    ApplicationBar.IsVisible = true;
                }
            }

            if (_photoResult != null)
            {
                PhotoModel.Singleton.FromCameraRollPath(_photoResult.OriginalFileName);

                if (_picker)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    if (_lens)
                    {
                        NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml?editor", UriKind.Relative));
                    }
                    else
                    {
                        NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml", UriKind.Relative));
                    }
                }
            }
            else
            {
                if (_device == null)
                {
                    InitializeCamera();
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if ((_focusing || _capturing) && e.IsCancelable)
            {
                e.Cancel = true;
            }

            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _photoResult = null;

            if (_device != null)
            {
                UninitializeCamera();
            }

            if (FreezeImage.Visibility == Visibility.Visible)
            {
                FreezeImage.Visibility = Visibility.Collapsed;
                FreezeImage.Source = null;
            }
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);
            
            if (_device != null)
            {
                AdaptToOrientation();
            }
        }

        private void FlashButton_Click(object sender, EventArgs e)
        {
            if (_flashState == FlashState.Auto)
            {
                SetFlashState(FlashState.Off);
            }
            else if (_flashState == FlashState.Off)
            {
                SetFlashState(FlashState.On);
            }
            else // FlashState.On
            {
                SetFlashState(FlashState.Auto);
            }
        }

        private void PickPhotoButton_Click(object sender, EventArgs e)
        {
            _photoChooserTask.Show();
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
                    _photoResult = e;
                }
                else
                {
                    var result = MessageBox.Show(AppResources.ViewfinderPage_GalleryReadErrorMessageBox_Text,
                        AppResources.ViewfinderPage_GalleryReadErrorMessageBox_Caption, MessageBoxButton.OKCancel);

                    if (result.HasFlag(MessageBoxResult.OK))
                    {
                        _photoChooserTask.Show();
                    }
                }
            }
        }

        private void AdaptToOrientation()
        {
            if (App.Current.Host.Content.ScaleFactor == 225)
            {
                // 720p
                Canvas.Width = 853;
                FreezeImage.Width = 853;
            }
            else
            {
                Canvas.Width = 800;
                FreezeImage.Width = 800;
            }

            double canvasAngle;

            if (Orientation.HasFlag(PageOrientation.LandscapeLeft))
            {
                canvasAngle = _device.SensorRotationInDegrees - 90;
            }
            else if (Orientation.HasFlag(PageOrientation.LandscapeRight))
            {
                canvasAngle = _device.SensorRotationInDegrees + 90;
            }
            else // PageOrientation.PortraitUp
            {
                canvasAngle = _device.SensorRotationInDegrees;
            }

            Canvas.RenderTransform = new RotateTransform()
            {
                CenterX = Canvas.Width / 2.0,
                CenterY = Canvas.Height / 2.0,
                Angle = canvasAngle
            };

            _device.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation, canvasAngle);
        }

        /// <summary>
        /// Synchronously initializes the photo capture device for a high resolution photo capture.
        /// Viewfinder video stream is sent to a VideoBrush element called ViewfinderVideoBrush, and
        /// device hardware capture key is wired to the CameraButtons_ShutterKeyHalfPressed and
        /// CameraButtons_ShutterKeyPressed methods.
        /// </summary>
        private async void InitializeCamera()
        {
            Windows.Foundation.Size captureResolution;

            var deviceName = DeviceStatus.DeviceName;

            if (deviceName.Contains("RM-875") || deviceName.Contains("RM-876") || deviceName.Contains("RM-877"))
            {
                captureResolution = new Windows.Foundation.Size(7712, 4352); // 16:9
                //captureResolution = new Windows.Foundation.Size(7136, 5360); // 4:3
            }
            else if (deviceName.Contains("RM-937") || deviceName.Contains("RM-938") || deviceName.Contains("RM-939"))
            {
                captureResolution = new Windows.Foundation.Size(5376, 3024); // 16:9
                //captureResolution = new Windows.Foundation.Size(4992, 3744); // 4:3
            }
            else
            {
                captureResolution = PhotoCaptureDevice.GetAvailableCaptureResolutions(SENSOR_LOCATION).First();
            }

            _device = await PhotoCaptureDevice.OpenAsync(SENSOR_LOCATION, captureResolution);
            _device.SetProperty(KnownCameraGeneralProperties.PlayShutterSoundOnCapture, true);

            if (_flashButton != null)
            {
                SetFlashState(_flashState);
            }

            AdaptToOrientation();  

            ViewfinderVideoBrush.SetSource(_device);

            if (PhotoCaptureDevice.IsFocusSupported(SENSOR_LOCATION))
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed += CameraButtons_ShutterKeyHalfPressed;
            }

            Microsoft.Devices.CameraButtons.ShutterKeyPressed += CameraButtons_ShutterKeyPressed;

            System.Diagnostics.Debug.WriteLine("Initialized!");
        }

        /// <summary>
        /// Uninitializes photo capture device and unwires device hardware capture keys.
        /// </summary>
        private void UninitializeCamera()
        {
            if (PhotoCaptureDevice.IsFocusSupported(SENSOR_LOCATION))
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;
            }

            Microsoft.Devices.CameraButtons.ShutterKeyPressed -= CameraButtons_ShutterKeyPressed;

            _device.Dispose();
            _device = null;
        }

        /// <summary>
        /// Asynchronously autofocuses the photo capture device.
        /// </summary>
        private async void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (!_focusing && !_capturing)
            {
                _focusing = true;

                _device.FocusRegion = null;

                await _device.FocusAsync();

                _focusing = false;

                if (_capture)
                {
                    _capture = false;

                    await CaptureAsync();
                }
            }
        }

        private async void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (_focusing)
            {
                _capture = true;
            }
            else
            {
                await CaptureAsync();
            }
        }

        private async void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!_focusing && !_capturing)
            {
                _focusing = true;

                var point = e.GetPosition(Canvas);
                var scaleX = _device.PreviewResolution.Width / Canvas.ActualWidth;
                var scaleY = _device.PreviewResolution.Height / Canvas.ActualHeight;

                // Show focusing bracket and set focus region if supported by the HW

                if (PhotoCaptureDevice.IsFocusRegionSupported(SENSOR_LOCATION))
                {
                    FocusBracket.RenderTransform = new TranslateTransform()
                    {
                        X = point.X,
                        Y = point.Y
                    };

                    FocusBracket.Visibility = Visibility.Visible;

                    _device.FocusRegion = new Windows.Foundation.Rect(point.X * scaleX, point.Y * scaleY, 1, 1);
                }

                // Focus and capture if focus is supported by the HW, otherwise just capture

                if (PhotoCaptureDevice.IsFocusSupported(SENSOR_LOCATION))
                {
                    CameraFocusStatus status;

                    try
                    {
                        status = await _device.FocusAsync();
                    }
                    catch (Exception)
                    {
                        status = CameraFocusStatus.NotLocked;
                    }

                    _focusing = false;

                    FocusBracket.Visibility = Visibility.Collapsed;

                    if (status == CameraFocusStatus.Locked)
                    {
                        await CaptureAsync();
                    }
                }
                else
                {
                    await CaptureAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously captures a frame for further usage.
        /// </summary>
        private async Task CaptureAsync()
        {
            if (!_focusing && !_capturing)
            {
                _capturing = true;

                ProgressBar.IsIndeterminate = true;
                ProgressBar.Visibility = Visibility.Visible;

                var stream = new MemoryStream();

                try
                {
                    var sequence = _device.CreateCaptureSequence(1);
                    sequence.Frames[0].CaptureStream = stream.AsOutputStream();
                    
                    await _device.PrepareCaptureSequenceAsync(sequence);

                    // Freeze preview image to avoid viewfinder showing live feed during/after capture

                    var freezeBitmap = new WriteableBitmap((int)_device.PreviewResolution.Width, (int)_device.PreviewResolution.Height);

                    _device.GetPreviewBufferArgb(freezeBitmap.Pixels);

                    freezeBitmap.Invalidate();

                    FreezeImage.Source = freezeBitmap;
                    FreezeImage.Visibility = Visibility.Visible;

                    await sequence.StartCaptureAsync();
                }
                catch (Exception)
                {
                    stream.Close();
                }

                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;

                _capturing = false;

                if (stream.CanRead)
                {
                    PhotoModel.Singleton.FromNewImage(stream, PhotoOrigin.Camera);

                    if (_picker)
                    {
                        NavigationService.GoBack();
                    }
                    else
                    {
                        if (_lens)
                        {
                            NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml?editor", UriKind.Relative));
                        }
                        else
                        {
                            NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml", UriKind.Relative));
                        }
                    }
                }
                else
                {
                    FreezeImage.Visibility = Visibility.Collapsed;
                    FreezeImage.Source = null;
                }
            }
        }

        private void SetFlashState(FlashState state)
        {
            try
            {
                _device.SetProperty(KnownCameraPhotoProperties.FlashMode, state);

                _flashState = state;

                if (_flashState == FlashState.Auto)
                {
                    _flashButton.IconUri = new Uri("/Assets/Icons/flash_auto.png", UriKind.Relative);
                }
                else if (_flashState == FlashState.On)
                {
                    _flashButton.IconUri = new Uri("/Assets/Icons/flash_on.png", UriKind.Relative);
                }
                else // FlashState.Off
                {
                    _flashButton.IconUri = new Uri("/Assets/Icons/flash_off.png", UriKind.Relative);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}