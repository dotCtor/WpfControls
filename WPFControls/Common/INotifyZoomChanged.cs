namespace System.Windows.Controls.Common
{
    public interface INotifyZoomChanged
    {
        event EventHandler ZoomStarted;
        event EventHandler ZoomCompleted;

        void OnZoomStarted();
        void OnZoomCompleted();
    }
}
