using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text;
using System.Threading.Tasks;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Services;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIAssistantEndpoint.Editor
{
    internal class MethodActionAdornment
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private Popup _tooltipPopup;
        private Button _actionButton;
        private bool _isBusy = false;
        private DispatcherTimer _hoverTimer;
        private SyntaxNode _currentMethodNode;
        private SnapshotPoint? _lastMousePosition;

        public MethodActionAdornment(IWpfTextView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layer = view.GetAdornmentLayer("AIMethodActionLayer");
            
            // Проверяем, что это C# файл
            if (!_view.TextBuffer.ContentType.IsOfType("CSharp"))
                return;

            _view.MouseHover += OnMouseHover;
            _view.VisualElement.MouseLeave += OnMouseLeave;
            _view.VisualElement.MouseMove += OnMouseMove;

            // Таймер для задержки показа тултипа
            _hoverTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Задержка 500мс перед показом
            };
            _hoverTimer.Tick += OnHoverTimerTick;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var point = e.GetPosition(_view.VisualElement);
                var textViewLine = _view.TextViewLines.GetTextViewLineContainingYCoordinate(point.Y);
                if (textViewLine == null)
                {
                    HideTooltip();
                    return;
                }

                var bufferPosition = textViewLine.GetBufferPositionFromXCoordinate(point.X);
                if (!bufferPosition.HasValue)
                {
                    HideTooltip();
                    return;
                }

                _lastMousePosition = bufferPosition.Value;

                // Сбрасываем таймер и запускаем заново
                _hoverTimer.Stop();
                _hoverTimer.Start();
            }
            catch { }
        }

        private void OnMouseHover(object sender, MouseHoverEventArgs e)
        {
            // Этот обработчик может быть использован для дополнительной логики
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            HideTooltip();
            _hoverTimer.Stop();
            _lastMousePosition = null;
        }

        private void OnHoverTimerTick(object sender, EventArgs e)
        {
            _hoverTimer.Stop();
            if (_lastMousePosition.HasValue)
            {
                CheckAndShowTooltip(_lastMousePosition.Value);
            }
        }

        private void CheckAndShowTooltip(SnapshotPoint position)
        {
            try
            {
                var snapshot = position.Snapshot;
                var fullText = snapshot.GetText();

                // Проверяем, что это C# файл
                if (!_view.TextBuffer.ContentType.IsOfType("CSharp"))
                    return;

                // Parse source with Roslyn
                var tree = CSharpSyntaxTree.ParseText(fullText);
                var root = tree.GetRoot();

                // find the smallest method/constructor/local function node that contains position
                var methodNode = root.DescendantNodes()
                    .Where(n => n is MethodDeclarationSyntax || n is ConstructorDeclarationSyntax || n is LocalFunctionStatementSyntax)
                    .Cast<SyntaxNode>()
                    .OrderBy(n => n.Span.Length)
                    .FirstOrDefault(n => n.Span.Start <= position.Position && n.Span.End >= position.Position);

                if (methodNode != null)
                {
                    _currentMethodNode = methodNode;
                    ShowTooltipForNode(methodNode, position);
                }
                else
                {
                    HideTooltip();
                }
            }
            catch
            {
                HideTooltip();
            }
        }

        // helper to remove existing button/adornments
        private void HideTooltip()
        {
            try
            {
                if (_tooltipPopup != null)
                {
                    _tooltipPopup.IsOpen = false;
                    _tooltipPopup = null;
                }
            }
            catch { }
            _actionButton = null;
            _currentMethodNode = null;
        }

        private void ShowTooltipForNode(SyntaxNode node, SnapshotPoint position)
        {
            HideTooltip();

            if (_isBusy) return;

            try
            {
                var snapshot = position.Snapshot;
                var line = snapshot.GetLineFromPosition(node.Span.Start);
                
                // Получаем геометрию для позиционирования
                var span = new SnapshotSpan(line.Start, 0);
                var geometry = _view.TextViewLines.GetMarkerGeometry(span);
                
                if (geometry == null) return;

                // Создаем кнопку
                _actionButton = new Button
                {
                    Content = "Описать с помощью AiAssistant",
                    Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 6, 10, 6),
                    FontSize = 12,
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235))
                };

                _actionButton.Click += async (s, ev) =>
                {
                    HideTooltip();
                    await OnDescribeClickedAsync(node);
                };

                // Создаем контейнер для тултипа
                var container = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Child = _actionButton,
                };

                // Создаем Popup
                _tooltipPopup = new Popup
                {
                    Child = container,
                    Placement = PlacementMode.Absolute,
                    StaysOpen = true,
                    AllowsTransparency = true,
                    PopupAnimation = PopupAnimation.Fade
                };

                // Вычисляем позицию относительно экрана
                var screenPoint = _view.VisualElement.PointToScreen(new Point(geometry.Bounds.Left, geometry.Bounds.Top));
                var windowPoint = Application.Current.MainWindow.PointFromScreen(screenPoint);
                
                // Если не удалось получить позицию окна, используем относительные координаты
                if (windowPoint.X == 0 && windowPoint.Y == 0)
                {
                    _tooltipPopup.PlacementTarget = _view.VisualElement;
                    _tooltipPopup.Placement = PlacementMode.Relative;
                    _tooltipPopup.HorizontalOffset = geometry.Bounds.Left + 5;
                    _tooltipPopup.VerticalOffset = geometry.Bounds.Top - 35;
                }
                else
                {
                    _tooltipPopup.HorizontalOffset = screenPoint.X;
                    _tooltipPopup.VerticalOffset = screenPoint.Y - 35;
                }

                _tooltipPopup.IsOpen = true;
                
                // Закрываем при клике вне тултипа
                _tooltipPopup.Closed += (s, e) =>
                {
                    _tooltipPopup = null;
                    _actionButton = null;
                };
                
                // Устанавливаем фокус на кнопку, чтобы тултип оставался открытым
                _actionButton.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing tooltip: {ex}");
                HideTooltip();
            }
        }

        private async Task OnDescribeClickedAsync(SyntaxNode node)
        {
            if (_isBusy) return;
            _isBusy = true;
            try
            {
                if (_actionButton == null) return;

                _actionButton.IsEnabled = false;
                var prev = _actionButton.Content;
                _actionButton.Content = "Loading...";

                var snapshot = _view.TextBuffer.CurrentSnapshot;

                // extract node text using Roslyn spans
                var span = node.Span;
                int start = span.Start;
                int length = span.Length;
                var methodText = snapshot.GetText(start, length);

                await GenerateAndInsertCommentsAsync(start, methodText);

                _actionButton.Content = prev;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error describing method: {ex}");
            }
            finally
            {
                _isBusy = false;
                if (_actionButton != null) _actionButton.IsEnabled = true;
                HideTooltip();
            }
        }

        private async Task GenerateAndInsertCommentsAsync(int insertPosition, string methodText)
        {
            // build prompt
            var prompt = "Напиши краткий XML комментарий документации (в стиле трех слэшей C#) для следующего C# метода. Верни только строки комментария, начинающиеся с '///':\n\n" + methodText;

            // load settings
            var cfg = new JsonConfigurationManager();
            if (!cfg.SettingsExist())
            {
                MessageBox.Show("Настройки AI не настроены. Откройте Сервис → AI Assistant → Настройки.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = cfg.LoadSettings();
            if (settings == null)
            {
                MessageBox.Show("Не удалось загрузить настройки.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new ServerConnectionService(settings);
            if (!await service.ConnectAsync())
            {
                MessageBox.Show("Не удалось подключиться к AI серверу.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var response = await service.SendRequestAsync(prompt);

            if (string.IsNullOrWhiteSpace(response))
            {
                MessageBox.Show("AI вернул пустой ответ.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // normalize comment: ensure each line starts with '///'
            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("///"))
                    lines[i] = "/// " + lines[i];
            }

            // compute indentation of target line
            var snapshot2 = _view.TextBuffer.CurrentSnapshot;
            var line = snapshot2.GetLineFromPosition(insertPosition);
            var lineText = line.GetText();
            var indent = new string(lineText.TakeWhile(Char.IsWhiteSpace).ToArray());
            // apply indentation to all comment lines
            var indentedComment = string.Join(Environment.NewLine, lines.Select(l => indent + l)) + Environment.NewLine;

            // insert comment above the node start line with undo group
            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Insert(line.Start.Position, indentedComment);
                edit.Apply();
            }
        }
    }
}
