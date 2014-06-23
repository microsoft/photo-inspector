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
using Microsoft.Xna.Framework.Media;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Windows.Storage;

namespace MagnifierApp.Pages
{
    public partial class PhotosPage : PhoneApplicationPage
    {
        private PhotosPageViewModel _viewModel = null;
        private PhotoChooserTask _photoChooserTask = new PhotoChooserTask();
        private ShareMediaTask _shareMediaTask = new ShareMediaTask();
        private PhotoResult _photoResult = null;
        private bool _picker = false;

        public PhotosPage()
        {
            InitializeComponent();

            _photoChooserTask.Completed += PhotoChooserTask_Completed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                if (NavigationContext.QueryString.ContainsKey("picker"))
                {
                    _picker = true;

                    GuideTextBlock.Text = AppResources.PhotosPage_PickerGuideTextBlock_Text;
                }
                else
                {
                    GuideTextBlock.Text = AppResources.PhotosPage_StandaloneGuideTextBlock_Text;

                    // Gallery button

                    var galleryButton = new ApplicationBarIconButton()
                    {
                        IconUri = new Uri("/Assets/Icons/folder.png", UriKind.Relative),
                        Text = AppResources.ViewfinderPage_GalleryButton_Text
                    };

                    galleryButton.Click += GalleryButton_Click;

                    ApplicationBar.Buttons.Add(galleryButton);

                    // Camera button

                    var cameraButton = new ApplicationBarIconButton()
                    {
                        IconUri = new Uri("/Assets/Icons/camera.png", UriKind.Relative),
                        Text = AppResources.PhotosPage_CameraButton_Text
                    };

                    cameraButton.Click += CameraButton_Click;

                    ApplicationBar.Buttons.Add(cameraButton);

                    // About menu item

                    var aboutMenuItem = new ApplicationBarMenuItem()
                    {
                        Text = AppResources.PhotosPage_AboutMenuItem_Text
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
                    NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml", UriKind.Relative));
                }
            }
            else
            {
                Microsoft.Devices.CameraButtons.ShutterKeyPressed += CameraButtons_ShutterKeyPressed;

                _viewModel = new PhotosPageViewModel();
                _viewModel.Photos.CollectionChanged += Photos_CollectionChanged;

                DataContext = _viewModel;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Microsoft.Devices.CameraButtons.ShutterKeyPressed -= CameraButtons_ShutterKeyPressed;

            DataContext = null;

            _photoResult = null;

            if (_viewModel != null)
            {
                _viewModel.Photos.CollectionChanged -= Photos_CollectionChanged;
                _viewModel = null;
            }
        }

        private void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (!_picker)
            {
                NavigationService.Navigate(new Uri("/Pages/ViewfinderPage.xaml", UriKind.Relative));
            }
        }

        private void Photos_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            AdaptToPhotosCollection();
        }

        private void AdaptToPhotosCollection()
        {
            if (_viewModel.Photos.Count > 0)
            {                
                GuidePanel.Visibility = Visibility.Collapsed;
                PhotosPanel.Visibility = Visibility.Visible;
            }
            else if (_viewModel.Initialized)
            {
                PhotosPanel.Visibility = Visibility.Collapsed;
                GuidePanel.Visibility = Visibility.Visible;
            }
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
        }

        private void CameraButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/ViewfinderPage.xaml", UriKind.Relative));
        }

        private void GalleryButton_Click(object sender, EventArgs e)
        {
            _photoChooserTask.Show();
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

        private async void Thumbnail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var image = sender as Image;           

            var photo = image.Tag as PhotosPageViewModel.Photo;

            StorageFile file = await StorageFile.GetFileFromPathAsync(photo.Path);
            Stream stream = await file.OpenStreamForReadAsync();
            var originalPath = photo.OriginalPath;

            if (originalPath == null)
            {
                PhotoModel.Singleton.FromCameraRollPath(photo.Path);
            }
            else
            {
                PhotoModel.Singleton.FromSavedPicturesPath(photo.Path, originalPath);
            }

            if (_picker)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml", UriKind.Relative));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            System.Diagnostics.Debug.Assert(menuItem != null);

            var photo = menuItem.CommandParameter as PhotosPageViewModel.Photo;

            System.Diagnostics.Debug.Assert(photo != null);

#pragma warning disable 4014
            _viewModel.DeletePhoto(photo);
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            System.Diagnostics.Debug.Assert(menuItem != null);

            var photo = menuItem.CommandParameter as PhotosPageViewModel.Photo;

            System.Diagnostics.Debug.Assert(photo != null);

            _shareMediaTask.FilePath = photo.Path;
            _shareMediaTask.Show();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = false;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = true;
        }
    }
}