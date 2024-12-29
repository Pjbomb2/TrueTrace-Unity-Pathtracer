static uint ScatteringTexRSize = 32;
static uint ScatteringTexMUSize = 128;
static uint ScatteringTexMUSSize = 32;
static uint ScatteringTexNUSize = 8;

#define bottom_radius (6360 * 1000.0f)
#define top_radius (6420 * 1000.0f)
static uint TransmittanceTexWidth = 256;
static uint TransmittanceTexHeight = 64;

float RayleighPhaseFunction(float nu) {
	float k = 3.0 / (16.0 * PI);
	return k * (1.0 + nu * nu);
}
float GetTextureCoordFromUnitRange(float x, int texture_size) {
	return 0.5f / (float)texture_size + x * (1.0f - 1.0f / (float)texture_size);
}

float MiePhaseFunction(float g, float nu) {
	float k = 3.0 / (8.0 * PI) * (1.0 - g * g) / (2.0 + g * g);
	return k * (1.0 + nu * nu) / pow(1.0 + g * g - 2.0 * g * nu, 1.5);
}

float GetUnitRangeFromTextureCoord(float u, int texture_size) {
	return (u - 0.5f / (float)texture_size) / (1.0f - 1.0f / (float)texture_size);
}

float DistanceToTopAtmosphereBoundary(float r, float mu) {
	float discriminant = r * r * (mu * mu - 1.0f) + top_radius * top_radius;
	return max(-r * mu + sqrt(max(discriminant, 0.0f)), 0.0f);
}
float DistanceToBottomAtmosphereBoundary(float r, float mu) {
	if (mu < -1 || mu > 1 || r > top_radius) return 0;
	float discriminant = r * r * (mu * mu - 1.0f) + bottom_radius * bottom_radius;
	return max(-r * mu - sqrt(max(discriminant, 0.0f)), 0.0f);
}


float4 GetScatteringTextureUvwzFromRMuMuSNu(float r, float mu, float mu_s, float nu, bool ray_r_mu_intersects_ground) {
	float H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);
	float rho = sqrt(max(r * r - bottom_radius * bottom_radius, 0.0f));
	float u_r = GetTextureCoordFromUnitRange(rho / H, ScatteringTexRSize);

	float r_mu = r * mu;
	float discriminant = r_mu * r_mu - r * r + bottom_radius * bottom_radius;
	float u_mu;
	if (ray_r_mu_intersects_ground) {
		float d = -r_mu - sqrt(max(discriminant, 0.0f));
		float d_min = r - bottom_radius;
		float d_max = rho;
		u_mu = 0.5f - 0.5f * GetTextureCoordFromUnitRange((d_max == d_min) ? 0.0f : (d - d_min) / (d_max - d_min), ScatteringTexMUSize / 2);
	}
	else {
		float d = -r_mu + sqrt(max(discriminant + H * H, 0.0f));
		float d_min = top_radius - r;
		float d_max = rho + H;
		u_mu = 0.5f + 0.5f * GetTextureCoordFromUnitRange((d - d_min) / (d_max - d_min), ScatteringTexMUSize / 2);
	}

	float d = DistanceToTopAtmosphereBoundary(bottom_radius, mu_s);
	float d_min = top_radius - bottom_radius;
	float d_max = H;
	float a = (d - d_min) / (d_max - d_min);
	float D = DistanceToTopAtmosphereBoundary(bottom_radius, -0.8f);
	float A = (D - d_min) / (d_max - d_min);

	float u_mu_s = GetTextureCoordFromUnitRange(max(1.0f - a / A, 0.0f) / (1.0f + a), ScatteringTexMUSSize);

	float u_nu = (nu + 1.0f) / 2.0f;
	return float4(u_nu, u_mu_s, u_mu, u_r);
}


Texture3D<float4> ScatteringTex;
Texture2D<float4> TransmittanceTex;
Texture2D<float4> IrradianceTex;
SamplerState linearClampSampler;
SamplerState sampler_ScatteringTex_trilinear_clamp;

