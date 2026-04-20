
using Assimp;
using OpenTK;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Shader3D
{
    public partial class Form1 : Form
    {
        private GLControl glControl;
        private Model model;
        private int shaderProgram;

        private Matrix4 modelMat = Matrix4.CreateScale(0.01f);
        private Matrix4 view;
        private Matrix4 projection;

        bool isMouseDown = false;
        Point lastMouse;
        float yaw = -90f;
        float pitch = 0f;
        float distance ;
        public Form1()
        {
            InitializeComponent();

            glControl = new GLControl();
            glControl.Dock = DockStyle.Fill;
            this.Controls.Add(glControl);

            glControl.Load += GL_Load;
            glControl.Paint += GL_Paint;
            glControl.Resize += GL_Resize;
            glControl.MouseMove += (s, e) =>
            {
                if (!isMouseDown) return;

                float dx = e.X - lastMouse.X;
                float dy = e.Y - lastMouse.Y;

                lastMouse = e.Location;

                yaw += dx * 0.1f;
                pitch += dy * 0.1f;

                pitch = MathHelper.Clamp(pitch, -89f, 89f);
            };
            glControl.MouseDown += (s, e) =>
            {
                isMouseDown = true;
                lastMouse = e.Location;
            };

            glControl.MouseUp += (s, e) =>
            {
                isMouseDown = false;
            };
            glControl.MouseWheel += (s, e) =>
            {
                if (e.Delta > 0)
                    distance -= 0.1f;
                else
                    distance += 0.1f; 

                distance = MathHelper.Clamp(distance, 0, 1000f);
            };
            view = Matrix4.LookAt(
                new Vector3(0, 0, distance),
                Vector3.Zero,
                Vector3.UnitY
            );
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 16; 
            timer.Tick += (s, e) => glControl.Invalidate();
            timer.Start();
        }

        private void GL_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);

            shaderProgram = CreateShader();

            model = new Model(@"Dragon 2.5_fbx.fbx");

            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), glControl.Width / (float)glControl.Height, 0.1f, 100f);
        }

        private void GL_Paint(object sender, PaintEventArgs e)
        {
            float deltaTime = 0.016f;

            Vector3 cameraPos;

            cameraPos.X = distance * MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            cameraPos.Y = distance * MathF.Sin(MathHelper.DegreesToRadians(pitch));
            cameraPos.Z = distance * MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));

            view = Matrix4.LookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
            modelMat = Matrix4.CreateScale(0.1f);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "viewPos"), cameraPos);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            glControl.Width / (float)glControl.Height,
            0.1f,
            100f);
            GL.UseProgram(shaderProgram);

            int modelLoc = GL.GetUniformLocation(shaderProgram, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram, "projection");
            GL.UniformMatrix4(modelLoc, false, ref modelMat);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), new Vector3(3, 3, 3));
            model.Draw();

            glControl.SwapBuffers();
        }

        private void GL_Resize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
        }

        private int CreateShader()
        {
            string vertex = @"#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

out vec3 Normal;
out vec3 FragPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    FragPos = vec3(model * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(model))) * aNormal;

    gl_Position = projection * view * vec4(FragPos, 1.0);
}";

            string fragment = @"#version 330 core

out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;

uniform vec3 lightPos;
uniform vec3 viewPos;

void main()
{
    vec3 lightColor = vec3(1.0);
    vec3 objectColor = vec3(0.8, 0.8, 0.8);

    // ambient
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * lightColor;

    // diffuse
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse) * objectColor;
    FragColor = vec4(result, 1.0);
}";

            int v = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(v, vertex);
            GL.CompileShader(v);

            int f = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(f, fragment);
            GL.CompileShader(f);

            int program = GL.CreateProgram();
            GL.AttachShader(program, v);
            GL.AttachShader(program, f);
            GL.LinkProgram(program);

            GL.DeleteShader(v);
            GL.DeleteShader(f);

            return program;
        }
    }
}
