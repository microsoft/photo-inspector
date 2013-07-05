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
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MagnifierApp.Pages
{
    public partial class PhotosPage : PhoneApplicationPage
    {
        private PhotosPageViewModel _viewModel = null;
        private PhotoChooserTask _photoChooserTask = new PhotoChooserTask();
        private ShareMediaTask _shareMediaTask = new ShareMediaTask();
        private ApplicationBarMenuItem _deleteAllMenuItem = null;

        public PhotosPage()
        {
            InitializeComponent();

            _photoChooserTask.Completed += PhotoChooserTask_Completed;

            // Gallery button

            var pickPhotoButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Assets/Icons/folder.png", UriKind.Relative),
                Text = AppResources.ViewfinderPage_PickPhotoButton_Text
            };

            pickPhotoButton.Click += PickPhotoButton_Click;

            ApplicationBar.Buttons.Add(pickPhotoButton);

            // Camera button

            var cameraButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Assets/Icons/camera.png", UriKind.Relative),
                Text = AppResources.PhotosPage_CameraButton_Text
            };

            cameraButton.Click += CameraButton_Click;

            ApplicationBar.Buttons.Add(cameraButton);

            // Delete all menu item

            _deleteAllMenuItem = new ApplicationBarMenuItem()
            {
                Text = AppResources.PhotosPage_DeleteAllMenuItem_Text
            };

            _deleteAllMenuItem.Click += DeleteAllMenuItem_Click;

            ApplicationBar.MenuItems.Add(_deleteAllMenuItem);

            // About menu item

            var aboutMenuItem = new ApplicationBarMenuItem()
            {
                Text = AppResources.PhotosPage_AboutMenuItem_Text
            };

            aboutMenuItem.Click += AboutMenuItem_Click;

            ApplicationBar.MenuItems.Add(aboutMenuItem);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhotoModel.Singleton.Image != null)
            {
                NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml", UriKind.Relative));
            }
            else
            {
                _viewModel = new PhotosPageViewModel();
                _viewModel.Photos.CollectionChanged += Photos_CollectionChanged;

                AdaptToPhotosCollection();

                DataContext = _viewModel;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            DataContext = null;

            if (_viewModel != null)
            {
                _viewModel.Photos.CollectionChanged -= Photos_CollectionChanged;
                _viewModel = null;
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

                _deleteAllMenuItem.IsEnabled = true;
            }
            else
            {
                PhotosPanel.Visibility = Visibility.Collapsed;
                GuidePanel.Visibility = Visibility.Visible;

                _deleteAllMenuItem.IsEnabled = false;
            }
        }

        private void DeleteAllMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(AppResources.PhotosPage_DeleteAllMessageBox_Text,
                AppResources.PhotosPage_DeleteAllMessageBox_Caption, MessageBoxButton.OKCancel);

            if (result.HasFlag(MessageBoxResult.OK))
            {
                _viewModel.DeleteAllPhotos();
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

        private void PickPhotoButton_Click(object sender, EventArgs e)
        {
            _photoChooserTask.Show();
        }

        private void PhotoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                if (e.ChosenPhoto.CanRead && e.ChosenPhoto.Length > 0)
                {
                    PhotoModel.Singleton.FromLibraryImage(e.OriginalFileName, e.ChosenPhoto);
                }
                else
                {
                    var result = MessageBox.Show(AppResources.ViewfinderPage_PickPhotoReadErrorMessageBox_Text,
                        AppResources.ViewfinderPage_PickPhotoReadErrorMessageBox_Caption, MessageBoxButton.OKCancel);

                    if (result.HasFlag(MessageBoxResult.OK))
                    {
                        _photoChooserTask.Show();
                    }
                }
            }
        }

        private void Thumbnail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var image = sender as Image;

            System.Diagnostics.Debug.Assert(image != null);

            var photo = image.Tag as PhotosPageViewModel.Photo;

            System.Diagnostics.Debug.Assert(photo != null);

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var file = store.OpenFile(photo.LocalPath, FileMode.Open);

                PhotoModel.Singleton.FromLocalImage(photo.LocalPath, file);

                NavigationService.Navigate(new Uri("/Pages/MagnifierPage.xaml", UriKind.Relative));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            System.Diagnostics.Debug.Assert(menuItem != null);

            var photo = menuItem.CommandParameter as PhotosPageViewModel.Photo;

            System.Diagnostics.Debug.Assert(photo != null);

            _viewModel.DeletePhoto(photo);
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            System.Diagnostics.Debug.Assert(menuItem != null);

            var photo = menuItem.CommandParameter as PhotosPageViewModel.Photo;

            System.Diagnostics.Debug.Assert(photo != null);

            _shareMediaTask.FilePath = photo.LibraryPath;
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