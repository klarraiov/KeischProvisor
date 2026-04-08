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


            InitializeView(e);
        }

        private void InitializeView(NavigationEventArgs e)
        {
            if (e.Parameter is not KeischProvisor.Pages.MainPage.HSHRSync)
            {
                Debug.WriteLine("Invalid parameter. Expected HSHRSync.");
                return;
            }
            MainPage.HSHRSync hshr = (MainPage.HSHRSync)e.Parameter;
            TopHeaderDetailPage_Title.Text = string.Format(TopHeaderDetailPage_Title.Text, hshr.index);
            TopHeaderDetailPage_HSHRMainIndexSettingsExpander.Header = string.Format((string)TopHeaderDetailPage_HSHRMainIndexSettingsExpander.Header, hshr.index);
            TopHeaderDetailPage_HSHRDataSettingsExpander.Header = string.Format((string)TopHeaderDetailPage_HSHRDataSettingsExpander.Header, hshr.index);

            HSHRIndex mainIndex = hshr.HSHRFile.MainIndex[hshr.index];
            HSHRData data = hshr.HSHRFile.Data[hshr.index];

            string resolvedName = HSHRFile.namehashPairs.TryGetValue(mainIndex.NameHash, out string name) ? name : "Unknown";
            bool isSubheaderConsistent = data.SubheadersCount == data.SubheaderIndex.Count && data.SubheadersCount == data.Subheader.Count;

            TopHeaderDetailPage_PropertiesSettingsCard_ResolvedName.Content = resolvedName;
            TopHeaderDetailPage_PropertiesSettingsCard_NameHashHex.Content = $"0x{mainIndex.NameHash:X8}";
            TopHeaderDetailPage_PropertiesSettingsCard_DataOffsetHex.Content = $"0x{mainIndex.DataOffset:X8}";
            TopHeaderDetailPage_PropertiesSettingsCard_HeaderSizeBytes.Content = $"{data.HeaderSize} bytes";
            TopHeaderDetailPage_PropertiesSettingsCard_AllDataSizeBytes.Content = $"{data.AllDataSize} bytes";
            TopHeaderDetailPage_PropertiesSettingsCard_SubheaderConsistency.Content = isSubheaderConsistent ? "Valid" : "Mismatch";

            TopHeaderDetailPage_HSHRMainIndexSettingsCard_NameHash.Content = hshr.HSHRFile.MainIndex[hshr.index].NameHash;
            TopHeaderDetailPage_HSHRMainIndexSettingsCard_DataOffset.Content = hshr.HSHRFile.MainIndex[hshr.index].DataOffset;

            TopHeaderDetailPage_HSHRDataSettingsCard_CSTT.Content = hshr.HSHRFile.Data[hshr.index].CSTTMarker;
            TopHeaderDetailPage_HSHRDataSettingsCard_HeaderSize.Content = hshr.HSHRFile.Data[hshr.index].HeaderSize;
            TopHeaderDetailPage_HSHRDataSettingsCard_AllDataSize.Content = hshr.HSHRFile.Data[hshr.index].AllDataSize;
            TopHeaderDetailPage_HSHRDataSettingsCard_SubheadersCount.Content = hshr.HSHRFile.Data[hshr.index].SubheadersCount;
            TopHeaderDetailPage_HSHRDataSettingsCard_IDST.Content = hshr.HSHRFile.Data[hshr.index].IDSTMarker;
            TopHeaderDetailPage_HSHRDataSettingsCard_IDEN.Content = hshr.HSHRFile.Data[hshr.index].IDENMarker;

        }
    }
}
