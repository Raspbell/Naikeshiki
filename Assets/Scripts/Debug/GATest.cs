using UnityEngine;

public class GATest : MonoBehaviour
{
    private GoogleAnalyticsManager _manager;

    void Start()
    {
        _manager = FindFirstObjectByType<GoogleAnalyticsManager>();
    }

    void Update()
    {
        // スペースキーを押したらテスト送信
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_manager != null)
            {
                Debug.Log("--- テスト送信開始 ---");
                _manager.SendScreenView("Test_Connection_Check");
            }
            else
            {
                Debug.LogError("GoogleAnalyticsManagerが見つかりません！");
            }
        }
    }
}