using System.Drawing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

//using System.Drawing;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    class OutBoxAdorner : Adorner
    {
        static OutBoxAdorner()
        {
            Adorner.OpacityProperty.OverrideMetadata(typeof(OutBoxAdorner), new FrameworkPropertyMetadata(0.7));
        }


        private Pen pen = new Pen(Brushes.LightGreen, 2);

        public OutBoxAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        public Rectangle Rectangle { get; set; }

        public Pen Pen
        {
            get { return pen; }
            set { pen = value; }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var rectangle = Rectangle;
            rectangle.Inflate(1, 1);
            Point lt = new Point(rectangle.Left, rectangle.Top);
            Point rt = new Point(rectangle.Left + rectangle.Width, rectangle.Top);
            Point lb = new Point(rectangle.Left, rectangle.Top + rectangle.Height);
            Point rb = new Point(rectangle.Left + rectangle.Width, rectangle.Top + rectangle.Height);


            drawingContext.DrawLine(Pen, lt, rt);
            drawingContext.DrawLine(Pen, lt, lb);
            drawingContext.DrawLine(Pen, lb, rb);
            drawingContext.DrawLine(Pen, rt, rb);


        }
    }
}
