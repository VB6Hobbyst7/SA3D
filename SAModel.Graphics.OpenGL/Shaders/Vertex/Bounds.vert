#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

layout(location = 10) uniform mat4 world;
layout(location = 11) uniform mat4 normalMtx;
layout(location = 12) uniform mat4 mvp;

out vec3 fragpos;
out vec3 normal;

void main()
{
	gl_Position = mvp * vec4(aPosition, 1);
	normal = normalize(mat3(normalMtx) * aNormal);
	fragpos = vec3(world * vec4(aPosition, 1));
}