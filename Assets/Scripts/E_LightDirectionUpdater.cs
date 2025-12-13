using UnityEngine;

[ExecuteAlways]
public class E_LightDirectionUpdater : MonoBehaviour
{
    public Material material;
    public Light lightSource;

    private void Update()
    {
        if (material != null && lightSource != null)
        {
            Vector3 lightDirection = lightSource.transform.forward;
            material.SetVector("_LightDir", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0));
        }
    }
}
