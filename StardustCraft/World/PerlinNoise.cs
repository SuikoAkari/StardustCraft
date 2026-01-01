using System;
using System.Collections.Generic;

namespace StardustCraft.World
{
    public static class PerlinNoise
    {
        private static Random random = new Random();
        private static int[] permutation;
        private static int[] p;

        static PerlinNoise()
        {
            InitializePermutation();
        }
        public static float SimplexNoise(float x, float y)
        {
            // Implementazione semplificata
            return (Noise(x, y) + 1) * 0.5f;
        }
        public static float OctaveNoise3D(float x, float y, float z, int octaves, float persistence, float scale)
        {
            float total = 0;
            float frequency = scale;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }
        private static void InitializePermutation()
        {
            permutation = new int[256];
            for (int i = 0; i < 256; i++)
            {
                permutation[i] = i;
            }

            // Shuffle the permutation array
            for (int i = 0; i < 256; i++)
            {
                int swapIndex = random.Next(i, 256);
                int temp = permutation[i];
                permutation[i] = permutation[swapIndex];
                permutation[swapIndex] = temp;
            }

            // Duplicate the permutation array
            p = new int[512];
            for (int i = 0; i < 512; i++)
            {
                p[i] = permutation[i % 256];
            }
        }

        public static float Noise(float x, float y, float z = 0)
        {
            // Find unit cube that contains point
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            int zi = (int)MathF.Floor(z) & 255;

            // Find relative x, y, z of point in cube
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);
            float zf = z - MathF.Floor(z);

            // Compute fade curves for each of x, y, z
            float u = Fade(xf);
            float v = Fade(yf);
            float w = Fade(zf);

            // Hash coordinates of the 8 cube corners
            int aaa = p[p[p[xi] + yi] + zi];
            int aba = p[p[p[xi] + yi + 1] + zi];
            int aab = p[p[p[xi] + yi] + zi + 1];
            int abb = p[p[p[xi] + yi + 1] + zi + 1];
            int baa = p[p[p[xi + 1] + yi] + zi];
            int bba = p[p[p[xi + 1] + yi + 1] + zi];
            int bab = p[p[p[xi + 1] + yi] + zi + 1];
            int bbb = p[p[p[xi + 1] + yi + 1] + zi + 1];

            // And add blended results from 8 corners of cube
            float x1 = Lerp(
                Grad(aaa, xf, yf, zf),
                Grad(baa, xf - 1, yf, zf),
                u);
            float x2 = Lerp(
                Grad(aba, xf, yf - 1, zf),
                Grad(bba, xf - 1, yf - 1, zf),
                u);
            float y1 = Lerp(x1, x2, v);

            x1 = Lerp(
                Grad(aab, xf, yf, zf - 1),
                Grad(bab, xf - 1, yf, zf - 1),
                u);
            x2 = Lerp(
                Grad(abb, xf, yf - 1, zf - 1),
                Grad(bbb, xf - 1, yf - 1, zf - 1),
                u);
            float y2 = Lerp(x1, x2, v);

            return Lerp(y1, y2, w);
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        // Metodi utility per generazione terreno
        public static float OctaveNoise(float x, float y, int octaves, float persistence, float scale)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency * scale, y * frequency * scale) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        public static float RidgedNoise(float x, float y, float scale)
        {
            float noise = Noise(x * scale, y * scale);
            return 1 - MathF.Abs(noise);
        }

        public static float FractalNoise(float x, float y, int octaves, float lacunarity = 2.0f, float gain = 0.5f)
        {
            float amplitude = 1.0f;
            float frequency = 1.0f;
            float total = 0.0f;
            float maxAmplitude = 0.0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return total / maxAmplitude;
        }
    }
}