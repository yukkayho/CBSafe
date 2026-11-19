using UnityEngine;

namespace CBSafe.Patches;

internal class CommandTicker : MonoBehaviour
{
	private void Update()
	{
		try
		{
			DlcUnlockCommand.Register();
			BoosterProof.Register();
		}
		catch
		{
		}
	}
}
