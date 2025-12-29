using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;

using FlyCapture2Managed;
using FlyCapture2Managed.Gui;

namespace MyCustomCameraOverlay // Make sure this matches your project name
{
    public partial class Form1 : Form
    {
        private CameraControlDialog m_camCtlDlg;
        private ManagedCameraBase m_camera = null;
        private ManagedImage m_rawImage;
        private ManagedImage m_processedImage;
        private bool m_grabImages;
        private AutoResetEvent m_grabThreadExited;
        private BackgroundWorker m_grabThread;

        // Overlay Variables
        private bool m_showOverlay = false;
        private Bitmap m_overlayBitmap = null;

        public Form1()
        {
            InitializeComponent();

            m_rawImage = new ManagedImage();
            m_processedImage = new ManagedImage();
            m_camCtlDlg = new CameraControlDialog();
            m_grabThreadExited = new AutoResetEvent(false);

            // Absolute path as requested
            try
            {
                string imagePath = @"C:\Users\TeachingLab\Desktop\OptogeneticsOrgerLab\PointGrey\reference.jpg";
                if (File.Exists(imagePath))
                {
                    m_overlayBitmap = new Bitmap(imagePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load overlay image: " + ex.Message);
            }
        }

        private void btnToggleOverlay_Click(object sender, EventArgs e)
        {
            m_showOverlay = !m_showOverlay;
            btnToggleOverlay.Text = m_showOverlay ? "Hide Overlay" : "Show Overlay";
        }

        private void UpdateUI(object sender, ProgressChangedEventArgs e)
        {
            UpdateStatusBar();

            Bitmap frame = m_processedImage.bitmap;

            if (m_showOverlay && frame != null && m_overlayBitmap != null)
            {
                using (Graphics g = Graphics.FromImage(frame))
                {
                    ColorMatrix matrix = new ColorMatrix();
                    matrix.Matrix33 = 0.5f; // 50% transparency

                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        g.DrawImage(
                            m_overlayBitmap,
                            new Rectangle(0, 0, frame.Width, frame.Height),
                            0, 0, m_overlayBitmap.Width, m_overlayBitmap.Height,
                            GraphicsUnit.Pixel,
                            attributes);
                    }
                }
            }

            pictureBox1.Image = frame;
            pictureBox1.Invalidate();
        }

        private void UpdateStatusBar()
        {
            toolStripStatusLabelImageSize.Text = string.Format("Image size: {0} x {1}", m_rawImage.cols, m_rawImage.rows);
            try
            {
                toolStripStatusLabelFrameRate.Text = string.Format("Frame rate: {0:F2}Hz", m_camera.GetProperty(PropertyType.FrameRate).absValue);
            }
            catch
            {
                toolStripStatusLabelFrameRate.Text = "Frame rate: 0.00Hz";
            }

            statusStrip1.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                ManagedBusManager busMgr = new ManagedBusManager();
                uint numCameras = busMgr.GetNumOfCameras();

                if (numCameras == 0)
                {
                    MessageBox.Show("No cameras detected.");
                    Close();
                    return;
                }

                ManagedPGRGuid guidToUse = busMgr.GetCameraFromIndex(0);
                InterfaceType ifType = busMgr.GetInterfaceTypeFromGuid(guidToUse);

                if (ifType == InterfaceType.GigE)
                    m_camera = new ManagedGigECamera();
                else
                    m_camera = new ManagedCamera();

                m_camera.Connect(guidToUse);

                m_camCtlDlg.Connect(m_camera); 

                m_camera.StartCapture();
                m_grabImages = true;
                StartGrabLoop();
            }
            catch (FC2Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
                Close();
            }
        }

        

        private void StartGrabLoop()
        {
            m_grabThread = new BackgroundWorker();
            m_grabThread.ProgressChanged += UpdateUI;
            m_grabThread.DoWork += GrabLoop;
            m_grabThread.WorkerReportsProgress = true;
            m_grabThread.RunWorkerAsync();
        }

        private void GrabLoop(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            while (m_grabImages)
            {
                try { m_camera.RetrieveBuffer(m_rawImage); }
                catch { continue; }

                lock (this)
                {
                    // Fixed the ambiguity by specifying FlyCapture2Managed
                    m_rawImage.Convert(FlyCapture2Managed.PixelFormat.PixelFormatBgr, m_processedImage);
                }
                worker.ReportProgress(0);
            }
            m_grabThreadExited.Set();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_grabImages = false;

            // Safely hide and disconnect the dialog
            if (m_camCtlDlg != null)
            {
                m_camCtlDlg.Hide();
                m_camCtlDlg.Disconnect();
            }

            if (m_camera != null && m_camera.IsConnected())
            {
                m_camera.StopCapture();
                m_camera.Disconnect();
            }
        }

        private void btnCameraSettings_Click(object sender, EventArgs e)
        {
            // Check if the camera is connected before trying to show the window
            if (m_camera != null && m_camera.IsConnected())
            {
                if (m_camCtlDlg.IsVisible())
                {
                    m_camCtlDlg.Hide();
                }
                else
                {
                    m_camCtlDlg.Show();
                }
            }
            else
            {
                MessageBox.Show("Please connect a camera first.");
            }
        }
    }
}