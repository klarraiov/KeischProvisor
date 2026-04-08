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
using Microsoft.Windows.Storage.Pickers;
using Microsoft.UI.Windowing;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ExperimentalPage : Page
{
    public ExperimentalPage()
    {
        InitializeComponent();
    }

    public static uint GetJoaat(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        string s = input.ToLower();
        uint hash = 0;

        for (int i = 0; i < s.Length; i++)
        {
            hash += s[i];
            hash += (hash << 10);
            hash ^= (hash >> 6);
        }

        hash += (hash << 3);
        hash ^= (hash >> 11);
        hash += (hash << 15);

        return hash;
    }
    private async void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new FolderPicker(((App.Current as App)!._window as Window)!.AppWindow.Id);
        var result = await folderPicker.PickSingleFolderAsync();

        var rpfs = Directory.EnumerateFiles(result.Path, "*.rpf", SearchOption.AllDirectories).ToList();
        Debug.WriteLine(rpfs.Count);
        foreach (var item in rpfs)
        {
            var relativePath = Path.GetRelativePath(result.Path, item).Replace(@"\", @"/");
            Debug.WriteLine("{" + $"{GetJoaat(relativePath)}, \"{relativePath}\"" + "},");
        }
    }
}
