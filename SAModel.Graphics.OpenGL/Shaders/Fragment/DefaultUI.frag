#version 430 core

out vec4 FragColor;

in vec2 uv;

uniform sampler2D texture0;

void main()
{
	vec4 col = texture(texture0, uv);
	if(col.a == 0) discard;
	FragColor = col;
}