#define rayleigh_scattering float3(0.00585f, 0.013558f, 0.03310f)
#define mie_scattering 0.003996f

float ClampCosine(float mu) {
	return clamp(mu, -1.0f, 1.0f);
}

float ClampRadius(float r) {
	return clamp(r, bottom_radius, top_radius);
}


float3 GetExtrapolatedSingleMieScattering(
	float4 scattering) {
	if (scattering.r == 0.0) {
		return 0;
	}
	return scattering.rgb * scattering.a / scattering.r *
		(rayleigh_scattering.r / mie_scattering.r) *
		(mie_scattering / rayleigh_scattering);
}

float3 GetCombinedScattering(
	float r, float mu, float mu_s, float nu,
	bool ray_r_mu_intersects_ground,
	inout float3 single_mie_scattering) {
	float4 uvwz = GetScatteringTextureUvwzFromRMuMuSNu(r, mu, mu_s, nu, ray_r_mu_intersects_ground);
	float tex_coord_x = uvwz.x * float(ScatteringTexNUSize - 1);
	float tex_x = floor(tex_coord_x);
	float lerp2 = tex_coord_x - tex_x;
	float3 uvw0 = float3((tex_x + uvwz.y) / float(ScatteringTexNUSize),
		uvwz.z, uvwz.w);
	float3 uvw1 = float3((tex_x + 1.0 + uvwz.y) / float(ScatteringTexNUSize),
		uvwz.z, uvwz.w);
	float4 combined_scattering =
		ScatteringTex.SampleLevel(sampler_ScatteringTex_trilinear_clamp, uvw0, 0) * (1.0 - lerp2) +
		ScatteringTex.SampleLevel(sampler_ScatteringTex_trilinear_clamp, uvw1, 0) * lerp2;
	float3 scattering = float3(combined_scattering.rgb);
	single_mie_scattering =
		GetExtrapolatedSingleMieScattering(combined_scattering);
	return scattering;
}

bool RayIntersectsGround(float r, float mu) {
	return (mu < 0.0f && r * r * (mu * mu - 1.0f) + bottom_radius * bottom_radius >= 0.0f);
}

float2 GetTransmittanceTextureUvFromRMu(float r, float mu) {
	float H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);

	float rho = sqrt(max(r * r - bottom_radius * bottom_radius, 0.0f));

	float d = DistanceToTopAtmosphereBoundary(r, mu);
	float d_min = top_radius - r;
	float d_max = rho + H;
	float x_mu = (d - d_min) / (d_max - d_min);
	float x_r = rho / H;
	return float2(GetTextureCoordFromUnitRange(x_mu, TransmittanceTexWidth), GetTextureCoordFromUnitRange(x_r, TransmittanceTexHeight));
}

SamplerState _LinearClamp;

float3 GetTransmittanceToTopAtmosphereBoundary(float r, float mu) {
	float2 uv = GetTransmittanceTextureUvFromRMu(r, mu);
	return TransmittanceTex.SampleLevel(_LinearClamp, uv, 0).rgb;
}

float3 GetTransmittance(float r, float mu, float d, bool ray_r_mu_intersects_ground) {
	float r_d = clamp(sqrt(d * d + 2.0f * r * mu * d + r * r), bottom_radius, top_radius);
	float mu_d = clamp((r * mu + d) / r_d, -1.0f, 1.0f);
	if (ray_r_mu_intersects_ground) {
		return min((GetTransmittanceToTopAtmosphereBoundary(r_d, -mu_d) /
			GetTransmittanceToTopAtmosphereBoundary(r, -mu)),
			float3(1.0f, 1.0f, 1.0f));
	} else {
		return min((GetTransmittanceToTopAtmosphereBoundary(r, mu) /
			GetTransmittanceToTopAtmosphereBoundary(r_d, mu_d)),
			float3(1.0f, 1.0f, 1.0f));
	}
}
#define IrradianceTexWidth 64
#define IrradianceTexHeight 16
float2 GetIrradianceTextureUvFromRMuS(float r, float mu_s) {
	float x_r = (r - bottom_radius) /
		(top_radius - bottom_radius);
	float x_mu_s = mu_s * 0.5 + 0.5;
	return float2(GetTextureCoordFromUnitRange(x_mu_s, IrradianceTexWidth),
		GetTextureCoordFromUnitRange(x_r, IrradianceTexHeight));
}

