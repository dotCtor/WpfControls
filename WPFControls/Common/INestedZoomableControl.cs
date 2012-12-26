using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Windows.Controls.Common
{
    public interface INestedZoomableControl
    {
        int NestedLevel { get; set; }
    }
}
