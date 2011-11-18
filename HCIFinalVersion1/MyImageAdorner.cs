using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace HCIFinalVersion1
{
    public class MyImageAdorner : Adorner
    {
        Thumb rotateHandle;
        Thumb moveHandle;
        Thumb scaleHandle;

        Path outline;
        VisualCollection visualChildren;
        Point center;

        TranslateTransform translate;
        ScaleTransform scale;
        RotateTransform rotation;
        TransformGroup transformGroup;

        const int HANDLEMARGIN = 10;

        public MyImageAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            visualChildren = new VisualCollection(this);

            rotateHandle = new Thumb();
            rotateHandle.Cursor = Cursors.SizeWE;
            rotateHandle.Width = 20;
            rotateHandle.Height = 20;
            rotateHandle.Background = Brushes.Blue;

            rotateHandle.DragDelta += new DragDeltaEventHandler(rotateHandle_DragDelta);
            rotateHandle.DragCompleted += new DragCompletedEventHandler(rotateHandle_DragCompleted);

            moveHandle = new Thumb();
            moveHandle.Cursor = Cursors.SizeAll;
            moveHandle.Width = 20;
            moveHandle.Height = 20;
            moveHandle.Background = Brushes.Yellow;

            moveHandle.DragDelta += new DragDeltaEventHandler(moveHandle_DragDelta);
            moveHandle.DragCompleted += new DragCompletedEventHandler(moveHandle_DragCompleted);

            scaleHandle = new Thumb();
            scaleHandle.Cursor = Cursors.SizeNWSE;
            scaleHandle.Width = 20;
            scaleHandle.Height = 20;
            scaleHandle.Background = Brushes.Red;

            scaleHandle.DragDelta += new DragDeltaEventHandler(scaleHandle_DragDelta);
            scaleHandle.DragCompleted += new DragCompletedEventHandler(scaleHandle_DragCompleted);

            outline = new Path();
            outline.Stroke = Brushes.Gray;
            outline.StrokeThickness = 10;

            visualChildren.Add(outline);
            visualChildren.Add(rotateHandle);
            visualChildren.Add(moveHandle);
            visualChildren.Add(scaleHandle);

            rotation = new RotateTransform();
            translate = new TranslateTransform();
            scale = new ScaleTransform();
            transformGroup = new TransformGroup();
        }

        void scaleHandle_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            MoveNewTransformToAdornedElement(scale);
        }

        void scaleHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point pos = Mouse.GetPosition(this);

            // TODO: Add scale logic
            scale.ScaleX = 0.5;
            scale.ScaleY = 0.5;

            scale.CenterX = center.X;
            scale.CenterY = center.Y;

            outline.RenderTransform = scale;
        }

        void moveHandle_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            MoveNewTransformToAdornedElement(translate);
        }

        void moveHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point pos = Mouse.GetPosition(this);

            double deltaX = pos.X - center.X;
            double deltaY = pos.Y - center.Y;

            translate.X = deltaX;
            translate.Y = deltaY;

            outline.RenderTransform = translate;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {

            center = new Point(AdornedElement.RenderSize.Width / 2, AdornedElement.RenderSize.Height / 2);
            Point bottomRight = new Point(AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);

            Rect rotateRect = new Rect(0,
                                       0 - (AdornedElement.RenderSize.Height / 2 + HANDLEMARGIN),
                                       AdornedElement.RenderSize.Width,
                                       AdornedElement.RenderSize.Height);
            Rect scaleRectUL = new Rect(0, 0, 10, 10);
            Rect scaleRectBR = new Rect(bottomRight.X, bottomRight.Y, 10, 10);

            Rect finalRect = new Rect(finalSize);


            rotateHandle.Arrange(rotateRect);
            moveHandle.Arrange(finalRect);
            scaleHandle.Arrange(scaleRectUL);
            scaleHandle.Arrange(scaleRectBR);

            outline.Data = new RectangleGeometry(finalRect);
            outline.Arrange(finalRect);

            return finalSize;
        }

        void rotateHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point pos = Mouse.GetPosition(this);

            double deltaX = pos.X - center.X;
            double deltaY = pos.Y - center.Y;

            double angle;

            if (deltaY.Equals(0))
            {
                if (!deltaX.Equals(0))
                {
                    angle = 90;
                }
                else
                {
                    return;
                }
            }
            else
            {
                double tan = deltaX / deltaY;
                angle = Math.Atan(tan);

                angle = angle * 180 / Math.PI;
            }

            // If the mouse crosses the vertical center, 
            // find the complementary angle.
            if (deltaY > 0)
            {
                angle = 180 - Math.Abs(angle);
            }

            // Rotate left if the mouse moves left and right
            // if the mouse moves right.
            if (deltaX < 0)
            {
                angle = -Math.Abs(angle);
            }
            else
            {
                angle = Math.Abs(angle);
            }

            if (Double.IsNaN(angle))
            {
                return;
            }

            // Apply the rotation to the outline.
            rotation.Angle = angle;
            rotation.CenterX = center.X;
            rotation.CenterY = center.Y;

            outline.RenderTransform = rotation;
        }

        /// <summary>
        /// Rotates to the same angle as outline.
        /// </summary>
        void rotateHandle_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            MoveNewTransformToAdornedElement(rotation);
        }

        protected override int VisualChildrenCount
        {
            get { return visualChildren.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visualChildren[index];
        }

        private void MoveNewTransformToAdornedElement(Transform transform)
        {
            if (transform == null)
            {
                return;
            }
            var newTransform = transform.Clone();
            newTransform.Freeze();

            transformGroup.Children.Insert(0, newTransform);

            AdornedElement.RenderTransform = transformGroup;

            outline.RenderTransform = Transform.Identity;
            this.InvalidateArrange();
        }

    }
}
