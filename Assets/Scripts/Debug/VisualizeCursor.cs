using UnityEngine;

public class VisualizeCursor : MonoBehaviour
{
    [SerializeField] private int cursorSize = 32;

    void Start()
    {
#if UNITY_EDITOR
        Cursor.SetCursor(UnityEditor.PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
#endif
    }
}
