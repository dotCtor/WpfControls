using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Threading;
using System.Timers;

namespace System.Windows.Controls
{
    public enum NavigationTriggerMode
    {
        None,
        ArrowKeys,
        MouseClick,
        MouseDrag
    }

    public class FlexyPanel : Panel
    {
        #region Private Fields
        private int _maxZIndex;
        private int _currentPageIndex;
        private bool _isMouseDownOnChild;
        private bool _isAnimating;
        private double _maxPageWidth;
        private double _draggedDistance;
        private Point _mouseDragStartPosition;
        private int _dragMoveStartedPageIndex;
        private Point _dragMoveStartPosition;
        private Thickness _dragMoveStartMargin;
        private FrameworkElement _mouseClickedChild;
        private List<FlexyPanelPage> _pages;
        private Dictionary<FrameworkElement, Thickness> _dragMovedElements;

        #endregion

        #region Public Properties
        public int PageCount { get { return _pages.Count; } }
        #endregion

        #region Dependency Properties

        public Thickness ItemMargin
        {
            get { return (Thickness)GetValue(ItemMarginProperty); }
            set { SetValue(ItemMarginProperty, value); }
        }
        public static readonly DependencyProperty ItemMarginProperty =
            DependencyProperty.Register("ItemMargin", typeof(Thickness), typeof(FlexyPanel), new UIPropertyMetadata(new Thickness(5.0)));

        public Duration NavigationAnimationDuration
        {
            get { return (Duration)GetValue(NavigationAnimationDurationProperty); }
            set { SetValue(NavigationAnimationDurationProperty, value); }
        }
        public static readonly DependencyProperty NavigationAnimationDurationProperty =
            DependencyProperty.Register("NavigationAnimationDuration", typeof(Duration), typeof(FlexyPanel), new UIPropertyMetadata(new Duration(TimeSpan.FromSeconds(0.75))));

        public NavigationTriggerMode NavigationTriggerMode
        {
            get { return (NavigationTriggerMode)GetValue(NavigationTriggerModeProperty); }
            set { SetValue(NavigationTriggerModeProperty, value); }
        }
        public static readonly DependencyProperty NavigationTriggerModeProperty =
            DependencyProperty.Register("NavigationTriggerMode", typeof(NavigationTriggerMode), typeof(FlexyPanel), new UIPropertyMetadata(NavigationTriggerMode.None));

        public bool IsNavigationAnimated
        {
            get { return (bool)GetValue(IsNavigationAnimatedProperty); }
            set { SetValue(IsNavigationAnimatedProperty, value); }
        }
        public static readonly DependencyProperty IsNavigationAnimatedProperty =
            DependencyProperty.Register("IsNavigationAnimated", typeof(bool), typeof(FlexyPanel), new UIPropertyMetadata(true));

        public bool AllowDragMoveChild
        {
            get { return (bool)GetValue(AllowDragMoveChildProperty); }
            set { SetValue(AllowDragMoveChildProperty, value); }
        }
        public static readonly DependencyProperty AllowDragMoveChildProperty =
            DependencyProperty.Register("AllowDragMoveChild", typeof(bool), typeof(FlexyPanel), new UIPropertyMetadata(false));

        #endregion

        #region Event Definitions
        public event EventHandler PageNavigated;
        public event EventHandler PageNavigating;
        #endregion

        #region CTOR
        public FlexyPanel()
        {
            _maxZIndex = 0; 
            _maxPageWidth = 0;
            _currentPageIndex = 0;
            _pages = new List<FlexyPanelPage>();
            _dragMovedElements = new Dictionary<FrameworkElement, Thickness>();
            this.Focusable = true;
            Keyboard.Focus(this);
        }
        #endregion

        #region Private Methods

