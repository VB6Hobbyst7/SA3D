#version 430 core

out vec4 FragColor;

layout(location = 13) uniform float glblend;

// vertex data
in vec3 fragpos;
in vec3 normal;
in vec2 uv0;
in vec4 col0;
in float weightColor;

uniform sampler2D texture0;

// flag values
#define FLAT			0x01
#define NO_AMBIENT		0x02
#define NO_DIFFUSE		0x04
#define NO_SPECULAR		0x08
#define USE_TEXTURE		0x10
#define NORMAL_MAPPING	0x20

#define LIGHTINGMASK 0xFF000000
#define SMOOTH		0x01000000
#define FALLOFF		0x02000000
#define NORMALS		0x03000000
#define COLORS		0x04000000
#define WEIGHTS		0x05000000
#define TEXCOORDS	0x06000000
#define TEXTURES	0x07000000
#define Culling		0x08000000

layout(std140, binding = 0) uniform Material
{
						// alignment	offset
	vec3 viewPos;		// 16			0
	vec3 viewDir;		// 16			16
	vec3 lightDir;		// 16			32
	vec4 diffuse;		// 16			48	
	vec4 specular;		// 16			64
	vec4 ambient;		// 16			80
	float exponent;		// 4			96
	int flags;			// 4			100
};

float lighting(vec3 viewDirection)
{
	int lightingMode = flags & LIGHTINGMASK;
	if(lightingMode == FALLOFF)
	{
		return abs(dot(viewDirection, normal));
	}
	else
	{
		float lighting = dot(normal, lightDir);
		if(lightingMode == SMOOTH)
			lighting = (lighting + 1) / 2;
		lighting = max(0, lighting);
		return lighting;
	}

}

float highlights(vec3 viewDirection)
{
	vec3 reflectDir = reflect(-lightDir, normal);
	return pow(max(dot(viewDirection, reflectDir), 0.0), exponent);
}

vec4 calcWeightColor(float weight)
{
	float hue = mod(weight * -0.625f + 0.666f, 1.0f) * 6f;
    int index = int(hue);
    float ff = hue - index;
    float q = 1.0f - ff;

	vec4 result = vec4(0,0,0,1);
	if(index == 0)
	{
		result.r = 1.0;	
		result.g = ff;
	}
	else if(index == 1)
	{
		result.r = q;
		result.g = 1.0;	
	}
	else if(index == 2)
	{
		result.g = 1.0;	
		result.b = ff;
	}
	else if(index == 3)
	{
		result.g = q;
		result.b = 1.0;	
	}
	else if(index == 4)
	{
		result.b = 1.0;	
		result.r = ff;
	}
	else if(index == 5)
	{
		result.b = q;
		result.r = 1.0;	
	}

	return result;
}

void main()
{
	vec4 col = vec4(0);
	int lightingMode = flags & LIGHTINGMASK;
	if(lightingMode == NORMALS)
		col = (vec4(normal, 1) + vec4(1)) / 2 ;
	else if(lightingMode == COLORS)
		col = col0;
	else if(lightingMode == TEXCOORDS)
		col = vec4(mod(uv0, 1), 1, 1);
	else if(lightingMode == WEIGHTS)
		col = calcWeightColor(weightColor);
	else if(lightingMode == TEXTURES)
		col = texture(texture0, uv0);
	else if(lightingMode == Culling)
	{
		if(gl_FrontFacing)
			col = vec4(0,0,1,1);
		else col = vec4(1,0,0,1);
	}
	else
	{
		vec4 tex = vec4(1);
		if((flags & USE_TEXTURE) != 0)
		{
			tex = texture(texture0, uv0);
		}
		tex *= col0;

		if((flags & FLAT) == 0)
		{
			float alpha = tex.a;
			vec3 viewDirection = viewDir;
			if(viewDir.x == 0 && viewDir.y == 0 && viewDir.z == 0)
			{
				viewDirection = normalize(viewPos - fragpos);
			}

			// checking ambient flag
			if((flags & NO_AMBIENT) == 0)
			{
				col += ambient * tex;
				alpha *= ambient.a;
			}

			// checking the diffuse flag
			if((flags & NO_DIFFUSE) == 0)
			{
				col += tex * diffuse * lighting(viewDirection);
				alpha *= diffuse.a;
			}

			// checking the specular flag
			if((flags & NO_SPECULAR) == 0)
			{
				col += specular * highlights(viewDirection) * 0.5f;
			}
		
			col.a = alpha;
		}
	}
	if(col.a == 0 && glblend == 1) discard;
	FragColor = col;
}