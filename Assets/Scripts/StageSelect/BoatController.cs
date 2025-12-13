using UnityEngine;
using DG.Tweening;
using System.Collections; // ← 追加: IEnumeratorを使うために必要

public class BoatController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float moveDuration = 5f;
    [SerializeField] private float baseY = 0f;
    [SerializeField] private float bounceAmplitude = 0.5f;
    [SerializeField] private float frequency = 1f;

    [SerializeField] private GameObject sail;
    [SerializeField] private float sailAngle = 45f;

    IEnumerator Start()
    {
        yield return null;

        int rotationDir = Random.Range(0, 2) == 0 ? 1 : -1;
        sail.transform.localRotation = Quaternion.Euler(0f, sailAngle * rotationDir, 0f);

        Sequence sequence = DOTween.Sequence();

        var distance = moveSpeed * moveDuration;
        var target = transform.position - transform.forward * distance;
        sequence.Append(transform.DOMove(target, moveDuration)
            .SetEase(Ease.Linear));

        if (frequency <= 0f) frequency = 1f;
        float period = 1f / frequency;
        int loops = Mathf.CeilToInt(moveDuration / period);

        sequence.Join(DOVirtual.Float(0f, Mathf.PI * 2f, period, v =>
        {
            var p = transform.position;
            p.y = baseY + Mathf.Sin(v) * bounceAmplitude;
            transform.position = p;
        }).SetEase(Ease.Linear).SetLoops(loops, LoopType.Restart));

        sequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}