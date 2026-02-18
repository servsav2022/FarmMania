using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayUIClick();
    }
}