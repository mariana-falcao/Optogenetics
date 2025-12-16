using System;
using System.IO.Ports;

class Program
{
    static void Main()
    {
        //Console.Write("Enter fps: ");
        //int fps = int.Parse(Console.ReadLine() ?? "30");
        //Console.Write("Trial duration: ");
        //int duration = int.Parse(Console.ReadLine() ?? "10");

        // Trial settings 
        int fps = 700;
        int duration = 1;
        //DutyCycle
        double dutyCycleT1 = 0.1;
        double dutyCycleT2 = 0.2;
        // Arduino settings given the frequency
        int prescalerT1 = 1024;
        int prescalerT2 = 128;


        string portName = "COM7";
        using var port = new SerialPort(portName, 115200);  // Clear boot noise: At 115200 baud, unlike the 9600, the PC starts reading immediately and the PC misses those very first characters, resulting in garbage like ????, so we need to tell the Pc to discard the first bytes 
        port.NewLine = "\n";
        port.Open();
        System.Threading.Thread.Sleep(1000);  // this line is absolutely ncessary. wait 1s to let arduino stabilize
        port.DiscardInBuffer();

        string command = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "START {0} {1} {2} {3} {4} {5}",
                     fps, duration, dutyCycleT1, dutyCycleT2, prescalerT1, prescalerT2);
        //string command = $"START fps: {fps} duration: {duration} dutyCycleT1: {dutyCycleT1} dutyCycleT2: {dutyCycleT2} prescalerT1: {prescalerT1} prescalerT2: {prescalerT2}";
        Console.WriteLine($"Sending command: {command}");
        port.WriteLine(command);

        // wait for arduino's response 
        while (true)
        {
            string line = port.ReadLine().Trim();
            Console.WriteLine($"Arduino: {line}");

            if (line == "DONE")
            {
                break;
            }
        }
        port.Close();
    }
}