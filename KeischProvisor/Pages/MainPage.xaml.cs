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
using Microsoft.Windows.Storage.Pickers;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using System.ComponentModel;
using Windows.Storage.Pickers.Provider;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    private Respectre.Utils.HSHRFile? currentHSHRFile;
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

    private async void MainPage_MenuFileOpenMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new FileOpenPicker((sender as FrameworkElement)!.XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add("*");
        picker.FileTypeFilter.Add(".cache");
        var result = await picker.PickSingleFileAsync();

        if (result != null)
        {
            StatusBarText.Text = string.Format(App.AppResourceManager.MainResourceMap.GetValue("Resources/Runtime_MainPage_StatusBarTextBlock_FileOpened").ValueAsString, result.Path);
            await InitializeCacheElement(result.Path);
        }
    }

    private async Task InitializeCacheElement(string filepath)
    {
        testui.Children.Clear();
        currentHSHRFile = null;

        currentHSHRFile = new Respectre.Utils.HSHRFile(filepath);
        string statusFormat = App.AppResourceManager.MainResourceMap.GetValue("Resources/Runtime_MainPage_StatusBarTextBlock_HSHRFileStatus").ValueAsString;
        await Task.Run(() =>
        {
            currentHSHRFile.ScanStructure(status =>
            {
                string text = string.Format(statusFormat, status.Position, status.CurrentIndex, status.TotalTopHeadersCount, status.Stage);
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusBarText.Text = text;
                });
            });

            DispatcherQueue.TryEnqueue(() =>
            {
                StatusBarText.Text = string.Format(
                    App.AppResourceManager.MainResourceMap
                        .GetValue("Resources/Runtime_MainPage_StatusBarTextBlock_FileOpened")
                        .ValueAsString,
                    filepath);
            });
        });

        MainPage_MainGrid_SignatureTextBlock.Text = Encoding.UTF8.GetString(BitConverter.GetBytes(currentHSHRFile.Signature));
        MainPage_MainGrid_BuildTextBlock.Text = $"{currentHSHRFile.Version} ({currentHSHRFile.VersionMajor}.{currentHSHRFile.VersionMinor})";
        MainPage_MainGrid_ChecksumTextBlock .Text = $"0x{currentHSHRFile.Checksum:X16} ({currentHSHRFile.Checksum})";
        MainPage_MainGrid_TopHeadersCountTextBlock.Text = $"{currentHSHRFile.TopHeadersCount}";

        //int previousRowCount = testui.RowDefinitions.Count;
        App.Current.Resources.TryGetValue("GeneralAnimations", out object transistion);
        for (int i = 0; i < currentHSHRFile.TopHeadersCount; i++)
        {
            RowDefinition rowDefinition = new RowDefinition { Height = GridLength.Auto };
            //testui.RowDefinitions.Add(rowDefinition);

            StackPanel sp = new StackPanel
            {
                Margin = new Thickness(10),
                ChildrenTransitions = (TransitionCollection)transistion,
                Children =
                {
                    new Expander
                    {
                        Content = new TextBlock{Text = $"Header {i}"},
                    }
                }
            };
            //Grid.SetRow(sp, previousRowCount + i);
            testui.Children.Add(sp);
        }
    }
}
