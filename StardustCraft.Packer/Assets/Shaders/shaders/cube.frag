#version 330 core
out vec4 FragColor;
in vec2 TexCoord;

uniform float alpha; // trasparenza cubo (0.0 - 1.0)

void main()
{
    // Colore arancio trasparente
    FragColor = vec4(1.0, 0.5, 0.2, alpha);
}