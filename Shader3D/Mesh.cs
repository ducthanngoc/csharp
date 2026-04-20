using Assimp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Shader3D
{
    public class Mesh
    {
        private int vao, vbo, ebo;
        private float[] vertices;
        private int[] indices;

        public Mesh(Assimp.Mesh mesh, Scene scene, string directory, Matrix4 transform)
        {
            List<float> verts = new List<float>();
            List<int> inds = new List<int>();

            for (int i = 0; i < mesh.VertexCount; i++)
            {
                var v = mesh.Vertices[i];
                var n = mesh.HasNormals ? mesh.Normals[i] : new Assimp.Vector3D(0, 1, 0);

                var pos = Vector3.TransformPosition(
                    new Vector3(v.X, v.Y, v.Z),
                    transform
                );

                verts.Add(pos.X);
                verts.Add(pos.Y);
                verts.Add(pos.Z);

                var normal = Vector3.TransformNormal(
                    new Vector3(n.X, n.Y, n.Z),
                    transform
                );

                normal = Vector3.Normalize(normal);

                verts.Add(normal.X);
                verts.Add(normal.Y);
                verts.Add(normal.Z);
            }

            foreach (var face in mesh.Faces)
            {
                foreach (var index in face.Indices)
                {
                    inds.Add(index);
                }
            }

            vertices = verts.ToArray();
            indices = inds.ToArray();

            SetupMesh();
        }

        private void SetupMesh()
        {
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            GL.BindVertexArray(vao);
            GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,indices.Length,DrawElementsType.UnsignedInt,0);
            GL.BindVertexArray(0);
        }
    }
}