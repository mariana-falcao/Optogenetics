using OpenCvSharp;
using System;
using System.IO.Ports;
using System.Linq;
using System.Reflection.PortableExecutable;
using VmbNET; // This is the only Vimba namespace you need

///////////////////////////// Simple Vimba X startup example to see if it works /////////////////////////////

//class Program
//{
//    static void Main()
//    {
//        try
//        {
//            // Start the system
//            using IVmbSystem vmb = IVmbSystem.Startup();
//            Console.WriteLine("Vimba X Startup Successful!");

//            // Get cameras
//            var cameras = vmb.GetCameras();
//            Console.WriteLine($"Found {cameras.Count} camera(s).");

//            if (cameras.Any())
//            {
//                var camera = cameras.First();
//                Console.WriteLine($"Using Camera: {camera.Id} ({camera.ModelName})");

//                // Success! Your hardware is talking to your code.
//            }
//            else
//            {
//                Console.WriteLine("No cameras found. Check your USB connection and Vimba Driver Installer.");
//            }
//        }
//        catch (Exception ex)
//        {
//            // VmbNETException is the base class for all VmbNET errors
//            if (ex is VmbNETException vmbEx)
//            {
//                Console.WriteLine($"Vimba Hardware Error: {vmbEx.Message}");
//            }
//            else
//            {
//                Console.WriteLine($"System Error: {ex.Message}");
//            }
//        }

//        Console.WriteLine("\nPress any key to exit...");
//        Console.ReadKey();
//    }
//}




class Program
{
    static VideoWriter _writer;
    static bool _isRecording = false;
    

static void Main()
    {
        // 1. SET YOUR FOLDER PATH HERE
        string folderPath = @"C:\Users\TeachingLab\Desktop\OptogeneticsOrgerLab\BehaviourData";
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fullPath = Path.Combine(folderPath, $"Trial_{timestamp}.avi"); 

        try
        {
            // Ensure directory exists
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            using IVmbSystem vmb = IVmbSystem.Startup();
            var cameras = vmb.GetCameras();
            if (cameras.Count == 0)
            {
                Console.WriteLine("No cams detected");
                return;
            }

            using var openCam = cameras[0].Open();

            // --- ROI CONFIGURATION ---
            // Set Offsets to 0 first to make room for changing Width/Height
            openCam.Features.OffsetX = 0;
            openCam.Features.OffsetY = 0;

            // Set the specific ROI dimensions from your image
            openCam.Features.Width = 320;
            openCam.Features.Height = 240;

            // Set the specific Offsets from your image
            openCam.Features.OffsetX = 160;
            openCam.Features.OffsetY = 120;

            // Configuration
            openCam.Features.PixelFormat = "Mono8";
            openCam.Features.ExposureTime = 1300.0;
            openCam.Features.Gain = 10.0;
            openCam.Features.TriggerSource = "Line2";
            openCam.Features.TriggerSelector = "FrameStart";
            openCam.Features.TriggerMode = "On";

            // Use (int)(long) to force the dynamic feature into a standard integer
            int width = (int)(long)openCam.Features.Width;
            int height = (int)(long)openCam.Features.Height;

            _writer = new VideoWriter(fullPath, FourCC.MJPG, 700, new Size(width, height), isColor: false);
            
            // Updated Event Handler
            int frameCount = 0;

            openCam.FrameReceived += (s, e) =>
            {
                using var frame = e.Frame;
                if (frame.BufferSize > 0)
                {
                    try
                    {
                        using Mat mat = Mat.FromPixelData(height, width, MatType.CV_8UC1, frame.Buffer);

                        if (_isRecording)
                        {
                            _writer.Write(mat);
                        }

                        // PREVIEW LOGIC
                        // To ensure we see SOMETHING, let's show every 10th frame
                        frameCount++;
                        if (frameCount % 10 == 0)
                        {
                            Cv2.ImShow("Mako Live Preview", mat);
                            // WaitKey is MANDATORY to see the window
                            Cv2.WaitKey(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If there is an error in processing, we want to know
                        Console.WriteLine("Preview Error: " + ex.Message);
                    }
                }
            };


            using var acquisition = openCam.StartFrameAcquisition();

            // Arduino Communication
            using var port = new SerialPort("COM7", 115200) { NewLine = "\n" };
            port.Open();
            System.Threading.Thread.Sleep(1000);
            port.DiscardInBuffer();

            Console.WriteLine("Ready. Sending START to Arduino...");
            _isRecording = true;
            port.WriteLine("START 700 1 0.1 0.2 1024 128");

            while (true)
            {
                string line = port.ReadLine().Trim();
                Console.WriteLine($"Arduino: {line}");
                if (line == "DONE") break;
            }

            _isRecording = false;
            Console.WriteLine("Stopping recording logic...");

            System.Threading.Thread.Sleep(200);

            if (_writer != null)
            { 
                _writer.Release();
                _writer.Dispose();
            }
            //openCam.Features.TriggerMode = "Off";
            acquisition.Dispose(); 
            Cv2.DestroyAllWindows();
            Console.WriteLine("Stopping Camera Stream...");
                
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        Console.WriteLine("Finished. Press any key to exit...");    
        Console.ReadKey();
    }
}

