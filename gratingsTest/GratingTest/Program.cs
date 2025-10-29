using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

class Program
{
    // ===================== CONFIGURABLE PARAMETERS =====================
    private static int numStripes = 8;        // Number of black/white stripes
    private static float driftSpeed = 0.25f;   // cycles per second
    private static float orientationDeg = 0f; // rotation angle in degrees
    private static float contrast = 1.0f;     // 0 = gray, 1 = full black/white
    private static float durationSec = 2f;   // total duration of grating display
    // ===================================================================

    static void Main()
    {
        var nativeSettings = new NativeWindowSettings()
        {
            Size = new Vector2i(800, 600),
            Title = "Configurable Drifting Grating"
        };

        using var window = new ConfigurableGratingWindow(
            GameWindowSettings.Default,
            nativeSettings,
            numStripes,
            driftSpeed,
            orientationDeg,
            contrast,
            durationSec
        );

        window.Run();
    }
}

class ConfigurableGratingWindow : GameWindow
{
    private int shaderProgram;
    private int vao;

    private int numStripes;
    private float driftSpeed;
    private float phase = 0f;
    private float orientationRad;
    private float contrast;
    private float durationSec;
    private double startTime;

    public ConfigurableGratingWindow(GameWindowSettings gws, NativeWindowSettings nws,
                                     int numStripes, float driftSpeed, float orientationDeg,
                                     float contrast, float durationSec)
        : base(gws, nws)
    {
        this.numStripes = numStripes;
        this.driftSpeed = driftSpeed;
        this.orientationRad = MathHelper.DegreesToRadians(orientationDeg);
        this.contrast = MathHelper.Clamp(contrast, 0f, 1f);
        this.durationSec = durationSec;
    }

    protected override void OnLoad()
    {
        GL.ClearColor(0.5f, 0.5f, 0.5f, 1f); // gray background for zero contrast

        // fullscreen quad
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

        // shaders
        string vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec2 aPosition;
            out vec2 vPos;
            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
                vPos = aPosition;
            }
        ";

        string fragmentShaderSource = @"
            #version 330 core
            in vec2 vPos;
            out vec4 FragColor;
            uniform int numStripes;
            uniform float phase;
            uniform float orientation;
            uniform float contrast;

            void main()
            {
                // rotate coordinates
                float x = vPos.x * cos(orientation) + vPos.y * sin(orientation);

                float normalizedX = (x + 1.0) / 2.0;
                float stripe = floor(fract(normalizedX + phase) * numStripes);
                float lum = mod(stripe, 2.0) < 1.0 ? 0.5f + contrast/2.0f : 0.5f - contrast/2.0f;
                FragColor = vec4(lum, lum, lum, 1.0);
            }
        ";

        shaderProgram = CompileShaders(vertexShaderSource, fragmentShaderSource);

        startTime = DateTime.Now.TimeOfDay.TotalSeconds;

        base.OnLoad();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        double elapsed = DateTime.Now.TimeOfDay.TotalSeconds - startTime;
        if (elapsed >= durationSec)
        {
            Close();
            return;
        }

        phase += driftSpeed * (float)args.Time;

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(shaderProgram);

        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "numStripes"), numStripes);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "phase"), phase);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "orientation"), orientationRad);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "contrast"), contrast);

        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        SwapBuffers();
        base.OnRenderFrame(args);
    }

    protected override void OnUnload()
    {
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
