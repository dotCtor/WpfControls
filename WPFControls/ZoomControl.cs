using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Windows.Controls.Common;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    [TemplatePart(Name = ZoomControl.RootElementPart, Type = typeof(FrameworkElement))]
    public class ZoomControl : ContentControl
    {
        #region TemplatePart Names
        private const string RootElementPart = "RootElement";
        #endregion

        #region Template Parts
        FrameworkElement _rootElement;
        #endregion

        #region Private Fields
        private ScaleTransform _scaleTrasform;
        private TransformGroup _transformGroup;
        #endregion

        #region Dependency Properties

        public double ZoomFactor
        {
            get 
            {
                return (double)GetValue(ZoomFactorProperty);
            }
            set 
            {
                SetValue(ZoomFactorProperty, value);
            }
        }
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register("ZoomFactor", typeof(double), typeof(ZoomControl), new UIPropertyMetadata(1.0,new PropertyChangedCallback(ZoomFactorPropertyChanged)));

        public Duration AnimationDuration
        {
            get { return (Duration)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(Duration), typeof(ZoomControl), new UIPropertyMetadata(new Duration(TimeSpan.FromSeconds(0.5))));

        public double PositionX
        {
            get { return (double)GetValue(PositionXProperty); }
            set { SetValue(PositionXProperty, value); }
        }
        public static readonly DependencyProperty PositionXProperty =
            DependencyProperty.Register("PositionX", typeof(double), typeof(ZoomControl), new UIPropertyMetadata(0.0, new PropertyChangedCallback(PositionXPropertyChanged)));

        public double PositionY
        {
            get { return (double)GetValue(PositionYProperty); }
            set { SetValue(PositionYProperty, value); }
        }
        public static readonly DependencyProperty PositionYProperty =
            DependencyProperty.Register("PositionY", typeof(double), typeof(ZoomControl), new UIPropertyMetadata(0.0, new PropertyChangedCallback(PositionYPropertyChanged)));

        public bool UseAnimations
        {
            get { return (bool)GetValue(UseAnimationsProperty); }
            set { SetValue(UseAnimationsProperty, value); }
        }
        public static readonly DependencyProperty UseAnimationsProperty =
            DependencyProperty.Register("UseAnimations", typeof(bool), typeof(ZoomControl), new UIPropertyMetadata(true));
        #endregion

        #region Events

        public event EventHandler ZoomStarted;
        public event EventHandler ZoomCompleted;
        
        #endregion

        #region Animations
        private DoubleAnimation _animZoom;
        private ThicknessAnimation _animMargin;
        private DoubleAnimation _animPositionX;
        private DoubleAnimation _animPositionY;
        #endregion

        #region CTOR
        public ZoomControl()
        {
            _scaleTrasform = new ScaleTransform();
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTrasform);

            _animZoom = new DoubleAnimation();
            _animPositionX = new DoubleAnimation();
            _animPositionY = new DoubleAnimation();
            _animMargin = new ThicknessAnimation();
            _animZoom.Duration = _animMargin.Duration = _animPositionX.Duration = _animPositionX.Duration = AnimationDuration;
            _animZoom.EasingFunction = _animMargin.EasingFunction = new CircleEase();

            _animZoom.Completed += new EventHandler(_animZoom_Completed);        
        }

        static ZoomControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomControl), new FrameworkPropertyMetadata(typeof(ZoomControl)));
        }
        #endregion

        #region Virtual Methods

        public virtual void OnZoomStarted()
        {
            if (ZoomStarted != null)
            {
                ZoomStarted(this,new EventArgs());
            }
        }

        public virtual void OnZoomCompleted()
        {
            if (ZoomCompleted != null)
            {
                ZoomCompleted(this, new EventArgs());
            }
        }

        #endregion

        #region Override Methods
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rootElement = this.GetTemplateChild(ZoomControl.RootElementPart) as FrameworkElement;

            if (Content != null)
            {
                _rootElement.LayoutTransform = _transformGroup;
                _rootElement.HorizontalAlignment = Windows.HorizontalAlignment.Stretch;
                _rootElement.VerticalAlignment = Windows.VerticalAlignment.Stretch;
            }
            _scaleTrasform.ScaleX = _scaleTrasform.ScaleY = ZoomFactor;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _rootElement.Width = sizeInfo.NewSize.Width;
                _rootElement.Height = sizeInfo.NewSize.Height;
            }
        }
        #endregion

        #region Public Methods

        public double GetScaleFactor(FrameworkElement visual)
        {
            GeneralTransform gt = visual.TransformToAncestor(_rootElement);
            Point point = gt.Transform(new Point());
            Size size = new Size(visual.Width, visual.Height);

            double scaleX = _rootElement.ActualWidth / size.Width;
            double scaleY = _rootElement.ActualHeight / size.Height;
            double scaleFactor = Math.Min(scaleX, scaleY);

            return scaleFactor;
        }

        public double GetActualScaleFactor(FrameworkElement visual)
        {
            GeneralTransform gt = visual.TransformToAncestor(_rootElement);
            Point point = gt.Transform(new Point());
            Size size = new Size(visual.ActualWidth, visual.ActualHeight);

            double scaleX = _rootElement.ActualWidth / size.Width;
            double scaleY = _rootElement.ActualHeight / size.Height;
            double scaleFactor = Math.Min(scaleX, scaleY);

            return scaleFactor;
        }

        public void Zoom(double zoomFactor) { Zoom(zoomFactor, UseAnimations); }

        public void Zoom(double zoomFactor, bool isAnimated)
        {
            if (isAnimated)
            {
                _animZoom.From = ZoomFactor;
                _animZoom.To = zoomFactor;
                OnZoomStarted();
                this.BeginAnimation(ZoomFactorProperty, _animZoom);
            }
            else
            {
                ZoomFactor = zoomFactor;
            }
        }

        public void ZoomToObject(FrameworkElement targetObject) { ZoomToObject(targetObject, UseAnimations); }

        public void ZoomToObject(FrameworkElement targetObject, bool isAnimated)
        {
            if (!isAnimated)
            {
                GeneralTransform gt = targetObject.TransformToAncestor(_rootElement);
                Point point = gt.Transform(new Point());
                Size size = new Size(targetObject.ActualWidth, targetObject.ActualHeight);

                double zoomX = _rootElement.ActualWidth / size.Width;
                double zoomY = _rootElement.ActualHeight / size.Height;
                double zoomFactor = Math.Min(zoomX, zoomY);

                Thickness margin = new Thickness();
                double diffScreenY = (_rootElement.ActualHeight - zoomFactor * size.Height);
                double diffScreenX = (_rootElement.ActualWidth - zoomFactor * size.Width);

                margin.Top = -((point.Y * zoomFactor) - ((diffScreenY / 2)));
                margin.Left = -((point.X * zoomFactor) - ((diffScreenX / 2)));

                ZoomFactor = zoomFactor;
                _rootElement.Margin = margin;
                _rootElement.UpdateLayout();
            }
            else
            {
                ZoomToObjectAnimated(targetObject);
            }
        }

        public void ResetZoom() { ResetZoom(UseAnimations); }

        public void ResetZoom(bool isAnimated)
        {
            if (isAnimated)
            {
                _animZoom.From = ZoomFactor;
                _animZoom.To = 1;

                _animMargin.To = new Thickness(0);
                _animMargin.From = _rootElement.Margin;

                _rootElement.BeginAnimation(FrameworkElement.MarginProperty, _animMargin);
                this.BeginAnimation(ZoomFactorProperty, _animZoom);
                OnZoomStarted();
            }
            else
            {
                ZoomFactor = 1;
                _rootElement.Margin = new Thickness(0);
            }
        }

        public void ResetZoomAnimated()
        {
            DoubleAnimation animZoom = new DoubleAnimation();
            animZoom.From = ZoomFactor;
            animZoom.To = 1;
            animZoom.Duration = new Duration(TimeSpan.FromSeconds(0.5));

            _scaleTrasform.BeginAnimation(ScaleTransform.ScaleXProperty, animZoom);
            _scaleTrasform.BeginAnimation(ScaleTransform.ScaleYProperty, animZoom);

            ThicknessAnimation animMargin = new ThicknessAnimation();
            animMargin.To = new Thickness(0);
            animMargin.From = _rootElement.Margin;
            animMargin.Duration = new Duration(TimeSpan.FromSeconds(0.5));

            _rootElement.BeginAnimation(FrameworkElement.MarginProperty, animMargin);
        }

        #endregion

        #region Private Methods
        private void ZoomToObjectAnimated(FrameworkElement targetObject)
        {
            if (_rootElement != null)
            {
                double objectsActualCurrentZoom = 1;
                if (targetObject.LayoutTransform is ScaleTransform)
                {
                    objectsActualCurrentZoom = (targetObject.LayoutTransform as ScaleTransform).ScaleX;
                    if (targetObject is INestedZoomableControl)
                    {
                        objectsActualCurrentZoom = 1 / Math.Pow(GetScaleFactor(targetObject), (targetObject as INestedZoomableControl).NestedLevel);
                    }
                }

                GeneralTransform gt = targetObject.TransformToAncestor(_rootElement);
                Point point = gt.Transform(new Point());
                Size size = new Size(targetObject.ActualWidth * objectsActualCurrentZoom, targetObject.ActualHeight * objectsActualCurrentZoom);

                double zoomX = _rootElement.ActualWidth / size.Width;
                double zoomY = _rootElement.ActualHeight / size.Height;
                double zoomFactor = Math.Min(zoomX, zoomY);

                Thickness margin = new Thickness();
                double diffScreenY = (_rootElement.ActualHeight - zoomFactor * size.Height);
                double diffScreenX = (_rootElement.ActualWidth - zoomFactor * size.Width);

                margin.Top = -((point.Y * zoomFactor) - ((diffScreenY / 2)));
                margin.Left = -((point.X * zoomFactor) - ((diffScreenX / 2)));

                _animZoom.From = ZoomFactor;
                _animZoom.To = zoomFactor;

                _animMargin.To = margin;
                _animMargin.From = _rootElement.Margin;

                _animPositionX.To = margin.Left;
                _animPositionX.From = PositionX;

                _animPositionY.To = margin.Top;
                _animPositionY.From = PositionY;

                OnZoomStarted();
                this.BeginAnimation(ZoomFactorProperty, _animZoom);
                _rootElement.BeginAnimation(FrameworkElement.MarginProperty, _animMargin);

                if (targetObject is INotifyZoomChanged)
                {
                    (targetObject as INotifyZoomChanged).OnZoomStarted();
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = AnimationDuration.TimeSpan;
                    timer.Tick += (s, e) =>
                    {
                        (targetObject as INotifyZoomChanged).OnZoomCompleted();
                        OnZoomCompleted();
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }
        #endregion

        #region Event Hadnlers
        void _animZoom_Completed(object sender, EventArgs e)
        {
            OnZoomCompleted();
        }
        #endregion

        #region PropertyChanged Callbacks

        private static void ZoomFactorPropertyChanged(DependencyObject d,DependencyPropertyChangedEventArgs e) 
        {
            ZoomControl zoomControl = d as ZoomControl;
            zoomControl._scaleTrasform.ScaleX = zoomControl._scaleTrasform.ScaleY = (double)e.NewValue;
        }

        private static void PositionXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomControl zoomControl = d as ZoomControl;
            double positionX = (double)e.NewValue;
            Thickness t = zoomControl._rootElement.Margin;
            t.Left = positionX;
            zoomControl._rootElement.Margin = t;
        }

        private static void PositionYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomControl zoomControl = d as ZoomControl;
            double positionY = (double)e.NewValue;
            Thickness t = zoomControl._rootElement.Margin;
            t.Top = positionY;
            zoomControl._rootElement.Margin = t;
        }

        #endregion
    }
}