float3 GetIrradiance(float r, float mu_s) {
	float2 uv = GetIrradianceTextureUvFromRMuS(r, mu_s);
	return IrradianceTex.SampleLevel(_LinearClamp, uv, 0);
}

float3 GetTransmittanceToSun(float r, float mu_s) {
	float sin_theta_h = bottom_radius / r;
	float cos_theta_h = -sqrt(max(1.0f - sin_theta_h * sin_theta_h, 0.0f));
	return (GetTransmittanceToTopAtmosphereBoundary(r, mu_s)) *
		smoothstep(-sin_theta_h * (0.00935f / 2.0f) / 1.0f,
			sin_theta_h * (0.00935f / 2.0f) / 1.0f,
			mu_s - cos_theta_h);
}

float3 GetSkyRadianceToPoint(
	float3 camera, float3 pos,
	float3 sun_direction, out float3 transmittance)
{
	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float3 view_ray = normalize(pos - camera);
	float r = length(camera);
	float rmu = dot(camera, view_ray);
	float distance_to_top_atmosphere_boundary = -rmu - sqrt(rmu * rmu - r * r + top_radius * top_radius);

	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0)
	{
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	}

	// Compute the r, mu, mu_s and nu parameters for the first texture lookup.
	float mu = rmu / r;
	float mu_s = dot(camera, sun_direction) / r;
	float nu = dot(view_ray, sun_direction);
	float d = length(pos - camera);
	bool ray_r_mu_intersects_ground = RayIntersectsGround(r, mu);

	transmittance = GetTransmittance(
		r, mu, d, ray_r_mu_intersects_ground);

	float3 single_mie_scattering;
	float3 scattering = GetCombinedScattering(
		r, mu, mu_s, nu, ray_r_mu_intersects_ground,
		single_mie_scattering);

	// Compute the r, mu, mu_s and nu parameters for the second texture lookup.
	// If shadow_length is not 0 (case of light shafts), we want to ignore the
	// scattering along the last shadow_length meters of the view ray, which we
	// do by subtracting shadow_length from d (this way scattering_p is equal to
	// the S|x_s=x_0-lv term in Eq. (17) of our paper).
	d = max(d, 0.0);
	float r_p = ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));
	float mu_p = (r * mu + d) / r_p;
	float mu_s_p = (r * mu_s + d * nu) / r_p;

	float3 single_mie_scattering_p;
	float3 scattering_p = GetCombinedScattering(
		r_p, mu_p, mu_s_p, nu, ray_r_mu_intersects_ground,
		single_mie_scattering_p);

	// Combine the lookup results to get the scattering between camera and point.
	float3  shadow_transmittance = transmittance;
	scattering = scattering - shadow_transmittance * scattering_p;
	single_mie_scattering = single_mie_scattering - shadow_transmittance * single_mie_scattering_p;
	// Hack to avoid rendering artifacts when the sun is below the horizon.
	single_mie_scattering = GetExtrapolatedSingleMieScattering(float4(scattering, single_mie_scattering.r));

	single_mie_scattering = single_mie_scattering *
		smoothstep(0, 0.01, mu_s);

	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(0.8f, nu);
}
float3 GetSunAndSkyIrradiance(float3 groundpoint, float3 normal, float3 sun_direction, inout float3 sky_irradiance) {
	float r = length(groundpoint);
	float mu_s = dot(groundpoint, sun_direction) / r;

	sky_irradiance = GetIrradiance(r, mu_s) * (1.0f + abs(dot(normal, groundpoint)) / r) * 0.5f;
	return GetTransmittanceToSun(r, mu_s) * max(dot(normal, sun_direction), 0);
}
float3 GroundColor;
float3 ClayColor;
float3 GetSkyRadiance(
	float3 camera, float3 view_ray, float shadow_length,
	float3 sun_direction, inout float3 transmittance, inout float3 debug) {
	// camera /= 2048.0f;
	// camera.y = max(camera.y, 0);
	camera.y = max(0.3f, camera.y);
	camera.y += bottom_radius;

	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float r = length(camera);
	float rmu = dot(camera, view_ray);
	float distance_to_top_atmosphere_boundary = -rmu -
		sqrt(rmu * rmu - r * r + top_radius * top_radius);
	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0) {
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	} else if (r >= top_radius) {
		// If the view ray does not intersect the atmosphere, simply return 0.
		transmittance = 1;
		return 0;
	}
	// Compute the r, mu, mu_s and nu parameters needed for the texture lookups.
	float mu = rmu / r;
	float mu_s = dot(camera, sun_direction) / r;
	float nu = dot(view_ray, sun_direction);
	bool ray_r_mu_intersects_ground = RayIntersectsGround(r, mu);

	transmittance = ray_r_mu_intersects_ground ? 0.0 :
		(GetTransmittanceToTopAtmosphereBoundary(r, mu));
	float3 single_mie_scattering;
	float3 scattering;
		scattering = GetCombinedScattering(
			r, mu, mu_s, nu, ray_r_mu_intersects_ground,
			single_mie_scattering);
		if(ray_r_mu_intersects_ground) {
			float3 Position = camera + view_ray * DistanceToBottomAtmosphereBoundary(r, mu);
			float3 Normal = normalize(Position - float3(0,-bottom_radius,0));
			float3 sky_irradiance;

			float3 sun_irradiance = GetSunAndSkyIrradiance(Position, Normal, sun_direction, sky_irradiance);
			float3 trans;
			float3 in_scatter = GetSkyRadianceToPoint(camera, Position, sun_direction, trans);
			debug = (GroundColor * (1.0f / PI) * (sun_irradiance + sky_irradiance)) * trans + in_scatter;
		}
	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(0.8f, nu);
}



