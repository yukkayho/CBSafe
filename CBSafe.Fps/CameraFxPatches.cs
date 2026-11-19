using UnityEngine;

namespace CBSafe.Fps;

internal static class CameraFxPatches
{
	public static bool BloomBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || !OptState.DisableBloom)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Bloom);
		Graphics.Blit(source, destination);
		return false;
	}

	public static bool MotionBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || !OptState.DisableMotionBlur)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.MotionBlur);
		Graphics.Blit(source, destination);
		return false;
	}

	public static bool AaBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || (!OptState.DisableAa && !OptState.CheapCamera))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Aa);
		Graphics.Blit(source, destination);
		return false;
	}

	public static bool ExplosionBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || !OptState.DisableExplosionBloom)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.ExplosionBloom);
		Graphics.Blit(source, destination);
		return false;
	}

	public static bool PlanarRender()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Planar, disable: true);
	}

	public static bool ParticleIkUpdate()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.ParticleIk, OptState.DisableParticleIk);
	}

	public static bool BeamRender()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.VolBeams, OptState.DisableVolumetricBeams);
	}

	public static bool BeamDust()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.VolBeams, OptState.DisableVolumetricBeams);
	}

	public static bool OutlineBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || !OptState.DisableHeavyPostProcess)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.PostProcess);
		Graphics.Blit(source, destination);
		return false;
	}

	public static bool HeatBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || !OptState.DisableHeatHaze)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.HeatHaze);
		Graphics.Blit(source, destination);
		return false;
	}

	public static bool LinearFogBlit(RenderTexture source, RenderTexture destination)
	{
		if (OptDark.Night || !OptState.DisableLinearFog)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.LinearFog);
		Graphics.Blit(source, destination);
		return false;
	}
}
