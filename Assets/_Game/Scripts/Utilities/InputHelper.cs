using UnityEngine;
using UnityEngine.EventSystems;

public static class InputHelper
{
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

#if UNITY_EDITOR || UNITY_STANDALONE

        return EventSystem.current.IsPointerOverGameObject();
#else

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
#endif
    }
}
