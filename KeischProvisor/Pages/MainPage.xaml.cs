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
using CommunityToolkit.WinUI.Controls;
using Respectre.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages;

public partial class TopHeaderNavigationInfo : ObservableObject
{
    [ObservableProperty]
    private int index;

    [ObservableProperty]
    private string headerName;

    [ObservableProperty]
    private string resolvedName;

    [ObservableProperty]
    private HSHRFile hSHRFile;
}
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    private Respectre.Utils.HSHRFile? currentHSHRFile;
    private ObservableCollection<TopHeaderNavigationInfo> navigationLists = new ObservableCollection<TopHeaderNavigationInfo>();
    internal sealed record HSHRSync(HSHRFile HSHRFile, int index);
    public MainPage()
    {
        InitializeComponent();
        this.Loaded += InitializeSettings;
        NavigationCacheMode = NavigationCacheMode.Required;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ((MainWindow)((App)App.Current!)._window!).AppTitleBar.IsBackButtonVisible = false;
        ((App.Current as App)!._window!.SystemBackdrop as MicaBackdrop)!.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt;
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
            StatusBarText.Text = string.Format(App.AppResourceManager.MainResourceMap.GetValue("Resources/MainPage_StatusBarTextBlock_FileOpened").ValueAsString, result.Path);
            await InitializeCacheElement(result.Path);
        }
    }

    private void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as SettingsCard)?.DataContext == null)
        {
            return;
        }
        ((App.Current as App)!._window as MainWindow)!.RequestPageTransition(typeof(TopHeaderDetailPage), (sender as SettingsCard).DataContext, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });

    }

    private async Task InitializeCacheElement(string filepath)
    {
        currentHSHRFile = null;
        navigationLists.Clear();

        currentHSHRFile = new Respectre.Utils.HSHRFile(filepath);
        string statusFormat = App.AppResourceManager.MainResourceMap.GetValue("Resources/MainPage_StatusBarTextBlock_HSHRFileStatus").ValueAsString;
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
                        .GetValue("Resources/MainPage_StatusBarTextBlock_FileOpened")
                        .ValueAsString,
                    filepath);
            });
        });

        MainPage_MainGrid_SignatureTextBlock.Text = Encoding.UTF8.GetString(BitConverter.GetBytes(currentHSHRFile.Signature));
        MainPage_MainGrid_BuildTextBlock.Text = $"{currentHSHRFile.VersionMajor}.{currentHSHRFile.VersionMinor} ({currentHSHRFile.Version})";
        MainPage_MainGrid_ChecksumTextBlock.Text = $"0x{currentHSHRFile.Checksum:X16} ({currentHSHRFile.Checksum})";
        MainPage_MainGrid_TopHeadersCountTextBlock.Text = $"{currentHSHRFile.TopHeadersCount}";

        string headernametemplate = App.AppResourceManager.MainResourceMap.GetValue("Resources/MainPage_TopHeaderSettingsCard_Header").ValueAsString;
        for (int i = 0; i < currentHSHRFile.TopHeadersCount; i++)
        {
            int index = i;
            TopHeaderNavigationInfo info = new TopHeaderNavigationInfo
            {
                Index = index,
                HeaderName = string.Format(headernametemplate, index),
                ResolvedName = HSHRFile.namehashPairs.TryGetValue(currentHSHRFile.MainIndex[index].NameHash, out string? resolvedNameinfo) ? resolvedNameinfo : "Unknown",
                HSHRFile = currentHSHRFile,
            };
             
            navigationLists.Add(info);
        }
    }
}

