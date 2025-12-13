using UnityEngine;

public static class TransformExtensions
{
    // 名前で検索
    public static Transform FindChildObjectByName(this Transform parentTransform, string objectName)
    {
        if (parentTransform == null)
        {
            return null;
        }

        foreach (Transform child in parentTransform)
        {
            if (child.name == objectName)
            {
                return child;
            }

            Transform found = child.FindChildObjectByName(objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    // タグで検索
    public static Transform FindChildObjectByTag(this Transform parentTransform, string tagName)
    {
        if (parentTransform == null)
        {
            return null;
        }

        foreach (Transform child in parentTransform)
        {
            if (child.CompareTag(tagName))
            {
                return child;
            }

            Transform found = child.FindChildObjectByTag(tagName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
