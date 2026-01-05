using UnityEngine;
using UnityEditor;

// BoxCollider2D のインスペクタを拡張する
[CustomEditor(typeof(BoxCollider2D))]
[CanEditMultipleObjects] // 複数選択時にも対応
public class CustomBoxCollider2DEditor : Editor
{
    // 元のBoxCollider2Dのインスペクタを保持するための変数
    private Editor defaultEditor;

    void OnEnable()
    {
        // BoxCollider2Dの標準エディタを作成
        // 引数の "UnityEditor.BoxCollider2DEditor" は内部的な型名
        defaultEditor = Editor.CreateEditor(targets, System.Type.GetType("UnityEditor.BoxCollider2DEditor, UnityEditor"));
    }

    void OnDisable()
    {
        DestroyImmediate(defaultEditor);
    }

    public override void OnInspectorGUI()
    {
        // 1. 標準のインスペクタ（SizeやOffsetなど）を表示
        if (defaultEditor != null)
        {
            defaultEditor.OnInspectorGUI();
        }

        // 2. 隙間を空けてボタンを表示
        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(1f, 1f, 1f); // 薄い緑色
        if (GUILayout.Button("Fit to Sprite Size", GUILayout.Height(25)))
        {
            // 選択されているすべてのBoxCollider2Dに対して処理を行う
            foreach (var obj in targets)
            {
                Fit((BoxCollider2D)obj);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void Fit(BoxCollider2D col)
    {
        SpriteRenderer sr = col.GetComponent<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
        {
            // Undo（元に戻す）に登録
            Undo.RecordObject(col, "Fit BoxCollider2D to Sprite");

            // サイズとオフセットを更新
            col.size = sr.sprite.bounds.size;
            col.offset = sr.sprite.bounds.center;

            // 変更を確定
            EditorUtility.SetDirty(col);
        }
        else
        {
            Debug.LogWarning($"{col.gameObject.name}: SpriteRenderer または Sprite が見つかりません。");
        }
    }
}