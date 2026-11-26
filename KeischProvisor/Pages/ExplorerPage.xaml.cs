using CommunityToolkit.Mvvm.ComponentModel;
using KeischProvisor.Controls;
using KeischProvisor.Utils;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Respectre.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeischProvisor.Pages
{
    /// <summary>Flattened tree row for the left pane.</summary>
    public partial class NodeRow : ObservableObject
    {
        [ObservableProperty] public partial string Glyph { get; set; } = "";
        [ObservableProperty] public partial string Label { get; set; } = "";
        [ObservableProperty] public partial string Name { get; set; } = "";
        [ObservableProperty] public partial string OffsetText { get; set; } = "";
        [ObservableProperty] public partial string SubCountText { get; set; } = "";

        /// <summary>
        /// Indentation as a width rather than a margin. The name cell is a fixed-width column,
        /// so a margin would eat into that width and shift the glyph column; a leading spacer
        /// keeps the glyph, index and label aligned inside the column at every depth.
        /// </summary>
        [ObservableProperty] public partial double IndentWidth { get; set; }

        [ObservableProperty] public partial bool IsExpandable { get; set; }

        /// <summary>False for the file root, which has no "#0"-style index cell.</summary>
        [ObservableProperty] public partial bool HasLabel { get; set; } = true;

        public CacheNode Node { get; set; } = null!;

        public Visibility LabelVisibility => HasLabel ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>Column widths shared with the caption row (x:Bind resolves against the row).</summary>
        public TableMetrics Metrics => TableMetrics.Shared;
    }

    /// <summary>One row in the raw field table.</summary>
    public partial class FieldRow : ObservableObject
    {
        [ObservableProperty] public partial string OffsetText { get; set; } = "";
        [ObservableProperty] public partial string Name { get; set; } = "";
        [ObservableProperty] public partial string Type { get; set; } = "";
        [ObservableProperty] public partial string HexText { get; set; } = "";
        [ObservableProperty] public partial string DecText { get; set; } = "";
        [ObservableProperty] public partial string StatusGlyph { get; set; } = "";

        public FieldDescriptor Descriptor { get; set; } = null!;

        public TableMetrics Metrics => TableMetrics.Shared;
    }

    /// <summary>
    /// One line of the hex viewer. Position, raw bytes and ASCII are three separate cells,
    /// so they can be selected and resized independently instead of forming one drag surface.
    /// </summary>
    public partial class HexRow : ObservableObject
    {
        [ObservableProperty] public partial string Position { get; set; } = "";
        [ObservableProperty] public partial string Raw { get; set; } = "";
        [ObservableProperty] public partial string Ascii { get; set; } = "";

        /// <summary>Absolute file offset of the first byte on this line.</summary>
        public long Offset { get; set; }

        public TableMetrics Metrics => TableMetrics.Shared;
    }

    public sealed partial class ExplorerPage : Page
    {
        private HSHRFile? _file;
        private byte[]? _binary;
        private CacheFieldBuilder? _builder;
        private CacheNode? _root;
        private bool _suppressSelection;

        public ObservableCollection<NodeRow> Nodes { get; } = new();
        public ObservableCollection<FieldRow> Fields { get; } = new();
        public ObservableCollection<HexRow> HexRows { get; } = new();

        /// <summary>Column widths for the caption rows, bound once per page.</summary>
        public TableMetrics Metrics => TableMetrics.Shared;

        public ExplorerPage()
        {
            InitializeComponent();
            this.Loaded += InitializeSettings;
            NavigationCacheMode = NavigationCacheMode.Required;

            // Both sections must exist with a body before any file is loaded, otherwise the
            // divider has nothing to sit between and looks broken on a fresh launch.
            //
            // This runs from the constructor rather than Loaded: Loaded fires *after*
            // OnNavigatedTo has kicked off LoadAsync, so doing it there would re-show the
            // placeholder just after a successfully loaded file had hidden it.
            ShowEmptyLayout();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)((App)Application.Current)._window!;
            mainWindow.RequestPageTransition(typeof(SettingsPage), null!, new DrillInNavigationTransitionInfo());
        }

        private void StatusBarToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem toggleMenuFlyoutItem)
            {
                StatusBarGrid.Visibility = toggleMenuFlyoutItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;

                App.AppSettings.IsStatusBarVisible = toggleMenuFlyoutItem.IsChecked;
                SettingsManager.SaveSettings(App.AppSettings);
            }
        }

        private void InitializeSettings(object sender, RoutedEventArgs e)
        {
            MainPage_StatusBarToggleMenuFlyoutItem.IsChecked = App.AppSettings.IsStatusBarVisible;
            StatusBarGrid.Visibility = App.AppSettings.IsStatusBarVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// The "no file" state: both sections are present with a placeholder body so the split
        /// stays draggable, and the identity bar explains how to begin.
        /// </summary>
        private void ShowEmptyLayout()
        {
            SummaryPanel.Visibility = Visibility.Visible;
            _summaryHasContent = false;

            FieldsPanel.Visibility = Visibility.Visible;
            HexPanel.Visibility = Visibility.Visible;

            PlaceholderFieldText.Visibility = Visibility.Visible;
            PlaceholderHexText.Visibility = Visibility.Visible;

            HexHeader.Text = "Bytes";
            UpdateDetailRows();
        }

        /// <summary>
        /// Hides both placeholders once real content exists. Called from the load path, which is
        /// the only place that can prove a file was actually parsed.
        /// </summary>
        private void HidePlaceholders()
        {
            PlaceholderFieldText.Visibility = Visibility.Collapsed;
            PlaceholderHexText.Visibility = Visibility.Collapsed;
        }

        private async void CommandOpenButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker((App.Current as App)!._window!.AppWindow.Id);
            picker.FileTypeFilter.Add("*");
            picker.FileTypeFilter.Add(".cache");

            var result = await picker.PickSingleFileAsync();
            if (result == null) return;
            MainWindow? mw = (MainWindow)((App)Application.Current)._window!;
            if (mw.ContentFrame.Content is Pages.ExplorerPage explorer)
                await explorer.LoadAsync(result.Path);
            else
                mw.RequestPageTransition(typeof(Pages.ExplorerPage), result.Path, null!);
        }

        private void CommandSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow? mw = (MainWindow)((App)Application.Current)._window!;
            mw.RequestPageTransition(typeof(Pages.SettingsPage), null!, new DrillInNavigationTransitionInfo());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (App.Current is App app && app._window is MainWindow mw)
            {
                mw.AppTitleBar.IsBackButtonVisible = false;
                mw.AppTitleBar.Title = "KeischProvisor";
                if (mw.SystemBackdrop is MicaBackdrop mica)
                    mica.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt;
            }

            if (e.Parameter is string path && File.Exists(path))
                _ = LoadAsync(path);
        }

        // ---- loading --------------------------------------------------------

        public async Task LoadAsync(string path)
        {
            StatusText.Text = "Reading…";
            _file = null; _binary = null; _builder = null; _root = null;
            Nodes.Clear(); Fields.Clear(); HexRows.Clear();

            try
            {
                var binary = await Task.Run(() => File.ReadAllBytes(path));
                var file = new HSHRFile(path);
                await Task.Run(() => file.ScanStructure(status =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                        StatusText.Text = $"Reading 0x{status.Position:X} · {status.Stage} {status.CurrentIndex}/{status.TotalTopHeadersCount}");
                }));

                _file = file;
                _binary = binary;
                _builder = new CacheFieldBuilder(file, binary);
                _root = CacheTreeBuilder.Build(file);

                BuildNodeRows();
                ShowNode(_root);

                // ---- global toolbar (previously MainPage) ----
                SignatureText.Text = Encoding.ASCII.GetString(BitConverter.GetBytes(file.Signature));
                BuildText.Text = $"{file.VersionMajor}.{file.VersionMinor}";
                HeaderCountText.Text = $"{file.TopHeadersCount} headers";
                ChecksumText.Text = $"0x{file.Checksum:X16}";
                ToolTipService.SetToolTip(ChecksumText, $"0x{file.Checksum:X16} ({file.Checksum:N0})");
                if (App.Current is App app && app._window is MainWindow mw)
                {
                    mw.ShowOpenedPath(path);
                }
                StatusText.Text = $"Loaded {new FileInfo(path).Length:N0} bytes · {Nodes.Count:N0} nodes";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed: {ex.Message}";
                Debug.WriteLine(ex);
            }
        }

        /// <summary>Top-level header indices whose subheaders are currently shown.</summary>
        private readonly HashSet<int> _expanded = new();

        /// <summary>
        /// Toggles a top header's subheaders by inserting/removing only that slice of rows.
        /// Rebuilding the whole collection would scroll the list back to the top and drop the
        /// user's place, which broke the workflow on every expand.
        /// </summary>
        private void ToggleExpanded(int topIndex)
        {
            if (_root == null) return;

            // NOTE: use Where+DefaultIfEmpty on the index only. A bare FirstOrDefault over a
            // tuple returns (null, 0) when nothing matches, which would silently resolve to
            // row 0 (the root) and toggle the wrong node.
            int rowIdx = Nodes.Select((r, i) => (Row: r, Index: i))
                              .Where(t => t.Row.Node.Kind == CacheNodeKind.TopHeader
                                       && t.Row.Node.TopIndex == topIndex)
                              .Select(t => t.Index)
                              .DefaultIfEmpty(-1)
                              .First();
            if (rowIdx < 0 || rowIdx >= Nodes.Count) return;

            var topNode = Nodes[rowIdx];
            if (!topNode.IsExpandable) return;

            bool willExpand = !_expanded.Contains(topIndex);
            int childCount = topNode.Node.Children.Count;

            _suppressSelection = true;
            if (willExpand)
            {
                _expanded.Add(topIndex);
                topNode.Glyph = "\uE70D";
                for (int k = 0; k < childCount; k++)
                    Nodes.Insert(rowIdx + 1 + k, MakeRow(topNode.Node.Children[k], 2));
            }
            else
            {
                _expanded.Remove(topIndex);
                topNode.Glyph = "\uE76C";
                for (int k = 0; k < childCount; k++)
                {
                    if (rowIdx + 1 < Nodes.Count &&
                        Nodes[rowIdx + 1].Node.Kind == CacheNodeKind.Subheader)
                        Nodes.RemoveAt(rowIdx + 1);
                }
            }
            _suppressSelection = false;

            NodeListView.SelectedIndex = rowIdx;
            NodeListView.ScrollIntoView(topNode);
            UpdateSelectionStatus();
        }

        private void BuildNodeRows()
        {
            Nodes.Clear();
            if (_root == null) return;

            Nodes.Add(MakeRow(_root, 0));
            foreach (var top in _root.Children)
            {
                Nodes.Add(MakeRow(top, 1));

                // Subheaders only appear when the parent top header is expanded; showing
                // all ~5000 of them at once makes the tree unreadable.
                if (_expanded.Contains(top.TopIndex))
                {
                    foreach (var sub in top.Children)
                        Nodes.Add(MakeRow(sub, 2));
                }
            }
            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            if (_root == null) { SelectionStatusText.Text = ""; return; }
            int shown = Nodes.Count;
            int total = 1 + _root.Children.Count + _root.Children.Sum(c => c.Children.Count);
            SelectionStatusText.Text = shown == total
                ? $"{shown:N0} nodes"
                : $"{shown:N0} / {total:N0} nodes";
        }

        private NodeRow MakeRow(CacheNode node, int depth)
        {
            // rpf.cache is a single file, so the root is a document, not a folder. Top-level
            // entries are cached CSTT header blocks and subheaders are single cached RPF pages.
            // Every glyph sits in a fixed 16px cell with centred text, so the icon column lines
            // up regardless of expansion state or label width.
            string glyph;
            if (node.Kind == CacheNodeKind.TopHeader && node.Children.Count > 0)
                glyph = _expanded.Contains(node.TopIndex) ? "\uE70D" : "\uE76C";
            else if (node.Kind == CacheNodeKind.Root)
                glyph = "\uE8A5";   // document
            else if (node.Kind == CacheNodeKind.TopHeader)
                glyph = "\uE8F1";   // library / header block
            else
                glyph = "\uE7C3";   // page

            return new NodeRow
            {
                Node = node,
                Glyph = glyph,
                Label = node.Label,
                HasLabel = !string.IsNullOrEmpty(node.Label),
                Name = node.Name ?? "—",
                OffsetText = node.Kind == CacheNodeKind.Root ? "0x0" : $"0x{node.Start:X}",
                SubCountText = node.Children.Count > 0 ? node.Children.Count.ToString() : "",
                IndentWidth = depth * 12,
                IsExpandable = node.Kind == CacheNodeKind.TopHeader && node.Children.Count > 0,
            };
        }

        /// <summary>Clicking the chevron expands/collapses that top header's subheaders.</summary>
        private void NodeRow_GlyphTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is NodeRow row && row.IsExpandable)
            {
                e.Handled = true;
                ToggleExpanded(row.Node.TopIndex);
            }
        }

        /// <summary>
        /// Double-clicking anywhere on a top-header row toggles its subheaders. Handled at the
        /// row template root because the ListViewItem swallows the gesture before it surfaces
        /// as DoubleTapped on the ListView itself.
        /// </summary>
        private void NodeRow_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is NodeRow row && row.IsExpandable)
            {
                ToggleExpanded(row.Node.TopIndex);
                e.Handled = true;
            }
        }

        // ---- selection ------------------------------------------------------

        private void NodeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;
            if (NodeListView.SelectedItem is NodeRow row)
                ShowNode(row.Node);
        }

        private void ShowNode(CacheNode node)
        {
            if (_builder == null || _file == null) return;

            _suppressSelection = true;
            Fields.Clear();

            // ---- A: identity ----
            switch (node.Kind)
            {
                case CacheNodeKind.Root:
                    IdentityTitle.Text = _file.Name ?? "rpf.cache";
                    IdentitySubtitle.Text = $"{_binary!.Length:N0} bytes · {_file.TopHeadersCount} top-level headers";
                    break;
                case CacheNodeKind.TopHeader:
                    IdentityTitle.Text = node.Name ?? "—";
                    IdentitySubtitle.Text = $"Top Header {node.Label} · 0x{node.Start:X} – 0x{node.End:X} ({node.Size:N0} bytes)";
                    break;
                default:
                    IdentityTitle.Text = node.Name ?? "—";
                    IdentitySubtitle.Text = $"Subheader of #{node.TopIndex} · 0x{node.Start:X} ({node.Size:N0} bytes)";
                    break;
            }

            // ---- B/C: fields ----
            List<FieldDescriptor> descriptors = node.Kind switch
            {
                CacheNodeKind.Root => _builder.BuildRoot(),
                CacheNodeKind.TopHeader => _builder.BuildTopHeader(node.TopIndex),
                _ => _builder.BuildSubheader(node.TopIndex, node.SubIndex),
            };

            PopulateSummary(descriptors.Where(d => d.Kind == FieldKind.Computed).ToList());

            foreach (var d in descriptors.Where(d => d.Kind != FieldKind.Computed))
            {
                Fields.Add(new FieldRow
                {
                    Descriptor = d,
                    OffsetText = d.Offset >= 0 ? $"0x{d.Offset:X}" : "—",
                    Name = d.Name,
                    Type = d.Type,
                    HexText = d.Hex,
                    DecText = d.Dec,
                    StatusGlyph = d.StatusGlyph,
                });
            }
            // The sections stay present and keep their split; an empty one simply shows its
            // caption with no rows instead of vanishing and taking the divider with it.
            FieldsPanel.Visibility = Visibility.Visible;
            HidePlaceholders();

            // ---- D: hex ----
            PopulateHex(node);
            HexHeader.Text = node.Kind == CacheNodeKind.Subheader
                ? "Subheader bytes"
                : node.Kind == CacheNodeKind.TopHeader ? "Block bytes" : "Header bytes";

            UpdateDetailRows();

            _suppressSelection = false;

            SelectionStatusText.Text = node.Kind == CacheNodeKind.Root
                ? $"{_file.TopHeadersCount} headers"
                : node.Kind == CacheNodeKind.TopHeader
                    ? $"{node.Children.Count} subheaders"
                    : $"subheader [{node.SubIndex}]";
        }

        private void PopulateSummary(List<FieldDescriptor> computed)
        {
            SummaryGrid.Children.Clear();
            SummaryGrid.RowDefinitions.Clear();

            // The section keeps its caption and chevron even when the node has nothing to
            // summarise, so every section is collapsible at any time.
            if (computed.Count == 0)
            {
                _summaryHasContent = false;
                SummaryPanel.Visibility = Visibility.Visible;
                SummaryContent.Visibility = Visibility.Collapsed;
                return;
            }

            _summaryHasContent = true;

            for (int i = 0; i < computed.Count; i++)
            {
                SummaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock
                {
                    Text = computed[i].Name,
                    Style = (Style)Resources["FieldLabelStyle"],
                    VerticalAlignment = VerticalAlignment.Top,
                };
                Grid.SetRow(label, i);
                Grid.SetColumn(label, 0);

                var value = new TextBlock
                {
                    Text = computed[i].StatusDetail ?? "—",
                    FontSize = 12,
                    IsTextSelectionEnabled = true,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                Grid.SetRow(value, i);
                Grid.SetColumn(value, 1);

                SummaryGrid.Children.Add(label);
                SummaryGrid.Children.Add(value);
            }
            SummaryPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Builds the hex viewer as one row per 16 bytes. Position, raw bytes and ASCII are
        /// separate cells so the three columns are independently selectable and resizable;
        /// a single multi-line TextBox merged them into one indistinguishable drag surface.
        /// </summary>
        private void PopulateHex(CacheNode node)
        {
            if (_binary == null) return;

            HexRows.Clear();
            _hexDump.Clear();

            long start = node.Start;
            long length = node.Size;

            if (node.Kind == CacheNodeKind.Root)
            {
                start = 0;
                length = Math.Min(_binary.Length, 0x5CC);
            }
            else if (length <= 0)
            {
                length = 16;
            }

            // Keep the dump bounded so selecting a large block stays instant.
            const long Cap = 32 * 1024;
            if (length > Cap) length = Cap;

            long end = Math.Min(start + length, _binary.Length);
            for (long off = start; off < end; off += 16)
            {
                int n = (int)Math.Min(16, end - off);
                var raw = new StringBuilder(48);
                var ascii = new StringBuilder(16);
                for (int i = 0; i < n; i++)
                {
                    byte b = _binary[off + i];
                    raw.Append(b.ToString("X2")).Append(' ');
                    ascii.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
                }

                HexRows.Add(new HexRow
                {
                    Offset = off,
                    Position = $"0x{off:X8}",
                    Raw = raw.ToString().TrimEnd(),
                    Ascii = ascii.ToString(),
                });

                _hexDump.Append($"0x{off:X8}  ");
                _hexDump.Append(raw.ToString().PadRight(48));
                _hexDump.Append(" |").Append(ascii).Append('|').Append('\n');
            }

            // Both sections stay mounted regardless of content: they show their captions and
            // keep whatever split the user dragged, even when a node has no rows of its own.
            HexPanel.Visibility = Visibility.Visible;
            HidePlaceholders();
        }

        private readonly StringBuilder _hexDump = new();

        private void CopyHexButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hexDump.Length == 0) return;
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(_hexDump.ToString());
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
            StatusText.Text = $"Copied {HexRows.Count:N0} lines";
        }

        /// <summary>Field row selected -> bring that field's bytes into view in the hex list.</summary>
        private void FieldListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FieldListView.SelectedItem is not FieldRow row) return;
            var d = row.Descriptor;
            if (d.Offset < 0 || d.Length <= 0 || HexRows.Count == 0) return;

            var target = HexRows.FirstOrDefault(h => h.Offset <= d.Offset && d.Offset < h.Offset + 16);
            if (target != null)
            {
                HexListView.SelectedItem = target;
                HexListView.ScrollIntoView(target);
            }
        }

        // ---- column resizing ------------------------------------------------

        // A column width lives in exactly one place: TableMetrics. The caption cell and every
        // data cell of that column bind their Width to the same property, so writing it here
        // moves both at once. No layout-pass mirroring, no ColumnDefinition pixel values, and
        // no per-column inset correction.

        /// <summary>A resizeable column: its current width, its floor, and how to write it.</summary>
        private readonly record struct ColumnSlot(double Width, double Min, Action<double> Apply);

        /// <summary>
        /// Resizes exactly one column, the way Windows File Explorer does: the dragged column
        /// changes width, and every column after it is simply pushed along keeping its own
        /// width. Nothing shrinks to compensate.
        ///
        /// Two earlier approaches were wrong for this UI. Trading width with the neighbour kept
        /// the table's total constant, so the neighbour shrank as you dragged (which Explorer
        /// never does) and any column beyond that neighbour never moved at all — that is why
        /// the Raw Fields check-mark column stayed put when the Dec divider was dragged.
        /// Capping growth to the visible viewport was also wrong: the columns already nearly
        /// fill the pane, so every divider hit an invisible wall after a few pixels. Explorer
        /// instead lets the table grow wider than the window and scrolls horizontally, and so
        /// does this table — see the ListViews' horizontal scroll settings.
        /// </summary>
        private static void ResizeOne(ColumnSlot dragged, double delta)
        {
            double next = Math.Max(dragged.Min, dragged.Width + delta);
            if (Math.Abs(next - dragged.Width) > 0.01) dragged.Apply(next);
        }

        // Each handler names the column whose divider it owns. Only that column changes width;
        // every column after it is pushed along with its own width preserved.

        // ---- sidebar tree: Name | Offset | Subs ------------------------------

        private void TreeNameGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.TreeNameWidth, 96, v => Metrics.TreeNameWidth = v), delta);

        private void TreeOffsetGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.TreeOffsetWidth, 56, v => Metrics.TreeOffsetWidth = v), delta);

        private void TreeSubsGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.TreeSubsWidth, 34, v => Metrics.TreeSubsWidth = v), delta);

        // ---- raw field table: Offset | Field | Type | Hex | Dec | status ------

        private void FieldOffsetGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.FieldOffsetWidth, 56, v => Metrics.FieldOffsetWidth = v), delta);

        private void FieldNameGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.FieldNameWidth, 90, v => Metrics.FieldNameWidth = v), delta);

        private void FieldTypeGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.FieldTypeWidth, 42, v => Metrics.FieldTypeWidth = v), delta);

        private void FieldHexGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.FieldHexWidth, 54, v => Metrics.FieldHexWidth = v), delta);

        private void FieldDecGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.FieldDecWidth, 50, v => Metrics.FieldDecWidth = v), delta);

        // ---- hex viewer: Position | Raw Byte | ASCII Text ---------------------

        private void HexPositionGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.HexPositionWidth, 70, v => Metrics.HexPositionWidth = v), delta);

        private void HexRawGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.HexRawWidth, 120, v => Metrics.HexRawWidth = v), delta);

        private void HexAsciiGrip_DragDelta(object sender, double delta) =>
            ResizeOne(new ColumnSlot(Metrics.HexAsciiWidth, 70, v => Metrics.HexAsciiWidth = v), delta);

        // ---- pane split -----------------------------------------------------

        // The two tables share the detail pane's height through star-sized rows. The divider
        // converts its drag into a change of the Fields row's pixel height and lets the Hex row
        // (still a star) absorb the remainder, so the pair always fills the pane exactly.
        //
        // Both regions exist even before a file is loaded: they show a placeholder instead of
        // disappearing. Otherwise the divider would be hidden on a fresh launch and the split
        // would appear broken until the user happened to open a cache.

        /// <summary>Ratios of the two panes when nothing has been dragged.</summary>
        private const double DefaultFieldsWeight = 2;
        private const double DefaultHexWeight = 3;

        /// <summary>
        /// Smallest height either pane may be dragged down to, in pixels. Matches the
        /// <c>MinHeight</c> on both rows in the XAML so the two clamps cannot disagree.
        /// </summary>
        private const double MinPaneHeight = 120;

        /// <summary>True once the user has dragged the divider away from the default split.</summary>
        private bool _paneSplitCustomized;

        /// <summary>Fields-row height chosen by the user, in pixels.</summary>
        private double _customFieldsHeight;

        /// <summary>Total height available to the two panes (everything below identity/summary).</summary>
        private double PaneAreaHeight => FieldsRow.ActualHeight + DetailDividerRow.ActualHeight
                                       + HexRowDefinition.ActualHeight;

        /// <summary>
        /// Applies the current split and collapse state to the detail rows.
        ///
        /// Spacing is fixed, never state-dependent. Grid row spacing cannot be used for this
        /// because it applies between every pair of rows regardless of their height, so a folded
        /// row still contributed a gap and a hidden divider row doubled the space between the
        /// panes. Instead DetailRoot has RowSpacing 0, identity and summary carry a bottom
        /// Margin, and the divider row is a constant 10px band. A collapsed section therefore
        /// removes exactly its own body and nothing else, so every remaining element keeps the
        /// same position.
        ///
        /// A collapsed section becomes <c>Auto</c> rather than <c>0</c>: the header and chevron
        /// must stay visible because they are the only way to expand it again.
        ///
        /// The rows carry no <c>MinHeight</c> in XAML, since a RowDefinition minimum outranks an
        /// assigned height. The usable minimum is enforced by <see cref="MinPaneHeight"/> in the
        /// drag handler instead.
        /// </summary>
        private void UpdateDetailRows()
        {
            // Capture the pane's last real height before anything below changes it.
            //
            // Both panes folded makes the row Auto, whose ActualHeight is only the caption
            // (about 18px). Reading ActualHeight at that point and pinning it — which an earlier
            // version did — reopens the pane to 18px, so it looks like expanding does nothing.
            // Remembering a height that is known to be a real pane size avoids that entirely.
            if (FieldsContent.Visibility == Visibility.Visible && FieldsRow.ActualHeight > CaptionHeight)
            {
                _lastFieldsHeight = FieldsRow.ActualHeight;
            }

            ApplyChevron(SummaryChevron, _summaryCollapsed);
            ApplyChevron(FieldsChevron, _fieldsCollapsed);
            ApplyChevron(HexChevron, _hexCollapsed);

            // The bodies collapse independently of the section, so an empty table can still show
            // its caption and chevron. "Has no content" and "user collapsed it" are separate
            // states: a node with nothing to summarise must not latch the section shut.
            SetBodyVisible(SummaryContent,
                !(_summaryCollapsed || !_summaryHasContent));
            SetBodyVisible(FieldsContent, !_fieldsCollapsed);
            SetBodyVisible(HexContent, !_hexCollapsed);

            bool fields = !_fieldsCollapsed;
            bool hex = !_hexCollapsed;

            // The grip is only meaningful between two panes. Its row keeps its 10px so the
            // spacing around it matches every other section gap; only the draggable rule hides.
            DetailDividerHost.Visibility = (fields && hex) ? Visibility.Visible : Visibility.Collapsed;

            if (!fields && !hex)
            {
                // Both bodies folded: only the two captions remain, stacked.
                SetRowHeight(FieldsRow, GridLength.Auto);
                SetRowHeight(HexRowDefinition, GridLength.Auto);
                return;
            }

            if (!hex)
            {
                // Folding the hex viewer leaves Raw Fields exactly where it was, and the space the
                // hex pane releases stays empty rather than being handed over: a pane that
                // silently doubles in size when its neighbour closes is disorienting, and the
                // user loses the split they had set.
                //
                // The remembered height is used rather than ActualHeight, because this branch
                // also runs when reopening from a state where the row was Auto and its measured
                // height was only the caption.
                SetRowHeight(FieldsRow, new GridLength(PreferredFieldsHeight, GridUnitType.Pixel));
                SetRowHeight(HexRowDefinition, GridLength.Auto);
                return;
            }

            if (!fields)
            {
                // Only the hex viewer is open, same reasoning.
                SetRowHeight(HexRowDefinition, new GridLength(1, GridUnitType.Star));
                SetRowHeight(FieldsRow, GridLength.Auto);
                return;
            }

            if (_paneSplitCustomized)
            {
                SetRowHeight(FieldsRow, new GridLength(_customFieldsHeight, GridUnitType.Pixel));
                SetRowHeight(HexRowDefinition, new GridLength(1, GridUnitType.Star));
            }
            else
            {
                SetRowHeight(FieldsRow, new GridLength(DefaultFieldsWeight, GridUnitType.Star));
                SetRowHeight(HexRowDefinition, new GridLength(DefaultHexWeight, GridUnitType.Star));
            }
        }

        /// <summary>
        /// Height of a section caption, used to tell a real pane height from the stub height a
        /// folded row measures at. Generous on purpose: anything below this is not a usable pane.
        /// </summary>
        private const double CaptionHeight = 40;

        /// <summary>
        /// Last height the Raw Fields pane had while it was actually open. Reopening restores
        /// this instead of whatever the collapsed row happens to measure.
        /// </summary>
        private double _lastFieldsHeight;

        /// <summary>
        /// Height to give the Raw Fields pane. Falls back to the default share of the pane on the
        /// very first open, when no real height has been observed yet.
        /// </summary>
        private double PreferredFieldsHeight =>
            _lastFieldsHeight > CaptionHeight
                ? _lastFieldsHeight
                : _customFieldsHeight > CaptionHeight
                    ? _customFieldsHeight
                    : Math.Max(MinPaneHeight, PaneAreaHeight * DefaultFieldsWeight /
                                             (DefaultFieldsWeight + DefaultHexWeight));

        /// <summary>
        /// Assigns a row height only when it differs from the current value.
        ///
        /// <c>UpdateDetailRows</c> runs on every node selection and on every toggle, and three of
        /// its branches set the same heights as the previous call. Assigning an equal
        /// <see cref="GridLength"/> still invalidates the row, which re-measures both ListViews
        /// and reads as a visible refresh of content nobody touched. Comparing first keeps an
        /// unchanged layout completely quiet.
        /// </summary>
        private static void SetRowHeight(RowDefinition row, GridLength height)
        {
            if (row.Height.Equals(height)) return;
            row.Height = height;
        }

        /// <summary>Chevron points down when the section is open and right when it is closed.</summary>
        private static void ApplyChevron(FontIcon chevron, bool collapsed) =>
            chevron.Glyph = collapsed ? "\uE76C" : "\uE70D";

        /// <summary>
        /// Shows or hides a section body, fading it in only when it actually becomes visible.
        ///
        /// The transition check is what keeps a toggle local. <see cref="UpdateDetailRows"/> is
        /// called for all three sections on every change, so an unconditional fade restarted all
        /// of them from zero opacity each time: collapsing one section made the other two flash
        /// as well, and selecting a node re-faded everything. Returning early when the requested
        /// state already holds means only the section the user touched animates.
        ///
        /// Critically, the body is only faded and never moved. Row heights settle instantly, so
        /// by the time the animation starts the element already occupies its final slot and
        /// cannot be drawn on top of a neighbour. That is what made
        /// <c>RepositionThemeTransition</c> unusable: it slid sections across rows that had
        /// already snapped to their new sizes, so the moving pane and its neighbour were visible
        /// in the same place for the duration of the transition.
        ///
        /// Only the body is animated, never the header, so the caption stays rock steady and the
        /// content materialises beneath it.
        /// </summary>
        private static void SetBodyVisible(FrameworkElement body, bool visible)
        {
            Visibility target = visible ? Visibility.Visible : Visibility.Collapsed;

            // Already settled in the requested state: leave it alone rather than replaying the
            // fade. This is the whole difference between a local toggle and a full refresh.
            if (body.Visibility == target && body.Opacity >= 1) return;

            body.Visibility = target;
            body.Opacity = 1;

            if (!visible) return;

            // Fade in only. The 140ms is short enough to read as instant feedback and long
            // enough to avoid a hard pop; no opacity change runs on collapse, so collapsing is
            // immediate and cannot overlap anything.
            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(140)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(fade, body);
            Storyboard.SetTargetProperty(fade, "Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fade);
            storyboard.Begin();
        }

        /// <summary>
        /// Divider drags need no special animation handling any more.
        ///
        /// These existed only to clear and restore a RepositionThemeTransition so the split
        /// tracked the pointer 1:1. With no layout transition on the pane, dragging is direct
        /// manipulation by construction, so both handlers are intentionally empty but kept
        /// wired: they document why the divider reports drag start and completion, and give a
        /// single obvious place to hook any future per-drag behaviour.
        /// </summary>
        private void DetailDivider_DragStarted(object? sender, EventArgs e)
        {
        }

        private void DetailDivider_DragCompleted(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// Moves the split by <paramref name="delta"/> pixels: dragging down grows the Raw Fields
        /// pane and shrinks the Hex viewer, dragging up does the reverse.
        ///
        /// Both panes are clamped to <see cref="MinPaneHeight"/> so neither can be dragged to
        /// nothing. If the pane is too short for two minimums, the divider stops responding
        /// rather than producing negative heights.
        /// </summary>
        private void DetailDivider_DragDelta(object sender, double delta)
        {
            if (_fieldsCollapsed || _hexCollapsed) return;

            double available = PaneAreaHeight;
            if (available <= 0) return;

            double current = FieldsRow.ActualHeight;
            if (current <= 0)
            {
                // First drag after a fresh layout: derive the current split from the weights.
                current = available * DefaultFieldsWeight / (DefaultFieldsWeight + DefaultHexWeight);
            }

            double max = available - DetailDividerRow.ActualHeight - MinPaneHeight;
            double next = Math.Clamp(current + delta, MinPaneHeight, Math.Max(MinPaneHeight, max));

            _customFieldsHeight = next;
            _paneSplitCustomized = true;
            FieldsRow.Height = new GridLength(next, GridUnitType.Pixel);
            HexRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        }

        /// <summary>Double-clicking the divider restores the default 2 : 3 split.</summary>
        private void DetailDivider_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _paneSplitCustomized = false;
            FieldsRow.Height = new GridLength(DefaultFieldsWeight, GridUnitType.Star);
            HexRowDefinition.Height = new GridLength(DefaultHexWeight, GridUnitType.Star);
            e.Handled = true;
        }

        // ---- section collapse -----------------------------------------------

        private bool _summaryCollapsed;
        private bool _fieldsCollapsed;
        private bool _hexCollapsed;

        /// <summary>
        /// Whether the current node produced any summary rows. Kept separate from
        /// <see cref="_summaryCollapsed"/> so an empty node hides the body without latching the
        /// section shut.
        /// </summary>
        private bool _summaryHasContent;

        private void SummaryHeader_Click(object sender, RoutedEventArgs e)
        {
            _summaryCollapsed = !_summaryCollapsed;
            UpdateDetailRows();
        }

        private void FieldsHeader_Click(object sender, RoutedEventArgs e)
        {
            _fieldsCollapsed = !_fieldsCollapsed;
            UpdateDetailRows();
        }

        private void HexHeader_Click(object sender, RoutedEventArgs e)
        {
            _hexCollapsed = !_hexCollapsed;
            UpdateDetailRows();
        }

        // ---- validation report ---------------------------------------------

        private async void ValidationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_file == null || _binary == null) return;

            int badCstt = 0, badIdst = 0, badIden = 0, badMainPtr = 0, badSubPtr = 0, badAlign = 0;
            long expected = 0x14 + (long)_file.TopHeadersCount * 8;

            for (int i = 0; i < _file.Data.Count; i++)
            {
                var d = _file.Data[i];
                if (d.BlockStart != expected) badCstt++;
                if (d.IDSTMarker != 0x49445354) badIdst++;
                if (d.IDENMarker != 0x4944454E) badIden++;
                if (d.HeaderMagicOffset >= 0 && d.HeaderMagicOffset % 16 != 0) badAlign++;

                uint mi = _file.MainIndex[i].DataOffset;
                if (mi + 4 > _binary.Length || Encoding.ASCII.GetString(_binary, (int)mi, 4) != "7FPR") badMainPtr++;
                foreach (var s in d.SubheaderIndex)
                    if (s.DataOffset + 4 > _binary.Length ||
                        Encoding.ASCII.GetString(_binary, (int)s.DataOffset, 4) != "7FPR") badSubPtr++;

                expected = d.BlockStart + 12 + d.AllDataSize;
            }
            bool eofOk = expected == _binary.Length;
            int totalSubs = _file.Data.Sum(d => d.SubheaderIndex.Count);

            var body = new StackPanel { Spacing = 2 };
            void Line(string text) => body.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas, Cascadia Mono, Courier New"),
                IsTextSelectionEnabled = true,
            });

            Line($"signature 'HSHR'      {(Encoding.ASCII.GetString(BitConverter.GetBytes(_file.Signature)) == "HSHR" ? "OK" : "FAIL")}");
            Line($"version               {_file.VersionMajor}.{_file.VersionMinor} (0x{_file.Version:X8})");
            Line($"CSTT chain            {(badCstt == 0 ? "OK" : $"{badCstt} broken")}");
            Line($"IDST markers          {(badIdst == 0 ? "OK" : $"{badIdst} bad")}");
            Line($"IDEN markers          {(badIden == 0 ? "OK" : $"{badIden} bad")}");
            Line($"7FPR 16-byte aligned  {(badAlign == 0 ? "OK" : $"{badAlign} misaligned")}");
            Line($"main index pointers   {(badMainPtr == 0 ? $"OK ({_file.Data.Count})" : $"{badMainPtr} bad")}");
            Line($"subheader pointers    {(badSubPtr == 0 ? $"OK ({totalSubs})" : $"{badSubPtr} bad")}");
            Line($"walk ends at EOF      {(eofOk ? $"OK (0x{expected:X})" : $"0x{expected:X} vs 0x{_binary.Length:X}")}");

            var dialog = new ContentDialog
            {
                Title = "Validation",
                Content = new ScrollViewer { Content = body, MaxHeight = 420 },
                CloseButtonText = "Close",
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
        }
    }
}
