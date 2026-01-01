#version 460 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vTexCoord;
layout (location = 2) in vec4 vLight;

out vec2  aTexCoord;
out vec4  aColor;
out float aDistance;

uniform mat4  model;
uniform mat4  view;
uniform mat4  projection;
uniform vec3  viewPos;
uniform float uGamma;
uniform float uTime;

void main() {
    aTexCoord = vTexCoord;

    // testing
    vec3 skyLightColor = vec3(1);
    float color = 4.0 * cos(uTime / 30.0) * cos(uTime / 30.0) - 1.5;
    
    //if (color > 1.0)
    //    skyLightColor = vec3(1.0);
    //else if (color < 0.2)
    //    skyLightColor = vec3(0.2);
    //else
    //    skyLightColor = vec3(color);

    aColor = vec4(pow(vLight.rgb, vec3(uGamma)), 1.0);
    aColor.rgb = aColor.rgb*skyLightColor * vLight.a;

    vec3 pos3D = (model * vec4(vPosition, 1.0)).xyz - viewPos.xyz;
    aDistance = length(view * model * vec4(pos3D.x, 0.1 * pos3D.y, pos3D.z, 0.0));

    gl_Position = projection * view * model * vec4(vPosition, 1.0);
}