        private void MeasureChildren(Size availableSize)
        {
            _pages = new List<FlexyPanelPage>();
            double tempWidth = 0;
            double tempHeight = 0;
            int currentPageIndex = 0;
            int currentRowIndex = 0;
            int maxRowCount = 2;

            List<FlexyPanelPageRow> allRows = new List<FlexyPanelPageRow>();

            FlexyPanelPageRow tempRow = new FlexyPanelPageRow();
            tempRow.Children = new List<FrameworkElement>();

            FlexyPanelPage tempPage = new FlexyPanelPage();
            tempPage.Rows = new List<FlexyPanelPageRow>();

            foreach (FrameworkElement child in InternalChildren)
            {
                double estimatedWidth = tempWidth + child.Width + ItemMargin.Left + ItemMargin.Right;
                tempHeight = child.Height + ItemMargin.Top + ItemMargin.Bottom;
                if (estimatedWidth < availableSize.Width)
                {
                    tempWidth = estimatedWidth;
                    tempRow.Children.Add(child);
                    tempRow.Height = Math.Max(child.Height + ItemMargin.Top + ItemMargin.Bottom, tempRow.Height);
                    tempRow.Width = tempWidth;
                }
                else
                {
                    allRows.Add(tempRow);
                    tempRow = new FlexyPanelPageRow();
                    tempRow.Children = new List<FrameworkElement>();
                    tempRow.Children.Add(child);
                    tempRow.Height = Math.Max(child.Height + ItemMargin.Top + ItemMargin.Bottom, tempRow.Height);
                    tempWidth = child.Width + ItemMargin.Right + ItemMargin.Left;
                }
            }

            allRows.Add(tempRow);

            foreach (var row in allRows)
            {
                if (currentRowIndex < maxRowCount)
                {
                    tempPage.Rows.Add(row);
                    currentRowIndex++;
                }
                else
                {
                    tempPage.Index = currentPageIndex++;
                    currentRowIndex = 0;
                    _pages.Add(tempPage);
                    tempPage = new FlexyPanelPage();
                    tempPage.Rows = new List<FlexyPanelPageRow>();
                    tempPage.Rows.Add(row);
                    currentRowIndex++;
                }
            }
            tempPage.Index = currentPageIndex++;
            _pages.Add(tempPage);
        }

        private void ArrengeChildren(Size finalSize)
        {
            int rowIndex;
            double baseXPosition;
            double baseYPosition;
            foreach (var page in _pages)
            {
                rowIndex = 0;
                baseYPosition = (finalSize.Height - page.Height) / 2;
                foreach (var row in page.Rows)
                {
                    baseXPosition = (finalSize.Width - page.Width) / 2 + page.Index * this.Width;
                    foreach (var child in row.Children)
                    {
                        child.Arrange(new Rect(baseXPosition,baseYPosition,double.MaxValue,finalSize.Height));
                        baseXPosition += child.Width + ItemMargin.Left + ItemMargin.Right;
                    }
                    rowIndex++;
                    baseYPosition += row.Height;
                }
            }
        }

        #endregion

        #region Public Methods

        public void NavigateToPage(int index) { NavigateToPage(index, IsNavigationAnimated); }

        public void NavigateToPage(int index,bool isAnimated)
        {
            OnPageNavigating();
            _isAnimating = isAnimated;
            int currentChildIndex = 0;
            double animationDelaySeconds = NavigationAnimationDuration.TimeSpan.TotalSeconds * 0.008;
            foreach (var page in _pages)
            {
                TimeSpan animationDelay = TimeSpan.FromSeconds(0);
                foreach (var row in page.Rows)
                {
                    foreach (var child in row.Children)
                    {
                        bool isDragMoving = AllowDragMoveChild && _isMouseDownOnChild && child.Equals(_mouseClickedChild);
                        if (isDragMoving)
                        {
                            ++currentChildIndex;
                            continue;
                        }
                        TranslateTransform tt = child.RenderTransform as TranslateTransform;
                        if (isAnimated)
                        {
                            DoubleAnimation d = new DoubleAnimation();
                            BackEase bEase = new BackEase();
                            bEase.EasingMode = EasingMode.EaseOut;
                            bEase.Amplitude = 0.3;
                            d.EasingFunction = bEase;
                            d.Duration = NavigationAnimationDuration;
                            d.From = tt.X;
                            d.To = -index * Width;
                            d.BeginTime = animationDelay;
                            d.Completed += (s, e) => 
                            {
                                if (++currentChildIndex == InternalChildren.Count)
                                {
                                    _isAnimating = false;
                                    OnPageNavigated();
                                }
                            };
                            tt.BeginAnimation(TranslateTransform.XProperty, d);
                            animationDelay += TimeSpan.FromSeconds(animationDelaySeconds);
                        }
                        else
                        {
                            tt.X = -index * Width;
                        }
                        _currentPageIndex = index;
                    }
                }
            }
            _dragMovedElements.Clear();
        }

