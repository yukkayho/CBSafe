using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Core.UI;

internal static class CustomOverlayPresenter
{
	internal static void Configure(GameObject overlay, Transform panel)
	{
		if (!(overlay == null))
		{
			RectTransform rectTransform = overlay.GetComponent<RectTransform>() ?? overlay.AddComponent<RectTransform>();
			RectTransform rectTransform2 = ((panel == null) ? null : panel.GetComponent<RectTransform>());
			if (rectTransform2 != null)
			{
				rectTransform.anchorMin = rectTransform2.anchorMin;
				rectTransform.anchorMax = rectTransform2.anchorMax;
				rectTransform.pivot = rectTransform2.pivot;
				rectTransform.anchoredPosition = rectTransform2.anchoredPosition;
				rectTransform.sizeDelta = rectTransform2.sizeDelta;
				rectTransform.localScale = rectTransform2.localScale;
				rectTransform.localRotation = rectTransform2.localRotation;
			}
			else
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
				rectTransform.localScale = Vector3.one;
				rectTransform.localRotation = Quaternion.identity;
			}
			CanvasGroup obj = overlay.GetComponent<CanvasGroup>() ?? overlay.AddComponent<CanvasGroup>();
			obj.alpha = 1f;
			obj.interactable = true;
			obj.blocksRaycasts = true;
		}
	}

	internal static void Show(GameObject overlay)
	{
		if (overlay == null)
		{
			return;
		}
		Transform parent = overlay.transform.parent;
		if (parent != null)
		{
			parent.gameObject.SetActive(value: true);
		}
		overlay.transform.SetAsLastSibling();
		overlay.SetActive(value: true);
		foreach (CanvasGroup componentsInChild in overlay.GetComponentsInChildren<CanvasGroup>(includeInactive: true))
		{
			if (!(componentsInChild == null))
			{
				componentsInChild.alpha = 1f;
				componentsInChild.interactable = true;
				componentsInChild.blocksRaycasts = true;
			}
		}
		Canvas.ForceUpdateCanvases();
		RectTransform component = overlay.GetComponent<RectTransform>();
		if (component != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(component);
		}
		Canvas.ForceUpdateCanvases();
	}
}
