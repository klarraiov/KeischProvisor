using KeischProvisor;
using KeischProvisor.Utils;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Globalization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    private bool _isAppLanguageComboBoxChanged = false;
    private int _appLanguageComboBoxSelectedIndex = -1;
    private int _initialAppLanguageComboBoxSelectedIndex = -1;
    public bool IsAppLanguageComboBoxChanged
    {
        get => _isAppLanguageComboBoxChanged;
        set
        {
            if (_isAppLanguageComboBoxChanged != value)
            {
                _isAppLanguageComboBoxChanged = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SettingsPage()
    {
        InitializeComponent();
        InitializeView();

        AppThemeRadioButtons.SelectionChanged += AppThemeRadioButtons_SelectionChanged;
        AppLanguageComboBox.SelectionChanged += AppLanguageComboBox_SelectionChanged;
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ((MainWindow)((App)App.Current!)._window!).AppTitleBar.IsBackButtonVisible = true;
        ((App.Current as App)!._window!.SystemBackdrop as MicaBackdrop)!.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;
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
        _initialAppLanguageComboBoxSelectedIndex = AppLanguageComboBox.SelectedIndex;
        _appLanguageComboBoxSelectedIndex = AppLanguageComboBox.SelectedIndex;
    }

    private void AppThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

    private void AppLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            if (comboBox.SelectedIndex == _initialAppLanguageComboBoxSelectedIndex)
            {
                IsAppLanguageComboBoxChanged = false;
                return;
            }

            IsAppLanguageComboBoxChanged = true;
            
            Debug.WriteLine(comboBox.SelectedIndex);
            ApplicationLanguages.PrimaryLanguageOverride = SettingsManager.AppLanguagesToTag((AppLanguages)comboBox.SelectedIndex);
            App.AppSettings.AppLanguage = (AppLanguages)comboBox.SelectedIndex;
            Debug.WriteLine(App.AppSettings.AppLanguage);
            SettingsManager.SaveSettings(App.AppSettings);
        }
    }

    private void AppLanguageRestartInfoButton_Click(object sender, RoutedEventArgs e)
    {
        ((App)App.Current).RestartApp();
    }

    private void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        MainWindow mainWindow = (MainWindow)((App)Application.Current)._window!;
        mainWindow.RequestPageTransition(typeof(ExperimentalPage), null!, new SuppressNavigationTransitionInfo());
    }
}
