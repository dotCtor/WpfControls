using System.Windows;

namespace WpfZoomContol
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
            ZoomManager.ZoomPanel = zoomControl;

            for (int i = 0; i < 19; i++)
            {
                TestControl t = new TestControl();
                wrapPanel.Children.Add(t);
            }

            zoomControl.ZoomCompleted += (s, e) => 
            {
                //wrapPanel.ZoomedFactor = zoomControl.ZoomFactor;
            };

            zoomControl.ZoomToObject(ParentGrid);

            this.MouseDown += (s, e) =>
            {
                if (e.ClickCount > 1)
                {
                    zoomControl.ResetZoom();
                }
                else
                {
                    //wrapPanel.ZoomedFactor = ZoomManager.ZoomPanel.ZoomFactor;
                }
            };
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            wrapPanel.Width = sizeInfo.NewSize.Width;
            wrapPanel.Height = sizeInfo.NewSize.Height;
        }
    }
}
