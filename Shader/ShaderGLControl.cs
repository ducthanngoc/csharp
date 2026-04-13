using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using StbImageSharp;
using System.Diagnostics;
using System.Text.RegularExpressions;
public enum IChannelType
{
    BufferA,
    BufferB,
    BufferC,
    BufferD,
    Sampler2D,
    SamplerCube
}
public class ShaderGLControl : GLControl
{
    private int program;
    private int input;
    private Stopwatch timer = new Stopwatch();
    private int frame = 0;
    private int iTimeLoc;
    private int iResolutionLoc;
    private int iMouseLoc;
    private int[] channelTextures = new int[4];
    private TextureTarget[] channelTargets = new TextureTarget[4];
    private ShaderBuffer[] buffers = new ShaderBuffer[4];
    private IChannelType[] iChannelType;

    private string[] iChannelPath;

    private string Code;

    private Vector2 mousePos;
    private Vector2 mouseDownPos;
    private bool mousePressed = false;

    public ShaderGLControl(IChannelType[] iChannelType) : base(new GLControlSettings()
    {
        API = ContextAPI.OpenGL,
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core
    })
    {
        Load += OnLoad;
        Paint += OnRender;
        Resize += OnResize;
        this.iChannelType = iChannelType;
        iChannelPath = new string[iChannelType.Length];
        for (int i = 0; i < iChannelType.Length; i++)
        {
            if (iChannelType[i].GetHashCode() <= 3)
                iChannelPath[i] = $"shaders/{iChannelType[i]}.glsl";
            else
                iChannelPath[i] = $"shaders/iChannel{i}.png";
        }
        MouseDown += (s, e) =>
        {
            mousePressed = true;
            mouseDownPos = new Vector2(e.X, Height - e.Y);
            mousePos = mouseDownPos;
        };
        MouseUp += (s, e) =>
        {
            mousePressed = false;
            mousePos = new Vector2(e.X, Height - e.Y);
        };
        MouseMove += (s, e) =>
        {
            if (mousePressed)
                mousePos = new Vector2(e.X, Height - e.Y);
        };
    }

    public void LoadShader(string path)
    {
        Code = File.ReadAllText(path);
        Code = CleanShaderCode(Code);
        if (IsHandleCreated)
            Init();
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(Code))
            Init();

