using KeischProvisor.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        this.Loaded += InitializeSettings;
        NavigationCacheMode = NavigationCacheMode.Required;
    }

    private void InitializeSettings(object sender, RoutedEventArgs e)
    {
        MainPage_StatusBarToggleMenuFlyoutItem.IsChecked = App.AppSettings.IsStatusBarVisible;
        StatusBar.Visibility = App.AppSettings.IsStatusBarVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow mainWindow = (MainWindow)((App)Application.Current)._window!;
        mainWindow.RequestPageTransition(typeof(SettingsPage), null!, new DrillInNavigationTransitionInfo());
    }

    private void StatusBarToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {   
        if (sender is ToggleMenuFlyoutItem toggleMenuFlyoutItem)
        {
            StatusBar.Visibility = toggleMenuFlyoutItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;

            App.AppSettings.IsStatusBarVisible = toggleMenuFlyoutItem.IsChecked;
            SettingsManager.SaveSettings(App.AppSettings);
        }
    }
}
