using System;
using System.IO.Ports;
using System.Threading;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

// ===================== CONFIGURABLE PARAMETERS =====================
string serialPortName = "COM7";  // ⚠️ CHANGE THIS to match your Arduino port
int baudRate = 115200;

// Grating parameters
int numStripes = 8;        // Number of black/white stripes
float driftSpeed = 0.25f;  // cycles per second
float orientationDeg = 0f; // rotation angle in degrees
float contrast = 1.0f;     // 0 = gray, 1 = full black/white

// Arduino command parameters
string startCommand = "START 30 7 0.1 0.2 1024 128";
// ===================================================================

// ----- Detect monitors -----
var monitors = Monitors.GetMonitors();
Console.WriteLine("Detected monitors:");
for (int i = 0; i < monitors.Count; i++)
{
    var m = monitors[i];
    Console.WriteLine($"[{i}] {m.Name}  {m.ClientArea.Size.X}x{m.ClientArea.Size.Y}");
}

var projectorMonitor = monitors.Count > 1 ? monitors[1] : monitors[0];

// ----- Setup serial port -----
SerialPort serialPort = null;
try
{
    serialPort = new SerialPort(serialPortName, baudRate);
    serialPort.NewLine = "\n";
    serialPort.Open();
    Console.WriteLine($"✓ Connected to Arduino on {serialPortName}");
    Console.WriteLine($"✓ Press SPACE to start trial");
    Console.WriteLine($"✓ Command: {startCommand}");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Failed to open serial port: {ex.Message}");
    Console.WriteLine("Exiting...");
    return;
}

// ----- Configure window -----
var nativeSettings = new NativeWindowSettings()
{
    Title = "Arduino-Controlled Grating [SPACE = Start Trial]",
    WindowState = WindowState.Fullscreen,
    CurrentMonitor = projectorMonitor.Handle,
};

// ----- Create and run -----
using var window = new SerialControlledGratingWindow(
    GameWindowSettings.Default,
    nativeSettings,
    serialPort,
    startCommand,
    numStripes,
    driftSpeed,
    orientationDeg,
    contrast
);

window.Run();

// ----- Cleanup -----
serialPort?.Close();

enum ScreenState
{
    Gray,        // Initial state (waiting)
    Grating,     // Showing drifting grating
    Black        // Black screen
}

class SerialControlledGratingWindow : GameWindow
{
    private SerialPort serialPort;
    private Thread serialThread;
    private string startCommand;

    private int shaderProgram;
    private int vao;

    private int numStripes;
    private float driftSpeed;
    private float phase = 0f;
    private float orientationRad;
    private float contrast;

    private ScreenState currentState = ScreenState.Gray;
    private bool shouldExit = false;
    private bool trialRunning = false;

    public SerialControlledGratingWindow(GameWindowSettings gws, NativeWindowSettings nws,
                                         SerialPort serialPort,
                                         string startCommand,
                                         int numStripes, float driftSpeed, float orientationDeg,
                                         float contrast)
        : base(gws, nws)
    {
        this.serialPort = serialPort;
        this.startCommand = startCommand;
        this.numStripes = numStripes;
        this.driftSpeed = driftSpeed;
        this.orientationRad = MathHelper.DegreesToRadians(orientationDeg);
        this.contrast = MathHelper.Clamp(contrast, 0f, 1f);
    }

    protected override void OnLoad()
    {
        GL.ClearColor(0.5f, 0.5f, 0.5f, 1f); // Gray background

        float[] vertices = {
            -1f, -1f,
             1f, -1f,
             1f,  1f,
            -1f, -1f,
             1f,  1f,
            -1f,  1f
        };

        vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        string vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec2 aPosition;
            out vec2 vPos;
            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
                vPos = aPosition;
            }";

        string fragmentShaderSource = @"
            #version 330 core
            in vec2 vPos;
            out vec4 FragColor;
            uniform int numStripes;
            uniform float phase;
            uniform float orientation;
            uniform float contrast;
            uniform int screenState; // 0=gray, 1=grating, 2=black

            void main()
            {
                if (screenState == 0) {
                    // Gray screen
                    FragColor = vec4(0.5, 0.5, 0.5, 1.0);
                    return;
                }
                
                if (screenState == 2) {
                    // Black screen
                    FragColor = vec4(0.0, 0.0, 0.0, 1.0);
                    return;
                }
                
                // Grating (screenState == 1)
                float x = vPos.x * cos(orientation) + vPos.y * sin(orientation);
                float normalizedX = (x + 1.0) / 2.0;
                float stripe = floor(fract(normalizedX + phase) * numStripes);
                float lum = mod(stripe, 2.0) < 1.0 ? 0.5 + contrast/2.0 : 0.5 - contrast/2.0;
                FragColor = vec4(lum, lum, lum, 1.0);
            }";

        shaderProgram = CompileShaders(vertexShaderSource, fragmentShaderSource);

        // Start serial listener thread
        if (serialPort != null && serialPort.IsOpen)
        {
            serialThread = new Thread(SerialListener);
            serialThread.IsBackground = true;
            serialThread.Start();
        }

        base.OnLoad();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        if (e.Key == Keys.Space && !trialRunning)
        {
            StartTrial();
        }
        else if (e.Key == Keys.Escape)
        {
            shouldExit = true;
        }

        base.OnKeyDown(e);
    }

    private void StartTrial()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine(startCommand);
                Console.WriteLine($"→ Sent to Arduino: {startCommand}");
                trialRunning = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to send command: {ex.Message}");
            }
        }
    }

    private void SerialListener()
    {
        while (!shouldExit && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string line = serialPort.ReadLine().Trim();
                Console.WriteLine($"Arduino: {line}");

                if (line == "GRATING_ON")
                {
                    currentState = ScreenState.Grating;
                    phase = 0f; // Reset phase
                    Console.WriteLine("→ Grating ON");
                }
                else if (line == "GRATING_OFF")
                {
                    Console.WriteLine("→ Grating OFF");
                }
                else if (line == "BLACK_SCREEN")
                {
                    currentState = ScreenState.Black;
                    Console.WriteLine("→ Black Screen");
                }
                else if (line == "TRIAL_END")
                {
                    currentState = ScreenState.Gray;
                    trialRunning = false;
                    Console.WriteLine("→ Trial Complete - Press SPACE for next trial");
                }
                else if (line == "EXIT")
                {
                    shouldExit = true;
                    Console.WriteLine("→ Exit command received");
                }
            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial error: {ex.Message}");
                break;
            }
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        if (shouldExit)
        {
            Close();
            return;
        }

        // Update phase only when showing grating
        if (currentState == ScreenState.Grating)
        {
            phase += driftSpeed * (float)args.Time;
        }

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(shaderProgram);

        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "numStripes"), numStripes);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "phase"), phase);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "orientation"), orientationRad);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "contrast"), contrast);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "screenState"), (int)currentState);

        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        SwapBuffers();
        base.OnRenderFrame(args);
    }

    protected override void OnUnload()
    {
        shouldExit = true;
        serialThread?.Join(1000);
        GL.DeleteProgram(shaderProgram);
        GL.DeleteVertexArray(vao);
        base.OnUnload();
    }

    private int CompileShaders(string vertexSource, string fragmentSource)
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
            Console.WriteLine(GL.GetShaderInfoLog(vertexShader));

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
        if (success == 0)
            Console.WriteLine(GL.GetShaderInfoLog(fragmentShader));

        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out success);
        if (success == 0)
            Console.WriteLine(GL.GetProgramInfoLog(program));

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }
}