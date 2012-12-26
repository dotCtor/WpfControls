using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Windows.Controls.Common
{
    public interface INotifyDragMove
    {
        event EventHandler DragMoveStarted;
        event EventHandler DragMoveCompleted;

        void OnDragMoveStarted();
        void OnDragMoveCompleted();
    }
}