        public void NavigateToNextPage() { NavigateToNextPage(IsNavigationAnimated); }

        public void NavigateToNextPage(bool isAnimated)
        {
            if (_currentPageIndex < _pages.Count -1)
            {
                NavigateToPage(_currentPageIndex + 1,isAnimated);
            }
        }

        public void NavigateToPreviousPage() { NavigateToPreviousPage(IsNavigationAnimated); }

        public void NavigateToPreviousPage(bool isAnimated)
        {
            if (_currentPageIndex > 0)
            {
                NavigateToPage(_currentPageIndex - 1, isAnimated);
            }
        }

        #endregion

        #region Override Methods
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            (VisualParent as FrameworkElement).SizeChanged += (s, e) =>
            {
                this.Width = (VisualParent as FrameworkElement).ActualWidth;
                this.Height = (VisualParent as FrameworkElement).ActualHeight;
                if (_currentPageIndex != 0)
                {
                    NavigateToPage(0);
                }
            };
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.Width = (VisualParent as FrameworkElement).ActualWidth;
            this.Height = (VisualParent as FrameworkElement).ActualHeight;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize.Width = double.IsPositiveInfinity(availableSize.Width) ? 640 : availableSize.Width;
            availableSize.Height = double.IsPositiveInfinity(availableSize.Width) ? 480 : availableSize.Height;

            MeasureChildren(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            ArrengeChildren(finalSize);
            if (!_isMouseDownOnChild || !_isAnimating)
            {
                NavigateToPage(_currentPageIndex,false);
            }
            return finalSize;
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            if (visualAdded != null)
            {
                FrameworkElement visual = visualAdded as FrameworkElement;
                Canvas.SetZIndex(visual, _maxZIndex);
                visual.RenderTransform = new TranslateTransform();
                visual.VerticalAlignment = Windows.VerticalAlignment.Top;
                visual.HorizontalAlignment = Windows.HorizontalAlignment.Left;
                visual.MouseDown += (s, e) =>
                {
                    double currentZIndex = Canvas.GetZIndex(visual);
                    Canvas.SetZIndex(visual, ++_maxZIndex);
                    _isMouseDownOnChild = true;
                    _mouseClickedChild = visual;
                    _dragMoveStartMargin = visual.Margin;
                    _dragMoveStartedPageIndex = _currentPageIndex;
                    _dragMoveStartPosition = e.GetPosition(null);
                };
                visual.MouseMove += (s, e) =>
                {
                    if (AllowDragMoveChild && _isMouseDownOnChild && e.LeftButton == Input.MouseButtonState.Pressed && s.Equals(_mouseClickedChild))
                    {
                        Point currentMousePosition = e.GetPosition(null);
                        double diffX = currentMousePosition.X - _dragMoveStartPosition.X;
                        double diffY = currentMousePosition.Y - _dragMoveStartPosition.Y;
                        Thickness tempMargin = _dragMoveStartMargin;
                        tempMargin.Left = _dragMoveStartMargin.Left + diffX;
                        tempMargin.Top = _dragMoveStartMargin.Top + diffY;
                        visual.Margin = tempMargin;
                        double mouseXPostion = e.GetPosition(this).X;
                        if (mouseXPostion < 50)
                        {
                            if (!_isAnimating)
                            {
                                NavigateToPreviousPage();
                            }
                        }
                        else if (mouseXPostion > this.ActualWidth - 50)
                        {
                            if (!_isAnimating)
                            {
                                NavigateToNextPage();
                            }
                        }
                    }
                };
                visual.MouseUp += (s, e) => 
                {
                    if (_isMouseDownOnChild && _mouseClickedChild.Equals(visual))
                    {
                        e.Handled = true;
                        _isMouseDownOnChild = false;
                        int pageIndex = _currentPageIndex;
                        Thickness margin = visual.Margin;
                        margin.Left += (_currentPageIndex - _dragMoveStartedPageIndex) * this.Width;
                        //_dragMovedElements[visual] = margin;
                        visual.Margin = margin;
                        Duration tempDuration = NavigationAnimationDuration;
                        NavigationAnimationDuration = new Duration(TimeSpan.FromSeconds(0));
                        NavigateToPage(pageIndex,true);
                        NavigationAnimationDuration = tempDuration;
                    }
                };
            }
        }

