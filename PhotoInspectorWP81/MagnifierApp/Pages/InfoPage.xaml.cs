/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using Microsoft.Phone.Controls;
using System.Windows;
using System.Windows.Navigation;

namespace MagnifierApp.Pages
{
    public partial class InfoPage : PhoneApplicationPage
    {
        private InfoPageViewModel _viewModel = null;

        public InfoPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _viewModel = new InfoPageViewModel();

            AdaptToInfosCollection();

            DataContext = _viewModel;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            DataContext = null;

            if (_viewModel != null)
            {
                _viewModel = null;
            }
        }

        private void AdaptToInfosCollection()
        {
            if (_viewModel.Infos.Count > 0)
            {
                GuidePanel.Visibility = Visibility.Collapsed;
                InfosPanel.Visibility = Visibility.Visible;
            }
            else
            {
                InfosPanel.Visibility = Visibility.Collapsed;
                GuidePanel.Visibility = Visibility.Visible;
            }
        }
    }
}