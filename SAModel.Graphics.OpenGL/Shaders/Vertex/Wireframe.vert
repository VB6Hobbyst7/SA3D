﻿#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 12) uniform mat4 mvp;

void main()
{
	gl_Position = mvp * vec4(aPosition, 1);
	gl_Position.z -= 0.001f;
}