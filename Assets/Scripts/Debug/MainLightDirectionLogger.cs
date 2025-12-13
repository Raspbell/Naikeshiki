using UnityEngine;

public class MainLightDirectionLogger : MonoBehaviour
{
    // 毎フレーム、メインライト方向を出力する
    void Update()
    {
        // Sun Source に指定された Directional Light を取得
        var sun = RenderSettings.sun;

        // 光の進行方向（ShaderGraphの Main Light Direction と対応）
        Vector3 lightDir = -sun.transform.forward;

        // 必要に応じてこちらに切替（環境によって符号が逆に見える場合）
        // Vector3 lightDir = sun.transform.forward;

        Debug.Log($"MainLightDirection (world) = {lightDir}");
    }
}
