using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using KeischProvisor.Utils;
using KeischProvisor;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        InitializeView();

        AppThemeRadioButtons.SelectionChanged += RadioButtons_SelectionChanged;
    }

    private void InitializeView()
    {
        AppThemeRadioButtons.SelectedIndex = App.AppSettings.AppTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            ElementTheme.Default => 2,
            _ => 2
        };
    }

    private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        
        Debug.WriteLine((sender as RadioButtons).SelectedIndex);
        FrameworkElement mainWindow = ((MainWindow)((App)Application.Current)._window).Content as FrameworkElement;
        ElementTheme elementTheme;
        switch ((sender as RadioButtons).SelectedIndex)
        {
            case 0:
                mainWindow.RequestedTheme = ElementTheme.Light;
                elementTheme = ElementTheme.Light;
                break;
            case 1:
                mainWindow.RequestedTheme = ElementTheme.Dark;
                elementTheme = ElementTheme.Dark;
                break;
            case 2:
                mainWindow.RequestedTheme = ElementTheme.Default;
                elementTheme = ElementTheme.Default;
                break;
            default:
                mainWindow.RequestedTheme = ElementTheme.Default;
                elementTheme = ElementTheme.Default;
                break;

        }
        App.AppSettings.AppTheme = elementTheme;
        SettingsManager.SaveSettings(App.AppSettings);
    }
}
