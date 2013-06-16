using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using Aldebaran.Proxies;

namespace PointCloudWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //test git push
        GeometryModel3D[] points = new GeometryModel3D[640 * 480];
        int pointDen = 10; // point density, counter intuitive, increasing the number increases the number of pixels skipped, and speeds up the program.
        int frameCount = 0; // counts the itteration of the frame
        int av = 0, count = 0; // av is the average value of danger pixels. It adds the location, then devides by the count ( the number of danger pixels). this gives a center of gravity for the object to avoid.
        float rotate = 0, speed = 0; // sets the rotation and speed of the robot.
        bool objectDetected = false, REDALERT = false; // objectDetect tells it to rotate according to yellow danger values. REDALERT warns that something is dangerously close, and prevents forward motion.
        KinectSensor sensor; // intializes the kinect sensor.
        MotionProxy mp = null; // intializes the motion proxy
        TextToSpeechProxy ttsp = null; // intializes the speech proxy
        RobotPostureProxy rpp = null;
        bool pathChecked = false, restChecked = false;

        public MainWindow()
        {
            InitializeComponent();
            
        }
        void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (frameCount % 3 == 0) // increase the number after the mod to increase speed. in its current setup it skips 2 out of 3 frames and only processes the 3rd.
            {
                DepthImageFrame imageFrame = e.OpenDepthImageFrame();
                if (imageFrame != null) // makes sure a frame exists.
                {
                    short[] pixelData = new short[imageFrame.PixelDataLength]; // Creates an array the size of the image frame pixel length
                    imageFrame.CopyPixelDataTo(pixelData); // copies pixel data to the pixel data array.
                    int temp = 0; // initalizes depth.
                    int i = 0; // intializes the count.
                    for (int y = 0; y < 480; y += pointDen)// iterates through each row.
                    {
                        for (int x = 0; x < 640; x += pointDen) //iterates through each column in the row.
                        {
                            temp = ((ushort)pixelData[x + y * 640])>> 4;
                            ((TranslateTransform3D)points[i].Transform).OffsetZ = temp; // sets the triangles z offset.
                            if (temp<400 && x >= 200 && x <= 400)
                            {
                                points[i].Material = new DiffuseMaterial(new SolidColorBrush(Colors.Red)); // throws a read warning.
                                rotate = -.2f;
                                speed = .0f;
                                REDALERT = true;
                            }
                            else if (temp <500 && x >= 200 && x<= 400 && !REDALERT)
                            {
                                points[i].Material = new DiffuseMaterial(new SolidColorBrush(Colors.Yellow)); // sets it to be yellow when it is in a cetain range.
                                objectDetected = true;
                                    av += x;
                                    count++;
                                // 3 triagles that are yellow, what x value? x  value is on left side, rotate right. 
                            }
                            else 
                            {
                                points[i].Material = new DiffuseMaterial(new SolidColorBrush(Colors.Blue)); // paints everyhting else blue.
                            }
                            i++;
                        }
                    }
                    if (objectDetected)
                    {
                        av = av / count;
                        av = av - 320;
                        if (av >= 0 && count > 5)
                        {
                           rotate = -.2f;
                           speed = .5f;
                        }
                        else if ( count > 5)
                        {
                            rotate = .2f;
                            speed = .5f;
                        }
                       
                    }
                    if(pathChecked == true)
                    {
                        if (mp == null)
                        {
                            mp = new MotionProxy(ipBox.Text, 9559);
                        }
                        if (mp != null)
                        {
                            mp.setWalkTargetVelocity(speed, 0, rotate, .5f);
                        }
                    }
                    else if (pathChecked == false)
                    {
                        if (mp == null)
                        {
                            mp = new MotionProxy(ipBox.Text, 9559);
                        }
                        if (mp != null)
                        {
                            mp.stopMove();
                        }
                    }
                
                }
            }
            speed = .6f;
            rotate = 0;
            av = 0;
            count = 0;
            objectDetected = false;
            REDALERT = false;
            pathChecked = false;
            frameCount++;
        }

        private GeometryModel3D Triangle(double x , double y, double s)
        {
            //define the geometry as a set of points and order the points are connected up
            Point3DCollection corners = new Point3DCollection();
            corners.Add(new Point3D(x, y, 0));
            corners.Add(new Point3D(x, y + s, 0));
            corners.Add(new Point3D(x + s, y + s, 0));
            Int32Collection Triangles = new Int32Collection();
            Triangles.Add(0);
            Triangles.Add(1);
            Triangles.Add(2);

            //After the geometry is defined
            MeshGeometry3D tmesh = new MeshGeometry3D();
            tmesh.Positions = corners;
            tmesh.TriangleIndices = Triangles;

            //Defines the normal vector
            tmesh.Normals.Add(new Vector3D(0, 0, -1));

            //Puts the geometry together with some material properties
            GeometryModel3D msheet = new GeometryModel3D();
            msheet.Geometry = tmesh;
            return msheet;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Initialize the motion proxy using the text in the ipBox
            MotionProxy mp = new MotionProxy(ipBox.Text, 9559);

            //Initialize the text to speech proxy using the text in the ipBox
            ttsp = new TextToSpeechProxy(ipBox.Text, 9559);

            //Initialize the robots posture proxy using the text in the ipBox
            RobotPostureProxy rpp = new RobotPostureProxy(ipBox.Text, 9559);

           //Wakes the robot up, tells the robot to stand and say hello
                mp.wakeUp();
                rpp.goToPosture("StandInit", .5f);
            ttsp.say("Hola");

            //Sets up lighting of the camera with a white color and directs it in the appropriate direction
            DirectionalLight DirLight1 = new DirectionalLight();
            DirLight1.Color = Colors.White;
            DirLight1.Direction = new Vector3D(1, 1, 1);

            //Setting up the camera so that the picture we see is in a good position
            PerspectiveCamera Camera1 = new PerspectiveCamera();
            Camera1.FarPlaneDistance = 16000;
            Camera1.NearPlaneDistance = 10;

            //Zoomed in with a 45 degree field of view
            Camera1.FieldOfView = 45;

            //50 mm away from the point cloud and centered in the middle
            Camera1.Position = new Point3D(320, 240, -50);

            //Flips the camera so that we see everything right side up
            Camera1.LookDirection = new Vector3D(0, 0, 1);
            Camera1.UpDirection = new Vector3D(0, -1, 0);

            //Creates a new model 3D group to hold the sets of 3D models
            Model3DGroup modelGroup = new Model3DGroup();

            //builds the 3D model
            int i = 0;
            for (int y = 0; y < 480; y += pointDen)
            {
                for (int x = 0; x < 640; x += pointDen)
                {
                    points[i] = Triangle(x, y, pointDen);
                    points[i].Transform = new TranslateTransform3D(0, 0, 0);
                    modelGroup.Children.Add(points[i]);
                    i++;
                }
            }

            //Adds the directional light to the model group
            modelGroup.Children.Add(DirLight1);

            ModelVisual3D modelsVisual = new ModelVisual3D();
            modelsVisual.Content = modelGroup;
            Viewport3D myViewport = new Viewport3D();
            myViewport.IsHitTestVisible = false;
            myViewport.Camera = Camera1;
            myViewport.Children.Add(modelsVisual);
            canvas1.Children.Add(myViewport);
            myViewport.Height = canvas1.Height;
            myViewport.Width = canvas1.Width;
            Canvas.SetTop(myViewport, 0);
            Canvas.SetLeft(myViewport, 0);

            sensor = KinectSensor.KinectSensors[0];
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.DepthFrameReady += DepthFrameReady;
            sensor.Start();
        }

        private void rest_button_Click(object sender, RoutedEventArgs e)
        {
            rpp = new RobotPostureProxy(ipBox.Text, 9559);
            mp.stopMove();
            rpp.goToPosture("SitRelax", 1.0f);
            mp.rest();
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            pathChecked = true;
        }

        private void restBox_Checked(object sender, RoutedEventArgs e)
        {
            restChecked = true;
        }
    }
}
