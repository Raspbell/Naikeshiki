using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 各ステージにアタッチされ、自動的に計測を行うクラス
/// </summary>
public class PuzzleAnalyticsTracker : MonoBehaviour
{
    private GoogleAnalyticsManager _analyticsManager;

    private string _regionName;
    private string _stageName;
    private float _stageStartTime;
    private float _lastPieceTime;

    // 地域ごとの累積時間を保存するためのキープレフィックス
    private const string RegionTotalTimeKeyPrefix = "Analytics_RegionTotal_";

    private void Awake()
    {
        // シーン名から名前を自動生成
        string sceneName = SceneManager.GetActiveScene().name;
        _stageName = sceneName;

        if (sceneName.Contains("_"))
        {
            string[] parts = sceneName.Split('_');
            _regionName = parts[0];
            _stageName = parts[1];
        }
        else
        {
            _regionName = "UnknownRegion";
        }
    }

    private void Start()
    {
        _analyticsManager = FindFirstObjectByType<GoogleAnalyticsManager>();

        if (_analyticsManager == null)
        {
            return;
        }

        // 開始ログを送信
        // _analyticsManager.SendScreenView($"{_regionName}_{_stageName}");

        _stageStartTime = Time.time;
        _lastPieceTime = Time.time;
    }

    public void TrackPieceSolved(string pieceName)
    {
        if (_analyticsManager == null)
        {
            return;
        }

        float now = Time.time;
        // 直前のピースをはめてからの経過時間（そのピースにかかった時間）
        float duration = now - _lastPieceTime;
        _lastPieceTime = now;
        _analyticsManager.SendPieceSolved(_regionName, _stageName, pieceName, duration);
    }

    public void TrackStageCleared()
    {
        if (_analyticsManager == null)
        {
            return;
        }

        // ステージ開始からの総経過時間
        float totalTime = Time.time - _stageStartTime;
        _analyticsManager.SendStageCleared(_regionName, _stageName, totalTime);

        // 地域ごとの累積時間を更新（ステージクリア時に加算して保存）
        // これにより、その地域のステージ1から順にプレイした場合の合計時間が蓄積される
        string key = RegionTotalTimeKeyPrefix + _regionName;
        float currentTotal = PlayerPrefs.GetFloat(key, 0f);
        PlayerPrefs.SetFloat(key, currentTotal + totalTime);
        PlayerPrefs.Save();
    }

    public void TrackRegionCleared()
    {
        if (_analyticsManager == null)
        {
            return;
        }

        string key = RegionTotalTimeKeyPrefix + _regionName;
        float duration = PlayerPrefs.GetFloat(key, 0f);

        if (duration <= 0f)
        {
            duration = Time.time - _stageStartTime;
        }

        _analyticsManager.SendRegionCleared(_regionName, duration);

        // 地域クリア後は累積時間をリセット（次の周回プレイ等のため）
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}