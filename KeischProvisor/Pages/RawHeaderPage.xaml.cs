using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RawHeaderPage : Page
    {
        private MainPage.HSHRSync? currentHSHRSync;
        //public BinaryReader binaryReader;
        public RawHeaderPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ((MainWindow)((App)App.Current!)._window!).AppTitleBar.IsBackButtonVisible = true;
            ((App.Current as App)!._window!.SystemBackdrop as MicaBackdrop)!.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;

            InitializeView(e);
        }

        private void InitializeView(NavigationEventArgs e)
        {
            if (e.Parameter is not KeischProvisor.Pages.MainPage.HSHRSync)
            {
                Debug.WriteLine("Invalid parameter. Expected HSHRSync.");
                return;
            }

            currentHSHRSync = e.Parameter as MainPage.HSHRSync;
            RawHeaderPage_Title.Text = string.Format(RawHeaderPage_Title.Text, currentHSHRSync!.index);
            int i = currentHSHRSync!.index;
            var header = currentHSHRSync.HSHRFile.Data[i].Header;

            var hexLines = new List<string>();
            for (int j = 0; j < header.Length; j += 16)
            {
                string x = BitConverter.ToString(header, j, Math.Min(16, header.Length - j)).Replace("-", " ");
                hexLines.Add(x);
            }

            testui.Text = string.Join(Environment.NewLine, hexLines);
        }
        //private async void testting()
        //{
        //    FileOpenPicker fileOpenPicker = new FileOpenPicker(((App)App.Current)._window.AppWindow.Id);
        //    var result = await fileOpenPicker.PickSingleFileAsync();

        //    if (result != null) 
        //    {
        //        var fs = new FileStream(result.Path, FileMode.Open, FileAccess.Read);

        //        binaryReader = new BinaryReader(File.OpenRead(result.Path));
        //        testHexBox.DataSource = binaryReader;

        //    }
        //}

    }
}