bool RayIntersectsGround2(float r, float mu) {
	return (mu < -0.01f && r * r * (mu * mu - 1.0f) + bottom_radius * bottom_radius >= 0.0f);
}

float3 GetSkyTransmittance(
	float3 camera, float3 view_ray, float shadow_length,
	float3 sun_direction) {
	// camera /= 2048.0f;
	camera.y = max(0.3f, camera.y);
	camera.y += bottom_radius;

	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float r = length(camera);
	float rmu = dot(camera, view_ray);
	float distance_to_top_atmosphere_boundary = -rmu -
		sqrt(rmu * rmu - r * r + top_radius * top_radius);
	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0) {
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	} else if (r >= top_radius) {
		// If the view ray does not intersect the atmosphere, simply return 0.
		return 1;
	}
	// Compute the r, mu, mu_s and nu parameters needed for the texture lookups.
	float mu = rmu / r;
	float mu_s = dot(camera, sun_direction) / r;
	float nu = dot(view_ray, sun_direction);
	bool ray_r_mu_intersects_ground = RayIntersectsGround2(r, mu);

	return ray_r_mu_intersects_ground ? 0.0 :
		(GetTransmittanceToTopAtmosphereBoundary(r, mu));
}



Texture3D<float4> CloudShapeDetailTex;
Texture3D<float4> CloudShapeTex;
Texture2D<float4> localWeatherTexture;
SamplerState my_linear_repeat_sampler;

// Scattering parameters
#define albedo float3(0.98, 0.98, 0.98)
#define powderScale 0.8
#define powderExponent 200
#define scatterAnisotropy1 0.6
#define scatterAnisotropy2 -0.3
#define scatterAnisotropyMix 0.5
#define skyIrradianceScale 0.1

