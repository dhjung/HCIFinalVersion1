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
using System.Collections;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using Coding4Fun.Kinect.Wpf.Controls;
using System.Diagnostics;
using System.Windows.Media.Animation;

namespace HCIFinalVersion1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private double WaitTime = 1500;
        private static Stopwatch hoverWatch;
        private static bool hoverTrigger = false;
        private static Button currentButton = new Button();
        private static bool popupWindow = false;

        private const float ClickThreshold = 0.33f;
        private const float SkeletonMaxX = 0.50f;
        private const float SkeletonMaxY = 0.50f;
            
        //Declare the Kinect NUI Runtime instance
        Microsoft.Research.Kinect.Nui.Runtime nui;
        private static double _topBoundary;
        private static double _bottomBoundary;
        private static double _leftBoundary;
        private static double _rightBoundary;
        private static double _itemLeft;
        private static double _itemTop;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            hoverWatch = new Stopwatch();

            if (Runtime.Kinects.Count == 0)
            {
                Console.Write("missing kinect");
            }
            else
            {
                nui = Runtime.Kinects[0];
                nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseDepth | RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);

                nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
                nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);

                nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color); //PoolSize = 2 buffers.  One for queuing and one for displaying
                nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.Depth);

                #region TransformSmooth
                //Must set to true and set after call to Initialize
                nui.SkeletonEngine.TransformSmooth = true;

                //Use to transform and reduce jitter
                var parameters = new TransformSmoothParameters
                {
                    Smoothing = 0.75f,
                    Correction = 0.0f,
                    Prediction = 0.0f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                };

                nui.SkeletonEngine.SmoothParameters = parameters;
                #endregion

                //add event to receive skeleton data
                nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            }

        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {

            //PlanarImage imageData = e.ImageFrame.Image;
            //image1.Source = BitmapSource.Create(imageData.Width, imageData.Height, 96, 96,
            //                            PixelFormats.Bgr32, null, imageData.Bits, imageData.Width * imageData.BytesPerPixel);
        }

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            //image2.Source = e.ImageFrame.ToBitmapSource();
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame allSkeletons = e.SkeletonFrame;

            if (allSkeletons != null) 
            {
                //get the first tracked skeleton
                SkeletonData skeleton = (from s in allSkeletons.Skeletons
                                         where s.TrackingState == SkeletonTrackingState.Tracked
                                         select s).FirstOrDefault();
                if (skeleton != null)
                {
                    //Control the Mouse coordinates
                    Joint jointRight = skeleton.Joints[JointID.HandRight];
                    Joint scaledRight = jointRight.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
                    int cursorX, cursorY;
                    cursorX = (int)scaledRight.Position.X;
                    cursorY = (int)scaledRight.Position.Y;

                    Joint jointLeft = skeleton.Joints[JointID.HandLeft];
                    Joint scaledLeft = jointRight.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
                    int leftCursorX, leftCursorY;
                    leftCursorX = (int)scaledRight.Position.X;
                    leftCursorY = (int)scaledRight.Position.Y;

                    //set positions
                    SetEllipsePosition(headEllipse, skeleton.Joints[JointID.Head]);
                    //SetEllipsePosition(handEllipse, skeleton.Joints[JointID.HandRight]);

                    SetEllipsePosition(rightHandEllipse, skeleton.Joints[JointID.HandRight]);

                    SetEllipsePosition(leftHandEllipse, skeleton.Joints[JointID.HandLeft]);

                    //Check if the right hand is hovering over the following buttons
                    CheckButton(One, rightHandEllipse, leftHandEllipse);
                    CheckButton(Two, rightHandEllipse, leftHandEllipse);
                    CheckButton(Three, rightHandEllipse, leftHandEllipse);
                    CheckButton(Four, rightHandEllipse, leftHandEllipse);
                    CheckButton(LoadImage, rightHandEllipse, leftHandEllipse);
                    CheckButton(Six, rightHandEllipse, leftHandEllipse);
                    CheckButton(Seven, rightHandEllipse, leftHandEllipse);
                    CheckButton(Eight, rightHandEllipse, leftHandEllipse);

                    //bool leftClick = false;
                    //NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, leftClick);
                    
                }
            }
        }

        private void SetEllipsePosition(FrameworkElement ellipse, Joint joint)
        {
            var scaledJoint = joint.ScaleTo(1230, 750, .5f, .5f);

            if (joint.ID == JointID.HandRight)
            {
                Canvas.SetLeft(ellipse, scaledJoint.Position.X);
                Canvas.SetTop(ellipse, scaledJoint.Position.Y);

                if (currentButton.Name == "LoadImage" && hoverTrigger == true)
                {
                    if (hoverWatch.ElapsedMilliseconds > WaitTime)
                    {
                        OpenFileDialog();
                        hoverTrigger = false;
                        popupWindow = true;
                    }
                    //Console.WriteLine(hoverWatch.ElapsedMilliseconds);
                }
                //Console.WriteLine("right hand");
            }
            else if (joint.ID == JointID.HandLeft)
            {
                Canvas.SetLeft(ellipse, scaledJoint.Position.X);
                Canvas.SetTop(ellipse, scaledJoint.Position.Y);
            }
            else if (joint.ID == JointID.Head)
            {
                Canvas.SetLeft(ellipse, scaledJoint.Position.X);
                Canvas.SetTop(ellipse, scaledJoint.Position.Y);
            }
        }

        /*
        private void SetButtonPosition(FrameworkElement button, Joint joint)
        {
            var scaledJoint = joint.ScaleTo(1230, 750, .5f, .5f);
            if (joint.ID == JointID.HandRight)
            {
                Canvas.SetLeft(button, scaledJoint.Position.X);
                Canvas.SetTop(button, scaledJoint.Position.Y);
            }
        }
        
        void kinectButton_Clicked(object sender, RoutedEventArgs e)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = "Triggered";
            textBlock.FontSize = 42;
            grid1.Children.Add(textBlock);
        }
         */

        private static void CheckButton(Button button, Ellipse left_thumbStick, Ellipse right_thumbStick)
        {
            if ( (IsItemMidpointInContainer(button, left_thumbStick) || IsItemMidpointInContainer(button, right_thumbStick)) && button.Visibility == Visibility.Visible)
            {
                button.Background = Brushes.Green;
                if (button.Name == "LoadImage")
                {
                    if (button == currentButton)
                    {
                        if (hoverTrigger == false)
                        {
                            hoverWatch.Reset();
                            hoverWatch.Start();
                        } //else continue counting time
                    }
                    else
                    {
                        currentButton = button;
                        //cursor has moved from one button to another.  Reset stop watch
                        hoverWatch.Reset();
                        hoverWatch.Start();
                    }
                    hoverTrigger = true;
                }
            }
            else
            {
                button.Background = Brushes.White;
                if (button.Name == "LoadImage")
                {
                    hoverTrigger = false;
                    hoverWatch.Reset();
                }
            }
        }

        public static bool IsItemMidpointInContainer(FrameworkElement container, FrameworkElement target)
        {
            FindValues(container, target);

            if (_itemTop < _topBoundary || _bottomBoundary < _itemTop)
            {
                //Midpoint of target is outside of top or bottom
                return false;
            }

            if (_itemLeft < _leftBoundary || _rightBoundary < _itemLeft)
            {
                //Midpoint of target is outside of left or right
                return false;
            }

            return true;
        }

        private static void FindValues(FrameworkElement container, FrameworkElement target)
        {
            var containerTopLeft = container.PointToScreen(new System.Windows.Point());
            var itemTopLeft = target.PointToScreen(new System.Windows.Point());

            _topBoundary = containerTopLeft.Y;
            _bottomBoundary = _topBoundary + container.ActualHeight;
            _leftBoundary = containerTopLeft.X;
            _rightBoundary = _leftBoundary + container.ActualWidth;

            //use midpoint of item (width or height divided by 2)
            _itemLeft = itemTopLeft.X + (target.ActualWidth / 2);
            _itemTop = itemTopLeft.Y + (target.ActualHeight / 2);
        }

        #region Open File Dialog and Add Image on Canvas

        private void OpenFileDialog()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.InitialDirectory = @"C:\Users\Public\Pictures\Sample Pictures\";

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;

                // Add Image on Canvas
                AddImageOnCanvas(filename, new Point(0, 0));
            }
        }

        private void AddImageOnCanvas(String path, Point pt)
        {
            //Create an new image
            BitmapImage bitmap = new BitmapImage(new Uri(path));
            Image image = new Image { Source = bitmap };

            image.Height = bitmap.PixelHeight;
            image.Width = bitmap.PixelWidth;

            InkCanvas.SetLeft(image, pt.X);
            InkCanvas.SetTop(image, pt.Y);
            inkCanvas.Children.Add(image);

            AdornerLayer adornerlayer = AdornerLayer.GetAdornerLayer(image);
            MyImageAdorner adorner = new MyImageAdorner(image);
            adornerlayer.Add(adorner);

            inkCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        #endregion
    }
}
