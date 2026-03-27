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
using Microsoft.Windows.Globalization;
using Microsoft.UI.Xaml.Media.Animation;

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
        AppLanguageComboBox.SelectionChanged += ComboBox_SelectionChanged;
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

        AppLanguageComboBox.SelectedIndex = (int)App.AppSettings.AppLanguage;
    }

    private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is RadioButtons rb)
        {
            ElementTheme elementTheme = rb.SelectedIndex switch
            {
                0 => ElementTheme.Light,
                1 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            ((Application.Current as App)!._window!.Content as FrameworkElement)!.RequestedTheme = elementTheme;

            App.AppSettings.AppTheme = elementTheme;
            SettingsManager.SaveSettings(App.AppSettings);
        }
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            ApplicationLanguages.PrimaryLanguageOverride = SettingsManager.AppLanguagesToTag((AppLanguages)comboBox.SelectedIndex);
        }

        App.AppSettings.AppLanguage = (AppLanguages)AppLanguageComboBox.SelectedIndex;
        SettingsManager.SaveSettings(App.AppSettings);

        //Frame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
        //this.Frame.BackStack.RemoveAt(this.Frame.BackStack.Count - 1);
    }
}
