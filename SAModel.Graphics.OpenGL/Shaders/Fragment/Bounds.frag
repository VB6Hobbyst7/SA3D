#version 430 core

out vec4 FragColor;

// vertex data
in vec3 normal;
in vec3 fragpos;

uniform vec3 viewPos;
uniform vec3 viewDir;

void main()
{
	//vec3 viewDirection = viewDir;
	//if(viewDir.x == 0 && viewDir.y == 0 && viewDir.z == 0)
	//	viewDirection = normalize(viewPos - fragpos);
	//float falloff = abs(dot(viewDirection, normal));

	FragColor = vec4(1,1,1, 0.1f);
}