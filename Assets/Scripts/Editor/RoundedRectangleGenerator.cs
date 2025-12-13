using UnityEngine;
using UnityEditor;
using System.IO;

namespace Malen.EditorTools
{
    public class RoundedRectangleGenerator : EditorWindow
    {
        // --- 設定項目 ---
        private int width = 512;
        private int height = 512;
        private float outerRadius = 85f; // 外側の半径

        // フレーム設定
        private bool isFrame = true;
        private float borderThickness = 40f;

        // 内側の角丸設定（追加機能）
        private bool separateInnerRadius = false; // 内側の半径を個別に設定するか
        private float innerRadiusOverride = 20f;  // 個別設定時の内側半径

        private Color fillColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        private Texture2D previewTexture;

        [MenuItem("Tools/Rounded Rectangle Generator")]
        public static void ShowWindow()
        {
            GetWindow<RoundedRectangleGenerator>("Rounded Rectangle Generator");
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            width = EditorGUILayout.IntField("Width (px)", width);
            height = EditorGUILayout.IntField("Height (px)", height);

            float minSide = Mathf.Min(width, height);
            float maxOuterRadius = minSide / 2f;

            // 外側の半径
            outerRadius = EditorGUILayout.Slider("Corner Radius", outerRadius, 0, maxOuterRadius);

            EditorGUILayout.Space(5);

            // フレーム設定
            isFrame = EditorGUILayout.Toggle("Is Frame (Border)", isFrame);
            if (isFrame)
            {
                EditorGUI.indentLevel++;
                float maxThickness = minSide / 2f;
                borderThickness = EditorGUILayout.Slider("Thickness", borderThickness, 1f, maxThickness);

                // 内側の半径を個別設定するか
                separateInnerRadius = EditorGUILayout.Toggle("Separate Inner Radius", separateInnerRadius);

                if (separateInnerRadius)
                {
                    // 内側のサイズに基づいた半径制限
                    float innerMinSide = Mathf.Min(Mathf.Max(0, width - borderThickness * 2), Mathf.Max(0, height - borderThickness * 2));
                    float maxInnerRadius = innerMinSide / 2f;
                    innerRadiusOverride = EditorGUILayout.Slider("Inner Radius", innerRadiusOverride, 0, maxInnerRadius);
                }
                else
                {
                    // 自動計算値の表示（読み取り専用）
                    float autoInnerRadius = Mathf.Max(0, outerRadius - borderThickness);
                    EditorGUILayout.LabelField("Auto Inner Radius", autoInnerRadius.ToString("F1"));
                    if (autoInnerRadius <= 0)
                    {
                        EditorGUILayout.HelpBox("Thickness exceeds Radius.\nInner corners will be sharp.", MessageType.Info);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            fillColor = EditorGUILayout.ColorField("Color", fillColor);

            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreview();
            }
            GUILayout.EndVertical();

            // --- プレビュー表示 (Scale to Fit) ---
            DrawPreviewArea();

            EditorGUILayout.Space();

            // --- 出力ボタン ---
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Export to PNG", GUILayout.Height(40)))
            {
                SaveTexture();
            }
            GUI.backgroundColor = Color.white;
        }

        // SDF計算関数（外側・内側共通）
        // p: 中心からの座標, size: 矩形のサイズ(w,h), radius: 角丸半径
        private float GetSDF(Vector2 p, Vector2 size, float radius)
        {
            // 半径はサイズからはみ出さないよう制限（計算上の破綻防止）
            float r = Mathf.Min(radius, Mathf.Min(size.x, size.y) / 2f);

            Vector2 halfSize = size / 2f - new Vector2(r, r);
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize;

            float distOutside = Vector2.Max(q, Vector2.zero).magnitude;
            float distInside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);

            return distOutside + distInside - r;
        }

        private void UpdatePreview()
        {
            if (previewTexture != null) DestroyImmediate(previewTexture);

            previewTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            previewTexture.filterMode = FilterMode.Bilinear;
            previewTexture.alphaIsTransparency = true;

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2(width / 2f, height / 2f);

            // 外側の矩形サイズ
            Vector2 outerSize = new Vector2(width, height);

            // 内側の矩形サイズ（くり抜き用）
            float innerW = Mathf.Max(0, width - borderThickness * 2);
            float innerH = Mathf.Max(0, height - borderThickness * 2);
            Vector2 innerSize = new Vector2(innerW, innerH);

            // 内側の半径決定ロジック
            float effectiveInnerRadius;
            if (separateInnerRadius)
            {
                effectiveInnerRadius = innerRadiusOverride;
            }
            else
            {
                // デフォルト：太さ分だけ半径を縮小（0以下なら直角になる）
                effectiveInnerRadius = Mathf.Max(0, outerRadius - borderThickness);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // ピクセル中心
                    Vector2 p = new Vector2(x + 0.5f - center.x, y + 0.5f - center.y);

                    // 1. 外側の形状計算
                    float distOuter = GetSDF(p, outerSize, outerRadius);
                    float alphaOuter = 1.0f - Mathf.Clamp01(distOuter + 0.5f); // 外側のアンチエイリアス

                    float finalAlpha = alphaOuter;

                    // 2. 内側のくり抜き計算
                    if (isFrame && innerW > 0 && innerH > 0)
                    {
                        float distInner = GetSDF(p, innerSize, effectiveInnerRadius);

                        // 内側の形状（穴）のアルファ値（穴の中なら1、外なら0）
                        float alphaHole = 1.0f - Mathf.Clamp01(distInner + 0.5f);

                        // 形状から穴を引く（穴の部分を透明にする）
                        finalAlpha = alphaOuter * (1.0f - alphaHole);
                    }

                    pixels[y * width + x] = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * finalAlpha);
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
        }

        private void DrawPreviewArea()
        {
            GUILayout.Label("Preview (Scale to Fit)", EditorStyles.boldLabel);
            Rect availableRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (previewTexture != null)
            {
                float targetAspect = (float)width / height;
                float windowAspect = availableRect.width / availableRect.height;
                Rect drawRect = availableRect;

                if (windowAspect > targetAspect)
                {
                    float fitWidth = availableRect.height * targetAspect;
                    drawRect.width = fitWidth;
                    drawRect.x += (availableRect.width - fitWidth) / 2f;
                }
                else
                {
                    float fitHeight = availableRect.width / targetAspect;
                    drawRect.height = fitHeight;
                    drawRect.y += (availableRect.height - fitHeight) / 2f;
                }

                EditorGUI.DrawTextureTransparent(drawRect, previewTexture, ScaleMode.StretchToFill);
                GUI.Label(availableRect, $"Size: {width}x{height}", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                GUILayout.BeginArea(availableRect);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Generate Preview")) UpdatePreview();
                GUILayout.FlexibleSpace();
                GUILayout.EndArea();
            }
        }

        private void SaveTexture()
        {
            if (previewTexture == null) UpdatePreview();
            string path = EditorUtility.SaveFilePanel("Save Texture", Application.dataPath, "RoundedFrame", "png");
            if (!string.IsNullOrEmpty(path))
            {
                byte[] pngData = previewTexture.EncodeToPNG();
                if (pngData != null)
                {
                    File.WriteAllBytes(path, pngData);
                    AssetDatabase.Refresh();
                    Debug.Log("Saved to: " + path);
                }
            }
        }

        private void OnDestroy() { if (previewTexture != null) DestroyImmediate(previewTexture); }
    }
}