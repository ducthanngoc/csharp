using OpenTK.Graphics.OpenGL4;

public class ShaderBuffer
{
    public int FBO;
    public int[] Textures = new int[2];
    public int Program;
    public int Texture2;
    public int Width;
    public int Height;
    public int WriteIndex = 0;
    public int ReadTexture => Textures[1 - WriteIndex];
    public int WriteTexture => Textures[WriteIndex];

    public int Texture => ReadTexture;
    public ShaderBuffer(int width, int height, string fragCode, System.Func<string, int> createProgram)
    {
        Width = width;
        Height = height;
        Program = createProgram(fragCode);

        FBO = GL.GenFramebuffer();
        GL.GenTextures(2, Textures);

        for (int i = 0; i < 2; i++)
        {
            GL.BindTexture(TextureTarget.Texture2D, Textures[i]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f,
                Width, Height, 0, PixelFormat.Rgba, PixelType.Float, System.IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Swap()
    {
        WriteIndex = 1 - WriteIndex; 
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
        for (int i = 0; i < 2; i++)
        {
            GL.BindTexture(TextureTarget.Texture2D, Textures[i]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f,
                Width, Height, 0, PixelFormat.Rgba, PixelType.Float, System.IntPtr.Zero);
        }
    }
}