        timer.Start();
    }
    private void OnResize(object? sender, EventArgs e)
    {
        GL.Viewport(0, 0, Width, Height);

        for (int i = 0; i < 4; i++)
        {
            buffers[i]?.Resize(Width, Height);
        }
    }
    private void Init()
    {
        string fragment = ConvertShadertoy(Code);
        program = CreateProgram(fragment);

        float[] quad =
        {
            -1f,-1f,
             1f,-1f,
            -1f, 1f,
             1f, 1f
        };

        input = GL.GenVertexArray();
        int vbo = GL.GenBuffer();

        GL.BindVertexArray(input);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

        iTimeLoc = GL.GetUniformLocation(program, "iTime");
        iResolutionLoc = GL.GetUniformLocation(program, "iResolution");
        iMouseLoc = GL.GetUniformLocation(program, "iMouse");

        InitChannels();
    }
    private void InitChannels()
    {
        for (int i = 0; i < iChannelType.Length; i++)
        {
            IChannelType type = iChannelType[i];

            if (type == IChannelType.Sampler2D)
            {
                channelTextures[i] = LoadIChannel(iChannelPath[i], TextureTarget.Texture2D);
                channelTargets[i] = TextureTarget.Texture2D;
            }
            else if (type == IChannelType.SamplerCube)
            {
                channelTextures[i] = LoadIChannel(iChannelPath[i], TextureTarget.TextureCubeMap, textureWrapMode: TextureWrapMode.ClampToEdge,generateMipmaps: true);
                channelTargets[i] = TextureTarget.TextureCubeMap;
            }
            else
            {
                string code = File.ReadAllText(iChannelPath[i]);
                string frag = ConvertShadertoy(code);

                buffers[i] = new ShaderBuffer(Width, Height, frag, CreateProgram);

                channelTextures[i] = buffers[i].Texture;
                channelTargets[i] = TextureTarget.Texture2D;
            }
        }
    }
    private void RenderBuffer(int i)
    {
        var buf = buffers[i];
        if (buf == null) return;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buf.FBO);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                 TextureTarget.Texture2D, buf.WriteTexture, 0);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(buf.Program);

        BindChannels(buf.Program);


        int selfChannelLoc = GL.GetUniformLocation(buf.Program, $"iChannel{i}");
        if (selfChannelLoc != -1)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + i);
            GL.BindTexture(TextureTarget.Texture2D, buf.ReadTexture);
            GL.Uniform1(selfChannelLoc, i);
        }

        SetCommonUniforms(buf.Program);

        GL.BindVertexArray(input);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        buf.Swap();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void OnRender(object? sender, PaintEventArgs e)
    {
        for (int i = 0; i < iChannelType.Length; i++)
        {
            if (iChannelType[i].GetHashCode() <= 3) 
            {
                RenderBuffer(i);
            }
        }

        frame++;

        for (int i = 0; i < 4; i++)
        {
            if (buffers[i] != null)
                channelTextures[i] = buffers[i].ReadTexture; 
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(program);

        BindChannels(program);
        SetCommonUniforms(program);

        GL.BindVertexArray(input);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        SwapBuffers();
        Invalidate();
    }
    private void BindChannels(int prog)
    {
        for (int i = 0; i < 4; i++)
        {
            if (channelTextures[i] != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(channelTargets[i], channelTextures[i]);

                int loc = GL.GetUniformLocation(prog, $"iChannel{i}");
                if (loc != -1)
                    GL.Uniform1(loc, i);
            }
        }
    }
    public string CleanShaderCode(string rawCode)
    {
        if (string.IsNullOrEmpty(rawCode)) return "";
        string noMultiLine = Regex.Replace(rawCode, @"(?<!/)/\*.*?\*/", "", RegexOptions.Singleline);
        string cleanCode = Regex.Replace(noMultiLine, @"//.*", "");
        return Regex.Replace(cleanCode, @"^\s*$\n|\r", "", RegexOptions.Multiline).Trim();
    }
    private string ConvertShadertoy(string code)
    {
        string outName = "fragColor";
        string inName = "fragCoord";

        var match = Regex.Match(code,
            @"void\s+mainImage\s*\(\s*out\s+vec4\s+(\w+)\s*,\s*(?:in\s+)?vec2\s+(\w+)\s*\)");

        if (match.Success)
        {
            outName = match.Groups[1].Value;
            inName = match.Groups[2].Value;
        }

        string safeOut = "_" + outName;
        string safeIn = "_" + inName;
        string channels = "";

        for (int i = 0; i < iChannelType.Length; i++)
        {
            IChannelType t = iChannelType[i];

            if (t == IChannelType.Sampler2D || t.GetHashCode() <= 3)
                channels += $"uniform sampler2D iChannel{i};\n";
            else if (t == IChannelType.SamplerCube)
                channels += $"uniform samplerCube iChannel{i};\n";
        }
        string header =
            "#version 330 core\r\n" +
            "out vec4 " + safeOut + ";\r\n" +

            "uniform vec3      iResolution;\n" +
            "uniform float     iTime;\n" +
            "uniform float     iTimeDelta;\n" +
            "uniform float     iFrameRate;\n" +
            "uniform int       iFrame;\n" +
            "uniform float     iChannelTime[4];\n" +
            "uniform vec3      iChannelResolution[4];\n" +
            "uniform vec4      iMouse;\n" +
            "uniform vec4      iDate;\r\n" +

            channels + "\r\n" +

            "#define " + safeIn + " (gl_FragCoord.xy)\r\n";

            string footer =
            "\r\nvoid main()\r\n" +
            "{\n" +
            "    mainImage(" + safeOut + ", " + safeIn + ");\n" +
            "}\r\n";

        return header + code + footer;
    }

    private int CreateProgram(string fs)
    {
        int f = Compile(ShaderType.FragmentShader, fs);
        int p = GL.CreateProgram();
        GL.AttachShader(p, f);
        GL.LinkProgram(p);

        GL.GetProgram(p, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
            throw new Exception(GL.GetProgramInfoLog(p));

        GL.DeleteShader(f);
        return p;
    }

    private int Compile(ShaderType type, string src)
    {
        int s = GL.CreateShader(type);
        GL.ShaderSource(s, src);
        GL.CompileShader(s);

        GL.GetShader(s, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
            throw new Exception(GL.GetShaderInfoLog(s));

        return s;
    }


    private int LoadIChannel(
        string path,
        TextureTarget target,
        TextureMinFilter textureMinFilter = TextureMinFilter.Linear,
        TextureMagFilter textureMagFilter = TextureMagFilter.Linear,
        TextureWrapMode textureWrapMode = TextureWrapMode.Repeat,
        bool generateMipmaps = false)
    {
        int tex = GL.GenTexture();
        GL.BindTexture(target, tex);

        using (var stream = File.OpenRead(path))
        {
            var img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            if (target == TextureTarget.Texture2D)
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.Rgba,
                    img.Width, img.Height, 0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    img.Data);
            }
            else if (target == TextureTarget.TextureCubeMap)
            {
                int faceSize = img.Width / 6;

                for (int i = 0; i < 6; i++)
                {
                    byte[] faceData = new byte[faceSize * faceSize * 4];

                    for (int y = 0; y < faceSize; y++)
                    {
                        for (int x = 0; x < faceSize; x++)
                        {
                            int srcX = i * faceSize + x;
                            int srcIndex = (y * img.Width + srcX) * 4;
                            int dstIndex = (y * faceSize + x) * 4;

                            faceData[dstIndex + 0] = img.Data[srcIndex + 0];
                            faceData[dstIndex + 1] = img.Data[srcIndex + 1];
                            faceData[dstIndex + 2] = img.Data[srcIndex + 2];
                            faceData[dstIndex + 3] = img.Data[srcIndex + 3];
                        }
                    }

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i,
                        0,
                        PixelInternalFormat.Rgba,
                        faceSize, faceSize, 0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        faceData);
                }
            }
        }

        GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)textureMinFilter);
        GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)textureMagFilter);

        if (target == TextureTarget.TextureCubeMap)
        {
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)textureWrapMode);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)textureWrapMode);
            GL.TexParameter(target, TextureParameterName.TextureWrapR, (int)textureWrapMode);
        }
        else
        {
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)textureWrapMode);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)textureWrapMode);
        }

        if (generateMipmaps)
        {
            GL.GenerateMipmap((GenerateMipmapTarget)target);
        }

        GL.BindTexture(target, 0);
        return tex;
    }
    private void SetCommonUniforms(int prog)
    {
        int timeLoc = GL.GetUniformLocation(prog, "iTime");
        int resLoc = GL.GetUniformLocation(prog, "iResolution");
        int mouseLoc = GL.GetUniformLocation(prog, "iMouse");
        int frameLoc = GL.GetUniformLocation(prog, "iFrame");

        if (timeLoc != -1)
            GL.Uniform1(timeLoc, (float)timer.Elapsed.TotalSeconds);

        if (resLoc != -1)
            GL.Uniform3(resLoc, Width, Height, 1f);

        if (mouseLoc != -1)
            GL.Uniform4(mouseLoc,
                mousePos.X, mousePos.Y,
                mousePressed ? mouseDownPos.X : 0f,
                mousePressed ? mouseDownPos.Y : 0f);

        if (frameLoc != -1)
            GL.Uniform1(frameLoc, frame); 
    }
}