// Raymarch to clouds
#define maxIterations 500
#define initialStepSize 100
#define maxStepSize 1000
#define maxRayDistance 1.5e5
#define minDensity 1e-5
#define minTransmittance 1e-2

#define PI 3.14159265359
#define PI2 6.28318530718
#define PI_HALF 1.5707963267949
#define RECIPROCAL_PI 0.31830988618
#define RECIPROCAL_PI2 0.15915494
#define LOG2 1.442695
#define EPSILON 1e-6

#define maxLayerHeights float4(1200,5000,8000,0)
#define minLayerHeights float4(600,4500,6700,0)
#define weatherExponents float4(1,1,3,1)
#define localWeatherFrequency float2(200,150)
#define coverage 0.3
#define coverageFilterWidths float4(0.6,0.3,0.5,0)
#define detailAmounts float4(1,0.8,0.3,0)
#define extinctionCoeffs float4(0.3,0.1,0.005,0)
#define shapeFrequency 0.0003
#define MULTI_SCATTERING_OCTAVES 8
#define ellipsoidCenter float3(0,0,0)
#define minHeight 0
#define maxHeight 8000

float inverseLerp(const float x, const float y, const float a) {
  return (a - x) / (y - x);
}

float2 inverseLerp(const float2 x, const float2 y, const float2 a) {
  return (a - x) / (y - x);
}

float3 inverseLerp(const float3 x, const float3 y, const float3 a) {
  return (a - x) / (y - x);
}

float4 inverseLerp(const float4 x, const float4 y, const float4 a) {
  return (a - x) / (y - x);
}

