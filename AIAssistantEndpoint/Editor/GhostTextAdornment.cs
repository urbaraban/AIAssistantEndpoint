using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text;

namespace AIAssistantEndpoint.Editor
{
    internal class GhostTextAdornment
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private TextBlock _adornmentElement;
        private SnapshotPoint _position;
        private string _suggestion;

        public GhostTextAdornment(IWpfTextView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layer = view.GetAdornmentLayer("AIGhostTextLayer");
            _view.LayoutChanged += OnLayoutChanged;
            _view.Caret.PositionChanged += OnCaretPositionChanged;
        }

        public void UpdateSuggestion(string suggestion)
        {
            _suggestion = suggestion ?? string.Empty;
            Show();
        }

        public void Clear()
        {
            _suggestion = null;
            RemoveAdornment();
        }

        public void Accept()
        {
            if (string.IsNullOrEmpty(_suggestion)) return;
            // Insert suggestion at caret
            var buffer = _view.TextBuffer;
            var caretPos = _view.Caret.Position.BufferPosition.Position;
            using (var edit = buffer.CreateEdit())
            {
                edit.Insert(caretPos, _suggestion);
                edit.Apply();
            }
            Clear();
        }

        internal bool HasSuggestion => !string.IsNullOrEmpty(_suggestion);

        private void Show()
        {
            RemoveAdornment();

            if (string.IsNullOrEmpty(_suggestion))
                return;

            // Create visual
            _adornmentElement = new TextBlock(new Run(_suggestion))
            {
                Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128)),
                FontFamily = new FontFamily("Consolas"),
                Opacity = 0.7,
                IsHitTestVisible = false
            };

            var caret = _view.Caret.Position.BufferPosition;
            _position = caret;
            var span = new SnapshotSpan(caret, 0);
            var geometry = _view.TextViewLines.GetMarkerGeometry(span);
            if (geometry != null)
            {
                Canvas.SetLeft(_adornmentElement, geometry.Bounds.Left);
                Canvas.SetTop(_adornmentElement, geometry.Bounds.Top);
            }

            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, _adornmentElement, null);
        }

        private void RemoveAdornment()
        {
            try
            {
                _layer.RemoveAllAdornments();
            }
            catch { }
            _adornmentElement = null;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // Reposition adornment if caret moved
            if (_adornmentElement == null) return;
            try
            {
                var caret = _view.Caret.Position.BufferPosition;
                var span = new SnapshotSpan(caret, 0);
                var geometry = _view.TextViewLines.GetMarkerGeometry(span);
                if (geometry != null)
                {
                    Canvas.SetLeft(_adornmentElement, geometry.Bounds.Left);
                    Canvas.SetTop(_adornmentElement, geometry.Bounds.Top);
                }
            }
            catch { }
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // move adornment with caret
            if (_adornmentElement == null) return;
            try
            {
                var caret = _view.Caret.Position.BufferPosition;
                var span = new SnapshotSpan(caret, 0);
                var geometry = _view.TextViewLines.GetMarkerGeometry(span);
                if (geometry != null)
                {
                    Canvas.SetLeft(_adornmentElement, geometry.Bounds.Left);
                    Canvas.SetTop(_adornmentElement, geometry.Bounds.Top);
                }
            }
            catch { }
        }
    }
}
