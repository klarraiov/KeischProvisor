using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
using KeischProvisor.Pages;
using Microsoft.UI.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeischProvisor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            RequestPageTransition(typeof(Pages.MainPage), null!, null!);
        }

        public void RequestPageTransition(Type sourcePageType, object parameter, NavigationTransitionInfo navigationTransitionInfo)
        {
            if (sourcePageType == typeof(SettingsPage))
            { 
                AppTitleBar.IsBackButtonVisible = true;
                ((MicaBackdrop)SystemBackdrop).Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;
            }
            else
            { 
                AppTitleBar.IsBackButtonVisible = false;
                ((MicaBackdrop)SystemBackdrop).Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt;
            }
            ContentFrame.Navigate(sourcePageType, parameter, navigationTransitionInfo);
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Debug.WriteLine(args.KeyboardAccelerator.Key.ToString());
            bool isKeyDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(args.KeyboardAccelerator.Key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (!isKeyDown) return;

            if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Left)
            {
                if (ContentFrame.CanGoBack)
                {
                    ContentFrame.GoBack();
                    args.Handled = true;

                }
            }
            else if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Right)
            {
                if (ContentFrame.CanGoForward)
                {
                    ContentFrame.GoForward();
                    args.Handled = true;
                }
            }
        }
        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                if (ContentFrame.CanGoBack)
                {
                    ContentFrame.GoBack();
                    e.Handled = true;

                }
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                if (ContentFrame.CanGoForward)
                {
                    ContentFrame.GoForward();
                    e.Handled = true;
                }
            }

        }

        private void AppTitleBar_BackRequested(TitleBar sender, object args)
        {
            if (ContentFrame.CanGoBack)
            {
                var lastPage = ContentFrame.BackStack.Last() ?? null;
                if (lastPage != null)
                {
                    RequestPageTransition(lastPage.SourcePageType, null!, lastPage.NavigationTransitionInfo);
                }
            }
        }
    }
}
