using System;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace KeischProvisor.Controls
{
    /// <summary>
    /// A thin drag handle that sits on a pane or column boundary and reports drag deltas.
    ///
    /// Two different layouts use it, which is why it takes an <see cref="Orientation"/>:
    ///
    ///   * <b>Horizontal</b> (default) — a table column divider. The explorer's tables are
    ///     hand-rolled <c>ListView</c> + <c>Grid</c> layouts where the caption row and every data
    ///     row are separate visual trees, so a real <c>GridSplitter</c> cannot serve them: it
    ///     resizes a <c>ColumnDefinition</c> that only exists in the caption grid. Instead this
    ///     handle reports a delta and the page writes it to the shared <see cref="TableMetrics"/>
    ///     that both the caption and all rows bind to — one source of truth, no mirroring.
    ///
    ///   * <b>Vertical</b> — the divider between the Raw Fields and Hex Viewer panes, where the
    ///     page converts the delta into row heights.
    ///
    /// The divider rule is <b>permanently visible</b> rather than hover-only: a boundary the user
    /// can drag should look draggable at rest, otherwise every adjustable split has to be
    /// discovered by accident.
    /// </summary>
    /// <remarks>
    /// Must be <c>partial</c>: the CsWinRT source generator only emits the activation factory
    /// for partial types. Without it the class compiles but XAML fails to instantiate it at
    /// runtime with CLASS_E_CLASSNOTAVAILABLE (0x80040111).
    /// </remarks>
    public sealed partial class ColumnGrip : Grid
    {
        private readonly Border _line;
        private bool _dragging;
        private double _lastPosition;

        /// <summary>
        /// Movement along the drag axis in device independent pixels since the previous event.
        /// Positive is right for <see cref="Orientation.Horizontal"/> and down for
        /// <see cref="Orientation.Vertical"/>.
        /// </summary>
        public event EventHandler<double>? DragDelta;

        /// <summary>
        /// Raised when a drag begins, before the first <see cref="DragDelta"/>.
        ///
        /// A host that animates layout changes uses this to switch the animation off for the
        /// duration of the drag: a layout transition re-tweens every frame the split moves, so
        /// the panes lag behind the pointer instead of tracking it.
        /// </summary>
        public event EventHandler? DragStarted;

        /// <summary>Raised when the drag ends, including when the pointer capture is lost.</summary>
        public event EventHandler? DragCompleted;

        // ---- Orientation ----------------------------------------------------

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation), typeof(Orientation), typeof(ColumnGrip),
                new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        /// <summary>Axis this handle can be dragged along.</summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColumnGrip)d).ApplyOrientation((Orientation)e.NewValue);
        }

        // ---- LineOpacity ----------------------------------------------------

        public static readonly DependencyProperty LineOpacityProperty =
            DependencyProperty.Register(
                nameof(LineOpacity), typeof(double), typeof(ColumnGrip),
                new PropertyMetadata(0.9, OnLineOpacityChanged));

        /// <summary>
        /// Opacity of the divider rule. The rule is always shown; raise this for a stronger
        /// boundary, lower it to make the split more recessive.
        /// </summary>
        public double LineOpacity
        {
            get => (double)GetValue(LineOpacityProperty);
            set => SetValue(LineOpacityProperty, value);
        }

        private static void OnLineOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grip = (ColumnGrip)d;
            grip._line.Opacity = (double)e.NewValue;
        }

        public ColumnGrip()
        {
            Background = new SolidColorBrush(Colors.Transparent);

            _line = new Border
            {
                Opacity = 0.9,
                Background = ResolveLineBrush(),
            };

            // The line is added at index 0 so the (transparent) hit area of the Grid itself
            // stays on top and receives every pointer event.
            Children.Add(_line);

            ApplyOrientation(Orientation.Horizontal);

            PointerPressed += OnGripPointerPressed;
            PointerMoved += OnGripPointerMoved;
            PointerReleased += OnGripPointerReleased;
            PointerCaptureLost += (_, _) =>
            {
                // Capture can be lost without PointerReleased (window deactivation, another
                // control stealing the pointer). Reset through the same path so a host that
                // disabled its animation on DragStarted always gets told to restore it.
                if (!_dragging) return;
                _dragging = false;
                DragCompleted?.Invoke(this, EventArgs.Empty);
            };
        }

        /// <summary>
        /// A horizontal divider is a 1px vertical rule centred in a narrow column; a vertical
        /// divider is a 1px horizontal rule centred in a short row. The cursor follows suit so
        /// the drag direction is obvious before the user commits to a drag.
        /// </summary>
        private void ApplyOrientation(Orientation orientation)
        {
            bool horizontal = orientation == Orientation.Horizontal;

            ProtectedCursor = InputSystemCursor.Create(horizontal
                ? InputSystemCursorShape.SizeWestEast
                : InputSystemCursorShape.SizeNorthSouth);

            if (horizontal)
            {
                _line.Width = 1;
                _line.Height = double.NaN;
                _line.HorizontalAlignment = HorizontalAlignment.Center;
                _line.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                _line.Width = double.NaN;
                _line.Height = 1;
                _line.HorizontalAlignment = HorizontalAlignment.Stretch;
                _line.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        private static Brush ResolveLineBrush()
        {
            try
            {
                if (Application.Current.Resources.TryGetValue("DividerStrokeColorDefaultBrush", out var value)
                    && value is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Not fatal: fall through to a neutral translucent rule.
            }
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A));
        }

        private void OnGripPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(null);
            if (point.Properties.IsLeftButtonPressed && CapturePointer(e.Pointer))
            {
                _dragging = true;
                _lastPosition = AlongAxis(point.Position);
                e.Handled = true;
                DragStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnGripPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_dragging) return;

            // Position is measured against the window rather than this element, because the
            // element itself moves when the boundary it sits on changes size.
            double position = AlongAxis(e.GetCurrentPoint(null).Position);
            double delta = position - _lastPosition;
            if (Math.Abs(delta) >= 0.5)
            {
                _lastPosition = position;
                DragDelta?.Invoke(this, delta);
            }
            e.Handled = true;
        }

        private void OnGripPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_dragging) return;
            // Clear the flag before releasing capture: the release raises PointerCaptureLost
            // synchronously, and that handler fires DragCompleted when it still sees a drag in
            // progress. Without this ordering the event would be raised twice.
            _dragging = false;
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
            DragCompleted?.Invoke(this, EventArgs.Empty);
        }

        private double AlongAxis(Windows.Foundation.Point position) =>
            Orientation == Orientation.Horizontal ? position.X : position.Y;
    }
}
