using CommunityToolkit.Mvvm.ComponentModel;

namespace KeischProvisor.Controls
{
    /// <summary>
    /// Column widths shared by every data row of the explorer's tables.
    ///
    /// A cell is sized by binding a text element's <c>Width</c> (a plain <see cref="double"/>)
    /// while its grid column stays <c>Auto</c>. That keeps the layout free of
    /// <c>GridLength</c> converters, and because every row observes this one object, dragging a
    /// column divider updates all visible rows together.
    ///
    /// The caption row is the source of truth: its flat definitions match each data row's
    /// effective cell widths, including leading margins, and the page mirrors the text widths
    /// back into this object after each layout pass. Splitters overlay boundaries and consume
    /// no layout width.
    ///
    /// Every data column is fixed-width with a trailing star spacer, so a divider always moves
    /// the boundary it sits on instead of reflowing unrelated columns. The single consequence
    /// is that the defaults must fit inside the narrowest supported pane; otherwise the
    /// trailing column is clipped and its divider becomes unreachable. Keep the sums comfortably
    /// under that width:
    ///   tree    = 224 + 84 + 46  = 354  (pane default 380, minus 20px row padding = 360)
    ///   fields  =  92 + 204 + 62 + 132 + 92 + 22 = 604
    ///   hex     =  92 + 336 + 160 = 588
    /// </summary>
    public sealed partial class TableMetrics : ObservableObject
    {
        /// <summary>The single instance every row and caption binds to.</summary>
        public static TableMetrics Shared { get; } = new();

        // ---- sidebar tree ---------------------------------------------------

        /// <summary>Width of the "Name" column of the node tree.</summary>
        [ObservableProperty]
        public partial double TreeNameWidth { get; set; } = 224;

        /// <summary>Width of the "Offset" column of the node tree.</summary>
        [ObservableProperty]
        public partial double TreeOffsetWidth { get; set; } = 84;

        /// <summary>Width of the "Subs" column of the node tree.</summary>
        [ObservableProperty]
        public partial double TreeSubsWidth { get; set; } = 46;

        // ---- raw field table ------------------------------------------------

        [ObservableProperty]
        public partial double FieldOffsetWidth { get; set; } = 92;

        /// <summary>Width of the "Field" name column.</summary>
        [ObservableProperty]
        public partial double FieldNameWidth { get; set; } = 208;

        [ObservableProperty]
        public partial double FieldTypeWidth { get; set; } = 62;

        [ObservableProperty]
        public partial double FieldHexWidth { get; set; } = 126;

        [ObservableProperty]
        public partial double FieldDecWidth { get; set; } = 92;

        [ObservableProperty]
        public partial double FieldStatusWidth { get; set; } = 22;

        // ---- hex viewer -----------------------------------------------------

        [ObservableProperty]
        public partial double HexPositionWidth { get; set; } = 92;

        /// <summary>Width of the "Raw Byte" column.</summary>
        [ObservableProperty]
        public partial double HexRawWidth { get; set; } = 350;

        [ObservableProperty]
        public partial double HexAsciiWidth { get; set; } = 160;
    }
}
