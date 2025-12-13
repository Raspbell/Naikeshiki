using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SceneTransition))]
public class FieldTestEditor : Editor
{
    // Bar_Slide, Bar_Flip, Tile_Slide, Tile_Flip
    private SerializedProperty sceneTransitionStartIntervalProperty;
    private SerializedProperty sceneTransitionSpeedProperty;

    // Tile_Rotate
    private SerializedProperty sceneTransitionRadianProperty;

    // Sprite
    private SerializedProperty sceneTransitionSpriteProperty;
    private SerializedProperty sceneTransitionSpriteColorProperty;
    private SerializedProperty sceneTransitionMaxScaleProperty;
    private SerializedProperty sceneTransitionSpriteSpeedProperty;

    private void OnEnable()
    {
        // Bar_Slide, Bar_Flip, Tile_Slide, Tile_Flip
        sceneTransitionStartIntervalProperty = serializedObject.FindProperty("sceneTransitionStartInterval");
        sceneTransitionSpeedProperty = serializedObject.FindProperty("sceneTransitionSpeed");

        // Tile_Rotate
        sceneTransitionRadianProperty = serializedObject.FindProperty("sceneTransitionRadian");

        // Sprite
        sceneTransitionSpriteProperty = serializedObject.FindProperty("sceneTransitionSprite");
        sceneTransitionSpriteColorProperty = serializedObject.FindProperty("sceneTransitionSpriteColor");
        sceneTransitionMaxScaleProperty = serializedObject.FindProperty("sceneTransitionMaxScale");
        sceneTransitionSpriteSpeedProperty = serializedObject.FindProperty("sceneTransitionSpriteSpeed");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        GUILayout.Space(10);
        SceneTransition instance = target as SceneTransition;

        switch (instance.transitionType) {
            case SceneTransition.TransitionType.Bar_Slide or SceneTransition.TransitionType.Bar_Flip or SceneTransition.TransitionType.Tile_Slide or SceneTransition.TransitionType.Tile_Flip:
                EditorGUILayout.PropertyField(sceneTransitionStartIntervalProperty, new GUIContent("sceneTransitionStartInterval"));
                EditorGUILayout.PropertyField(sceneTransitionSpeedProperty, new GUIContent("sceneTransitionSpeed"));
                break;

            case SceneTransition.TransitionType.Tile_Rotate:
                EditorGUILayout.PropertyField(sceneTransitionStartIntervalProperty, new GUIContent("sceneTransitionStartInterval"));
                EditorGUILayout.PropertyField(sceneTransitionSpeedProperty, new GUIContent("sceneTransitionSpeed"));
                EditorGUILayout.PropertyField(sceneTransitionRadianProperty, new GUIContent("sceneTransitionRadian"));
                break;

            case SceneTransition.TransitionType.Sprite:
                EditorGUILayout.PropertyField(sceneTransitionSpriteProperty, new GUIContent("sceneTransitionSprite"));
                EditorGUILayout.PropertyField(sceneTransitionSpriteColorProperty, new GUIContent("sceneTransitionSpriteColor"));
                EditorGUILayout.PropertyField(sceneTransitionMaxScaleProperty, new GUIContent("sceneTransitionMaxScale"));
                EditorGUILayout.PropertyField(sceneTransitionSpriteSpeedProperty, new GUIContent("sceneTransitionSpriteSpeed"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
