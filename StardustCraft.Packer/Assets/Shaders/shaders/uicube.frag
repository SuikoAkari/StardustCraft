#version 330 core
in vec2 vUV;
flat in int vTex;

uniform sampler2D uTextures[6];
uniform float alpha;

out vec4 FragColor;

void main()
{
    vec4 c = texture(uTextures[vTex], vUV);
    FragColor = vec4(c.rgb, c.a * alpha);
}