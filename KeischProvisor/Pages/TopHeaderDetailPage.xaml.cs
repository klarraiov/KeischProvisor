using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Respectre.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TopHeaderDetailPage : Page
    {
        public TopHeaderDetailPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ((MainWindow)((App)App.Current!)._window!).AppTitleBar.IsBackButtonVisible = true;
            ((App.Current as App)!._window!.SystemBackdrop as MicaBackdrop)!.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;

            Debug.WriteLine(((KeischProvisor.Pages.MainPage.HSHRSync)e.Parameter).MainIndex.NameHash);
            Debug.WriteLine(((KeischProvisor.Pages.MainPage.HSHRSync)e.Parameter).MainIndex.DataOffset);
            Debug.WriteLine(((KeischProvisor.Pages.MainPage.HSHRSync)e.Parameter).Data.AllDataSize);

            InitializeView(e);
        }

        private void InitializeView(NavigationEventArgs e)
        {
            //App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_Title").ValueAsString
            string name = $"0x{((KeischProvisor.Pages.MainPage.HSHRSync)e.Parameter).MainIndex.NameHash:X8}";
            TopHeaderDetailPage_Title.Text = string.Format(TopHeaderDetailPage_Title.Text, name);

        }
    }
}