        protected override void OnMouseDown(Input.MouseButtonEventArgs e)
        {
            Keyboard.Focus(this);
            if (NavigationTriggerMode == Controls.NavigationTriggerMode.MouseClick)
            {
                if (e.LeftButton == Input.MouseButtonState.Pressed)
                {
                    NavigateToPreviousPage();
                }
                else if (e.RightButton == Input.MouseButtonState.Pressed)
                {
                    NavigateToNextPage();
                }
            }
            else if (NavigationTriggerMode == Controls.NavigationTriggerMode.MouseDrag)
            {
                _mouseDragStartPosition = e.GetPosition(null);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(Input.MouseEventArgs e)
        {
            if (e.LeftButton == Input.MouseButtonState.Pressed && NavigationTriggerMode == Controls.NavigationTriggerMode.MouseDrag)
            {
                e.Handled = true;
                Point currentPosition = e.GetPosition(null);
                double baseX = _currentPageIndex * this.Width;
                double diffX = currentPosition.X - _mouseDragStartPosition.X;
                //foreach (FrameworkElement child in InternalChildren)
                //{
                //    TranslateTransform tt = child.RenderTransform as TranslateTransform;
                //    tt.X = baseX + diffX;
                //    tt.X = 500;
                //}
                _draggedDistance = diffX;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(Input.MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            bool dragNavigated = Math.Abs(_draggedDistance) > this.Width / 20;
            if (dragNavigated && NavigationTriggerMode == Controls.NavigationTriggerMode.MouseDrag)
            {
                if (_draggedDistance < 0)
                {
                    NavigateToNextPage();
                }
                else
                {
                    NavigateToPreviousPage();
                }
            }
        }

        protected override void OnKeyDown(Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (NavigationTriggerMode == Controls.NavigationTriggerMode.ArrowKeys)
            {
                if (e.Key == Input.Key.Left)
                {
                    NavigateToPreviousPage();
                }
                else if (e.Key == Input.Key.Right)
                {
                    NavigateToNextPage();
                }
            }
        }
        #endregion

        #region Virtual Methods
        public virtual void OnPageNavigating()
        {
            if (PageNavigating != null)
            {
                PageNavigating(this,new EventArgs());
            }
        }

        public virtual void OnPageNavigated()
        {
            if (PageNavigated != null)
            {
                PageNavigated(this, new EventArgs());
            }
        }
        #endregion

        #region PropertyChangedCallback

        private static void PageWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlexyPanel flexyPanel = d as FlexyPanel;
            double pageWidth = (double)e.NewValue;
            flexyPanel._maxPageWidth = pageWidth;
        }

        #endregion
    }
}