float remap(const float x, const float min1, const float max1, const float min2, const float max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float2 remap(const float2 x, const float2 min1, const float2 max1, const float2 min2, const float2 max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float2 remap(const float2 x, const float min1, const float max1, const float min2, const float max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float3 remap(const float3 x, const float3 min1, const float3 max1, const float3 min2, const float3 max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float3 remap(const float3 x, const float min1, const float max1, const float min2, const float max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float4 remap(const float4 x, const float4 min1, const float4 max1, const float4 min2, const float4 max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float4 remap(const float4 x, const float min1, const float max1, const float min2, const float max2) {
  return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}


float2 getGlobeUv(const float3 position) {
  float2 st = normalize(position.yx);
  float phi = atan(st.x / st.y);
  float theta = asin(normalize(position).z);
  return float2(phi * RECIPROCAL_PI2 + 0.5, theta * RECIPROCAL_PI + 0.5);
}

// float getMipLevel(const float2 uv) {
//   float2 coord = uv * resolution;
//   float2 ddx = dFdx(coord);
//   float2 ddy = dFdy(coord);
//   return max(0.0, 0.5 * log2(max(dot(ddx, ddx), dot(ddy, ddy))));
// }

struct WeatherSample {
  float4 heightFraction; // Normalized height of each layer
  float4 density;
};

float4 shapeAlteringFunction(const float4 heightFraction, const float bias) {
  // Apply a semi-circle transform to round the clouds towards the top.
  float4 biased = pow(heightFraction, bias);
  float4 x = clamp(biased * 2.0 - 1.0, -1.0, 1.0);
  return 1.0 - x * x;
}

WeatherSample sampleWeather(const float2 uv, const float height, const float mipLevel) {
  WeatherSample weather;
  weather.heightFraction = saturate(
    remap(height, minLayerHeights, maxLayerHeights, 0.0, 1.0)
  );

  float4 localWeather = pow(
    localWeatherTexture.SampleLevel(my_linear_repeat_sampler, uv * localWeatherFrequency, mipLevel),
    weatherExponents
  );
  float4 heightScale = shapeAlteringFunction(weather.heightFraction, 0.4);

  // Modulation to control weather by coverage parameter.
  // Reference: https://github.com/Prograda/Skybolt/blob/master/Assets/Core/Shaders/Clouds.h#L63
  float4 factor = 1.0 - coverage * heightScale;
  weather.density = saturate(
    remap(
      lerp(localWeather, 1.0, coverageFilterWidths),
      factor,
      factor + coverageFilterWidths,
      0.0,
      1.0
    )
  );

  return weather;
}

float sampleDensityDetail(WeatherSample weather, const float3 position, const float mipLevel) {
  float4 density = weather.density;
  if (mipLevel < 2.0) {
    float shape = CloudShapeTex.SampleLevel(my_linear_repeat_sampler, position * shapeFrequency, 0.0).r;
    // shape = pow(shape, 6.0) * 0.4; // Modulation for whippier shape
    shape = 1.0 - shape; // Or invert for fluffy shape
    density = lerp(density, saturate(remap(density, shape, 1.0, 0.0, 1.0)), detailAmounts);

    #ifdef USE_DETAIL
    if (mipLevel < 1.0) {
      float detail = shapeDetailTexture.SampleLevel(my_linear_repeat_sampler, position * shapeDetailFrequency, 0.0).r;
      // Fluffy at the top and whippy at the bottom.
      float4 modifier = lerp(
        float4(pow(detail, 6.0)),
        float4(1.0 - detail),
        saturate(remap(weather.heightFraction, 0.2, 0.4, 0.0, 1.0))
      );
      modifier = lerp(float4(0.0), modifier, detailAmounts);
      density = saturate(
        remap(density * 2.0, float4(modifier * 0.5), float4(1.0), float4(0.0), float4(1.0))
      );
    }
    #endif
  }
  // Nicely decrease density at the bottom.
  return saturate(dot(density, extinctionCoeffs * weather.heightFraction));
}


float2 henyeyGreenstein(const float2 g, const float cosTheta) {
  float2 g2 = g * g;
  const float reciprocalPi4 = 0.07957747154594767;
  return reciprocalPi4 * ((1.0 - g2) / pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
}

float phaseFunction(const float cosTheta, const float attenuation) {
  float2 g = float2(scatterAnisotropy1, scatterAnisotropy2);
  float2 weights = float2(1.0 - scatterAnisotropyMix, scatterAnisotropyMix);
  return dot(henyeyGreenstein(g * attenuation, cosTheta), weights);
}

float sampleOpticalDepth(
  const float3 rayOrigin,
  const float3 rayDirection,
  const int iterations,
  const float mipLevel
) {
  float stepSize = 40.0 / float(iterations);
  float opticalDepth = 0.0;
  float stepScale = 1.0;
  float prevStepScale = 0.0;
  for (int i = 0; i < iterations; ++i) {
    float3 position = rayOrigin + rayDirection * stepScale * stepSize;
    float2 uv = getGlobeUv(position);
    float height = length(position) - bottom_radius;
    WeatherSample weather = sampleWeather(uv, height, mipLevel);
    float density = sampleDensityDetail(weather, position, mipLevel);
    opticalDepth += density * (stepScale - prevStepScale) * stepSize;
    prevStepScale = stepScale;
    stepScale *= 2.0;
  }
  return opticalDepth;
}

float multipleScattering(const float opticalDepth, const float cosTheta) {
  // Multiple scattering approximation
  // See: https://fpsunflower.github.io/ckulla/data/oz_volumes.pdf
  // Attenuation (a), contribution (b) and phase attenuation (c).
  float3 abc = 1.0;
  const float3 attenuation = float3(0.5, 0.5, 0.8); // Should satisfy a <= b
  float scattering = 0.0;
  for (int octave = 0; octave < MULTI_SCATTERING_OCTAVES; ++octave) {
    float beerLambert = exp(-opticalDepth * abc.y);
    // A similar approximation is described in the Frostbite's paper, where
    // phase angle is attenuated.
    scattering += abc.x * beerLambert * phaseFunction(cosTheta, abc.z);
    abc *= attenuation;
  }
  return scattering;
}
float raySphereSecondIntersection(
  const float3 origin,
  const float3 direction,
  const float3 center,
  const float radius
) {
  float3 a = origin - center;
  float b = 2.0 * dot(direction, a);
  float c = dot(a, a) - radius * radius;
  float discriminant = b * b - 4.0 * c;
  return discriminant < 0.0
    ? -1.0
    : (-b + sqrt(discriminant)) * 0.5;
}


float4 marchToClouds(const float3 rayOrigin, const float3 rayDirection, const float maxRayDistance2,
  const float jitter,
  const float3 jitterVector,
  const float rayStartTexelsPerPixel,
  const float3 sunDirection,
  float3 sunIrradiance,
  float3 skyIrradiance,
  out float weightedMeanDepth
) {
  float3 radianceIntegral = 0.0;
  float transmittanceIntegral = 1.0;
  float weightedDistanceSum = 0.0;
  float transmittanceSum = 0.0;

  float stepSize = initialStepSize;
  float rayDistance = stepSize * jitter;
  float cosTheta = dot(sunDirection, rayDirection);

  for (int i = 0; i < maxIterations; ++i) {
    if (rayDistance > maxRayDistance2) {
      break; // Termination
    }
    float3 position = rayOrigin + rayDirection * rayDistance;

    // Sample a rough density.
    float mipLevel = log2(max(1.0, rayStartTexelsPerPixel + rayDistance * 1e-5));
    float height = length(position) - bottom_radius;
    float2 uv = getGlobeUv(position);
    WeatherSample weather = sampleWeather(uv, height, mipLevel);

    if (any(weather.density > minDensity)) {
      // Sample a detailed density.
      float density = sampleDensityDetail(weather, position, mipLevel);
      if (density > minDensity) {
        // #ifdef ACCURATE_ATMOSPHERIC_IRRADIANCE
        sunIrradiance = GetSunAndSkyIrradiance(
          position,
          rayDirection,
          sunDirection,
          skyIrradiance
        );
        // #endif // ACCURATE_ATMOSPHERIC_IRRADIANCE

        // Distance to the top of the bottom layer along the sun direction.
        // This matches the ray origin of BSM.
        float distanceToTop = raySphereSecondIntersection(
          position + ellipsoidCenter,
          sunDirection,
          ellipsoidCenter,
          bottom_radius + maxLayerHeights.x
        );

        // Obtain the optical depth at the position from BSM.
        float3 shadow = 0;//getShadow(position);
        float frontDepth = shadow.x;
        float meanExtinction = shadow.y;
        float maxOpticalDepth = shadow.z;
        float shadowOpticalDepth = min(
          maxOpticalDepth,
          meanExtinction * max(0.0, distanceToTop - frontDepth)
        );

        float sunOpticalDepth = 0.0;
        if (mipLevel < 0.5) {
          sunOpticalDepth = sampleOpticalDepth(position, sunDirection, 2, mipLevel);
        } else {
          sunOpticalDepth = sampleOpticalDepth(position, sunDirection, 1, mipLevel);
        }
        float opticalDepth = sunOpticalDepth + shadowOpticalDepth;
        float scattering = multipleScattering(opticalDepth, cosTheta);
        float3 radiance = (sunIrradiance * scattering + skyIrradiance * skyIrradianceScale) * density;

        // Fudge factor for the irradiance from ground.
        if (mipLevel < 0.5) {
          float groundOpticalDepth = sampleOpticalDepth(
            position,
            -normalize(position),
            2,
            mipLevel
          );
          radiance += radiance * exp(-groundOpticalDepth - (height - minHeight) * 0.01);
        }

        #ifdef USE_POWDER
        radiance *= 1.0 - powderScale * exp(-density * powderExponent);
        #endif // USE_POWDER

        // Energy-conserving analytical integration of scattered light
        // See 5.6.3 in https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf
        float transmittance = exp(-density * stepSize);
        float clampedDensity = max(density, 1e-7);
        float3 scatteringIntegral = (radiance - radiance * transmittance) / clampedDensity;
        radianceIntegral += transmittanceIntegral * scatteringIntegral;
        transmittanceIntegral *= transmittance;

        // Aerial perspective affecting clouds
        // See 5.9.1 in https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf
        weightedDistanceSum += rayDistance * transmittanceIntegral;
        transmittanceSum += transmittanceIntegral;
      }

      // Take a shorter step because we've already hit the clouds.
      stepSize *= 1.005;
      rayDistance += stepSize;
    } else {
      // Otherwise step longer in empty space.
      // TODO: This produces some banding artifacts.
      rayDistance += lerp(stepSize, maxStepSize, min(1.0, mipLevel));
    }

    if (transmittanceIntegral <= minTransmittance) {
      break; // Early termination
    }
  }

  // The final product of 5.9.1 and we'll evaluate this in aerial perspective.
  weightedMeanDepth = transmittanceSum > 0.0 ? weightedDistanceSum / transmittanceSum : 0.0;

  return float4(
    radianceIntegral,
    saturate(remap(transmittanceIntegral, minTransmittance, 1.0, 1.0, 0.0))
  );
}


float raySphereFirstIntersection(
  const float3 origin,
  const float3 direction,
  const float3 center,
  const float radius
) {
  float3 a = origin - center;
  float b = 2.0 * dot(direction, a);
  float c = dot(a, a) - radius * radius;
  float discriminant = b * b - 4.0 * c;
  return discriminant < 0.0
    ? -1.0
    : (-b - sqrt(discriminant)) * 0.5;
}


void raySphereIntersections(
  const float3 origin,
  const float3 direction,
  const float3 center,
  const float radius,
  out float intersection1,
  out float intersection2
) {
  float3 a = origin - center;
  float b = 2.0 * dot(direction, a);
  float c = dot(a, a) - radius * radius;
  float discriminant = b * b - 4.0 * c;
  if (discriminant < 0.0) {
    intersection1 = -1.0;
    intersection2 = -1.0;
    return;
  } else {
    float Q = sqrt(discriminant);
    intersection1 = (-b - Q) * 0.5;
    intersection2 = (-b + Q) * 0.5;
  }
}

void getRayNearFar(const float3 rayDirection, out float rayNear, out float rayFar) {
  bool intersectsGround =
    raySphereFirstIntersection(CamPos, rayDirection, ellipsoidCenter, bottom_radius) >= 0.0;

  if (CamPos.y < minHeight) {
    if (intersectsGround) {
      rayNear = -1.0;
      return;
    }
    rayNear = raySphereSecondIntersection(
      CamPos,
      rayDirection,
      ellipsoidCenter,
      bottom_radius + minHeight
    );
    rayFar = raySphereSecondIntersection(
      CamPos,
      rayDirection,
      ellipsoidCenter,
      bottom_radius + maxHeight
    );
    rayFar = min(rayFar, maxRayDistance);
  } else if (CamPos.y < maxHeight) {
    rayNear = 0.0;
    if (intersectsGround) {
      rayFar = raySphereFirstIntersection(
        CamPos,
        rayDirection,
        ellipsoidCenter,
        bottom_radius + minHeight
      );
    } else {
      rayFar = raySphereSecondIntersection(
        CamPos,
        rayDirection,
        ellipsoidCenter,
        bottom_radius + maxHeight
      );
    }
  } else {
    float intersection1;
    float intersection2;
    raySphereIntersections(
      CamPos,
      rayDirection,
      ellipsoidCenter,
      bottom_radius + maxHeight,
      intersection1,
      intersection2
    );
    rayNear = intersection1;
    if (intersectsGround) {
      rayFar = raySphereFirstIntersection(
        CamPos,
        rayDirection,
        ellipsoidCenter,
        bottom_radius + minHeight
      );
    } else {
      rayFar = intersection2;
    }
  }
}