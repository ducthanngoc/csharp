using Assimp;
using OpenTK.Mathematics;

namespace Shader3D
{
    public class Model
    {
        private List<Mesh> meshes = new List<Mesh>();
        private Scene scene;
        private string directory;

        public Model(string path)
        {
            LoadModel(path);
        }

        private void LoadModel(string path)
        {
            AssimpContext importer = new AssimpContext();

            scene = importer.ImportFile(path,
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateSmoothNormals |
                PostProcessSteps.FlipUVs |
                PostProcessSteps.JoinIdenticalVertices);

            if (scene == null || scene.RootNode == null)
                throw new Exception("Load model failed");

            directory = Path.GetDirectoryName(path);

            ProcessNode(scene.RootNode, Matrix4.Identity);
        }

        private void ProcessNode(Node node, Matrix4 parentTransform)
        {
            Matrix4 transform = parentTransform * ConvertMatrix(node.Transform);

            foreach (int meshIndex in node.MeshIndices)
            {
                var mesh = scene.Meshes[meshIndex];
                meshes.Add(new Mesh(mesh, scene, directory, transform));
            }

            foreach (var child in node.Children)
            {
                ProcessNode(child, transform);
            }
        }

        Matrix4 ConvertMatrix(Matrix4x4 m)
        {
            return new Matrix4(
                m.A1, m.B1, m.C1, m.D1,
                m.A2, m.B2, m.C2, m.D2,
                m.A3, m.B3, m.C3, m.D3,
                m.A4, m.B4, m.C4, m.D4
            );
        }

        public void Draw()
        {
            foreach (var mesh in meshes)
            {
                mesh.Draw();
            }
        }
    }
}