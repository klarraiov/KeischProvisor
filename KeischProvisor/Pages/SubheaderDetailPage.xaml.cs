using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SubheaderDetailPage : Page
    {
        private MainPage.HSHRSync? currentHSHRSync;

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
            if (e.Parameter is not KeischProvisor.Pages.MainPage.HSHRSync)
            {
                Debug.WriteLine("Invalid parameter. Expected HSHRSync.");
                return;
            }
            currentHSHRSync = (MainPage.HSHRSync)e.Parameter;

            await Task.Run(() =>
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    for (int i = 0; i < currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].SubheadersCount; i++)
                    {
                        int index = i;
                        await Task.Run(() =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                SettingsCard subheaderindex = new SettingsCard()
                                {
                                    Header = $"Subheader {i}",
                                    Description = $"Name: {currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].SubheaderIndex[i].NameHash}, DataOffset: 0x{currentHSHRSync.HSHRFile.Data[currentHSHRSync.index].SubheaderIndex[i].DataOffset:X8}"
                                };

                                testui.Children.Add(subheaderindex);
                            });

                        });
                    }
                });
            });


        }
    }
}
