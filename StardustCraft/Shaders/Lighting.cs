using OpenTK.Mathematics;

namespace StardustCraft.Shaders
{
    public class Lighting
    {
        public struct DirLight
        {
            public Vector3 Direction;
            public Vector3 Ambient;
            public Vector3 Diffuse;
            public Vector3 Specular;
        }

        public struct PointLight
        {
            public Vector3 Position;
            public float Constant;
            public float Linear;
            public float Quadratic;
            public Vector3 Ambient;
            public Vector3 Diffuse;
            public Vector3 Specular;
        }

        // Luce direzionale (sole)
        public DirLight SunLight = new DirLight
        {
            Direction = new Vector3(-0.5f, -1.0f, -0.5f).Normalized(),
            Ambient = new Vector3(0.2f, 0.2f, 0.2f),
            Diffuse = new Vector3(0.8f, 0.8f, 0.8f),
            Specular = new Vector3(0.5f, 0.5f, 0.5f)
        };

        // Luce puntiforme (torcia del player)
        public PointLight TorchLight  = new PointLight
        {
            Position = Vector3.Zero,
            Constant = 1.0f,
            Linear = 0.09f,
            Quadratic = 0.032f,
            Ambient = new Vector3(0.05f, 0.05f, 0.05f),
            Diffuse = new Vector3(0.8f, 0.6f, 0.2f), // Colore arancione caldo
            Specular = new Vector3(1.0f, 0.8f, 0.3f)
        };

        // Tempo per ciclo giorno/notte
        public float TimeOfDay  = 0.5f; // 0.5 = mezzogiorno

        public void Update(float deltaTime, Vector3 playerPosition)
        {
            // Aggiorna ciclo giorno/notte
            TimeOfDay += deltaTime * 0.01f; // Più lento
            TimeOfDay %= 1.0f;

            // Aggiorna direzione sole (simula ciclo giorno/notte)
            float sunAngle = TimeOfDay * MathHelper.TwoPi;
            SunLight.Direction = new Vector3(
                MathF.Cos(sunAngle),
                MathF.Sin(sunAngle) * 0.5f + 0.5f, // Più alto/basso
                MathF.Sin(sunAngle)
            ).Normalized();

            // Regola intensità in base all'ora del giorno
            float sunHeight = SunLight.Direction.Y;
            float intensity = MathHelper.Clamp(sunHeight * 2.0f, 0.1f, 1.0f);

            SunLight.Diffuse = new Vector3(intensity, intensity, intensity);
            SunLight.Ambient = new Vector3(intensity * 0.3f, intensity * 0.3f, intensity * 0.3f);

            // Aggiorna posizione torcia (segue player)
            TorchLight.Position = playerPosition + new Vector3(0, 1.5f, 0); // Sopra la testa
        }

        public void ApplyToShader(Shader shader, Vector3 viewPos)
        {
            shader.Use();

            // Luce direzionale
            shader.SetVector3("dirLight.direction", SunLight.Direction);
            shader.SetVector3("dirLight.ambient", SunLight.Ambient);
            shader.SetVector3("dirLight.diffuse", SunLight.Diffuse);
            shader.SetVector3("dirLight.specular", SunLight.Specular);

            // Luce puntiforme (torcia)
            shader.SetVector3("pointLight.position", TorchLight.Position);
            shader.SetFloat("pointLight.constant", TorchLight.Constant);
            shader.SetFloat("pointLight.linear", TorchLight.Linear);
            shader.SetFloat("pointLight.quadratic", TorchLight.Quadratic);
            shader.SetVector3("pointLight.ambient", TorchLight.Ambient);
            shader.SetVector3("pointLight.diffuse", TorchLight.Diffuse);
            shader.SetVector3("pointLight.specular", TorchLight.Specular);

            // Altri uniform
            shader.SetVector3("viewPos", viewPos);
            shader.SetFloat("time", TimeOfDay * 24.0f * 60.0f * 60.0f); // Secondi nel giorno virtuale
        }

        // Versione semplice per shader semplice
        public void ApplySimpleLighting(Shader shader)
        {
            shader.Use();
            shader.SetVector3("lightDirection", SunLight.Direction);
            shader.SetVector3("lightColor", SunLight.Diffuse);
            shader.SetFloat("lightIntensity", SunLight.Diffuse.X); // Usa componente R come intensità
            shader.SetVector3("ambientColor", SunLight.Ambient);
        }
    }
}