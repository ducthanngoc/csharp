using System.IO;

namespace Shader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            IChannelType[] iChannelType =
            {
                IChannelType.BufferA,
                IChannelType.Sampler2D
            };
        var gl = new ShaderGLControl(iChannelType)
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(gl);
            gl.LoadShader("shaders/image.frag");
        }
    }
}
