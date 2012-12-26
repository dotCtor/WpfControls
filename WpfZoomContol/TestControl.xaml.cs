using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Common;

namespace WpfZoomContol
{
    /// <summary>
    /// Interaction logic for TestControl.xaml
    /// </summary>
    public partial class TestControl : UserControl, INotifyZoomChanged , INestedZoomableControl
    {
        #region Dependency Properties
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(TestControl), new UIPropertyMetadata(1.0,new PropertyChangedCallback(ScalePropertyChanged)));
        #endregion

        #region CTOR

        public TestControl()
        {
            InitializeComponent();

            this.MouseDown += (s, e) => 
            {
                if (e.ClickCount > 1)
                {
                    e.Handled = true;
                    ZoomManager.ZoomPanel.ZoomToObject(this);
                }
            };
        }

        #endregion

        #region INotfiyZoomChanged
        public event EventHandler ZoomStarted;

        public event EventHandler ZoomCompleted;

        public virtual void OnZoomStarted()
        {
            if (ZoomStarted != null)
            {
                ZoomStarted(this, new EventArgs());
            }
        }

        public virtual void OnZoomCompleted()
        {
            if (ZoomCompleted != null)
            {
                ZoomCompleted(this, new EventArgs());
            }
            var t = new TestControl();
            LayoutRoot.Children.Add(t);
            t.Scale = 1 / ZoomManager.ZoomPanel.GetScaleFactor(t);
            t.NestedLevel = NestedLevel + 1;
        }
        #endregion

        #region PropertyChangedCallback
        private static void ScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TestControl).LayoutTransform = new ScaleTransform((double)e.NewValue,(double)e.NewValue);
        }
        #endregion

        #region INestedZoomableControl
        private int _nestedLevel = 0;
        public int NestedLevel
        {
            get { return _nestedLevel; }
            set { _nestedLevel = value; }
        }
        #endregion
    }
}
