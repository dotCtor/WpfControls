using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Windows.Controls
{
    struct FlexyPanelPage
    {
        private double _width;
        public double Width 
        {
            get
            {
                _width = 0;
                if (Rows != null)
                {
                    foreach (var row in Rows)
                    {
                        _width = Math.Max(_width, row.Width);
                    }
                }
                return _width;
            }
        }
        private double _height;
        public double Height
        {
            get 
            {
                _height = 0;
                if (Rows != null)
                {
                    foreach (var row in Rows)
                    {
                        _height += row.Height;
                    }
                }
                return _height;
            }
        }
        public int Index { get; set; }
        public List<FrameworkElement> Children { get; set; }
        public List<FlexyPanelPageRow> Rows { get; set; }
    }

    struct FlexyPanelPageRow
    {
        public List<FrameworkElement> Children { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
    }
}
