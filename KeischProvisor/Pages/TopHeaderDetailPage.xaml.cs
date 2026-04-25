using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Respectre.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public sealed partial class TopHeaderDetailPage : Page
    {
        private MainPage.HSHRSync? currentHSHRSync;

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
            currentHSHRSync = (MainPage.HSHRSync)e.Parameter;
            TopHeaderDetailPage_Title.Text = string.Format(TopHeaderDetailPage_Title.Text, currentHSHRSync.index);
            TopHeaderDetailPage_HSHRMainIndexSettingsExpander.Header = string.Format((string)TopHeaderDetailPage_HSHRMainIndexSettingsExpander.Header, currentHSHRSync.index);
            TopHeaderDetailPage_HSHRDataSettingsExpander.Header = string.Format((string)TopHeaderDetailPage_HSHRDataSettingsExpander.Header, currentHSHRSync.index);

            HSHRIndex mainIndex = currentHSHRSync.HSHRFile.MainIndex[currentHSHRSync.index];
            HSHRData data = currentHSHRSync.HSHRFile.Data[currentHSHRSync.index];

            string resolvedName = HSHRFile.namehashPairs.TryGetValue(mainIndex.NameHash, out string? name) ? name : "Unknown";
            bool isSubheaderConsistent = data.SubheadersCount == data.SubheaderIndex.Count && data.SubheadersCount == data.Subheader.Count;

            TextBlock resolvedNameTextBlock = new TextBlock { Text = resolvedName , IsTextSelectionEnabled = true};

            TopHeaderDetailPage_PropertiesSettingsCard_ResolvedName.Content = resolvedNameTextBlock;
            TopHeaderDetailPage_PropertiesSettingsCard_NameHashHex.Content = $"0x{mainIndex.NameHash:X8}";
            TopHeaderDetailPage_PropertiesSettingsCard_DataOffsetHex.Content = $"0x{mainIndex.DataOffset:X8}";
            TopHeaderDetailPage_PropertiesSettingsCard_HeaderSizeBytes.Content = $"{data.HeaderSize} bytes";
            TopHeaderDetailPage_PropertiesSettingsCard_AllDataSizeBytes.Content = $"{data.AllDataSize} bytes";
            TopHeaderDetailPage_PropertiesSettingsCard_SubheaderConsistency.Content = isSubheaderConsistent ? App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_PropertiesValidationResultTrue").ValueAsString : App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_SubheaderConsistencyResultFalse").ValueAsString;

            TopHeaderDetailPage_HSHRMainIndexSettingsCard_NameHash.Content = currentHSHRSync.HSHRFile.MainIndex[currentHSHRSync.index].NameHash;
            TopHeaderDetailPage_HSHRMainIndexSettingsCard_DataOffset.Content = currentHSHRSync.HSHRFile.MainIndex[currentHSHRSync.index].DataOffset;

            TopHeaderDetailPage_HSHRDataSettingsCard_CSTT.Content = currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].CSTTMarker == BitConverter.ToUInt32(Encoding.ASCII.GetBytes("TTSC"), 0) ? App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_PropertiesValidationResultTrue").ValueAsString : App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_SubheaderConsistencyResultFalse").ValueAsString; ;
            TopHeaderDetailPage_HSHRDataSettingsCard_HeaderSize.Content = $"{currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].HeaderSize} bytes";
            TopHeaderDetailPage_HSHRDataSettingsCard_AllDataSize.Content = $"{currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].AllDataSize} bytes";
            TopHeaderDetailPage_HSHRDataSettingsCard_SubheadersCount.Content = currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].SubheadersCount;
            TopHeaderDetailPage_HSHRDataSettingsCard_IDST.Content = currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].IDSTMarker == BitConverter.ToUInt32(Encoding.ASCII.GetBytes("TSDI"), 0) ? App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_PropertiesValidationResultTrue").ValueAsString : App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_SubheaderConsistencyResultFalse").ValueAsString;
            TopHeaderDetailPage_HSHRDataSettingsCard_IDEN.Content = currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].IDENMarker == BitConverter.ToUInt32(Encoding.ASCII.GetBytes("NEDI"), 0) ? App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_PropertiesValidationResultTrue").ValueAsString : App.AppResourceManager.MainResourceMap.GetValue("Resources/TopHeaderDetailPage_PropertiesSettingsCard_SubheaderConsistencyResultFalse").ValueAsString;

        }

        private void TopHeaderDetailPage_HSHRDataSettingsCard_SubheadersIndex_Click(object sender, RoutedEventArgs e)
        {
            ((App.Current as App)!._window as MainWindow)!.RequestPageTransition(typeof(SubheaderDetailPage), currentHSHRSync!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private void TopHeaderDetailPage_HSHRDataSettingsCard_Header_Click(object sender, RoutedEventArgs e)
        {
            ((App.Current as App)!._window as MainWindow)!.RequestPageTransition(typeof(RawHeaderPage), currentHSHRSync!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight});

        }
    }
}
