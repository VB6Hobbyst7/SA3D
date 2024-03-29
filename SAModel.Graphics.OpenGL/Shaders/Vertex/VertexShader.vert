﻿#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec4 aCol0;
layout(location = 3) in vec2 aUV0;
layout(location = 4) in float aWeightColor;

layout(location = 10) uniform mat4 world;
layout(location = 11) uniform mat4 normalMtx;
layout(location = 12) uniform mat4 mvp;

out vec3 fragpos;
out vec3 normal;
out vec2 uv0;
out vec4 col0;
out float weightColor;

void main()
{
	uv0 = aUV0;
	col0 = aCol0;
	weightColor = aWeightColor;

	gl_Position = mvp * vec4(aPosition, 1);

	normal = normalize(mat3(normalMtx) * aNormal);
	fragpos = vec3(world * vec4(aPosition, 1));
}