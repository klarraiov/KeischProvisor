using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor.Pages
{
    partial class SubheaderIndex : ObservableObject
    {
        [ObservableProperty]
        private int index;
        [ObservableProperty]
        private string nameHash;
        [ObservableProperty]
        private uint dataOffset;
        [ObservableProperty]
        private string settingsCardHeaderName = string.Empty;
        [ObservableProperty]
        private string settingsCardDescription = string.Empty;

    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SubheaderDetailPage : Page
    {
        private TopHeaderNavigationInfo? currentNavigationInfo;
        private ObservableCollection<SubheaderIndex> subheaderIndices = new ObservableCollection<SubheaderIndex>();

        public SubheaderDetailPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);


            ((MainWindow)((App)App.Current!)._window!).AppTitleBar.IsBackButtonVisible = true;
            ((App.Current as App)!._window!.SystemBackdrop as MicaBackdrop)!.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;

            await InitializeView(e);
        }

        private async Task InitializeView(NavigationEventArgs e)
        {
            if (e.Parameter is not TopHeaderNavigationInfo)
            {
                Debug.WriteLine("Invalid parameter. Expected HSHRSync.");
                return;
            }
            currentNavigationInfo = (TopHeaderNavigationInfo)e.Parameter;

            SubheaderDetailPage_Title.Text = string.Format(SubheaderDetailPage_Title.Text, currentNavigationInfo.Index);
            string headername = App.AppResourceManager.MainResourceMap.GetValue("Resources/SubheaderDetailPage_SubheaderListView_SubheaderSettingsCard_Header").ValueAsString;
            for (int i = 0; i < currentNavigationInfo.HSHRFile.Data[currentNavigationInfo.Index].SubheadersCount; i++)
            {
                int index = i;

                var subheaderindex = new SubheaderIndex
                {
                    Index = index,
                    NameHash = currentNavigationInfo.HSHRFile.Data[currentNavigationInfo.Index].SubheaderIndex[index].NameHash.ToString("X8"),
                    DataOffset = currentNavigationInfo.HSHRFile.Data[currentNavigationInfo.Index].SubheaderIndex[index].DataOffset,
                    SettingsCardHeaderName =  string.Format(headername, index),
                    SettingsCardDescription = "NameHash: " + currentNavigationInfo.HSHRFile.Data[currentNavigationInfo.Index].SubheaderIndex[index].NameHash.ToString("X8") + ", DataOffset: " + currentNavigationInfo.HSHRFile.Data[currentNavigationInfo.Index].SubheaderIndex[index].DataOffset.ToString("X8")

                };
                subheaderIndices.Add(subheaderindex);
            }

        }
    }
}
