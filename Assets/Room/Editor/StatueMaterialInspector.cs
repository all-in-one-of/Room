using UnityEngine;
using UnityEditor;

namespace Room
{
    public class StatueMaterialInspector : ShaderGUI
    {
        static class Labels
        {
            public static GUIContent albedoMap = new GUIContent("Albedo Map");
            public static GUIContent normalMap = new GUIContent("Normal Map");
            public static GUIContent occlusionMap = new GUIContent("Occlusion Map");
            public static GUIContent curvatureMap = new GUIContent("Curvature Map");
        }

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            // Base maps
            EditorGUILayout.LabelField("Base Maps", EditorStyles.boldLabel);

            editor.TexturePropertySingleLine(
                Labels.normalMap, FindProperty("_NormalMap", props)
            );

            editor.TexturePropertySingleLine(
                Labels.occlusionMap,
                FindProperty("_OcclusionMap", props),
                FindProperty("_OcclusionMapStrength", props)
            );

            editor.TexturePropertySingleLine(
                Labels.curvatureMap, FindProperty("_CurvatureMap", props)
            );

            EditorGUILayout.Space();

            // Channel 1
            EditorGUILayout.LabelField("Channel 1", EditorStyles.boldLabel);
            editor.ShaderProperty(FindProperty("_Color1", props), "Color");
            editor.ShaderProperty(FindProperty("_Metallic1", props), "Metallic");
            editor.ShaderProperty(FindProperty("_Smoothness1", props), "Smoothness");

            EditorGUILayout.Space();

            // Channel 2
            EditorGUILayout.LabelField("Channel 2", EditorStyles.boldLabel);
            editor.ShaderProperty(FindProperty("_Color2", props), "Color");
            editor.ShaderProperty(FindProperty("_Metallic2", props), "Metallic");
            editor.ShaderProperty(FindProperty("_Smoothness2", props), "Smoothness");

            EditorGUILayout.Space();

            // Backface
            EditorGUILayout.LabelField("Back face", EditorStyles.boldLabel);
            editor.ShaderProperty(FindProperty("_Color3", props), "Color");
            editor.ShaderProperty(FindProperty("_Metallic3", props), "Metallic");
            editor.ShaderProperty(FindProperty("_Smoothness3", props), "Smoothness");

            EditorGUILayout.Space();

            // Detail maps
            EditorGUILayout.LabelField("Detail Maps", EditorStyles.boldLabel);

            editor.TexturePropertySingleLine(
                Labels.albedoMap, FindProperty("_DetailAlbedoMap", props)
            );

            editor.TexturePropertySingleLine(
                Labels.normalMap,
                FindProperty("_DetailNormalMap", props),
                FindProperty("_DetailNormalMapScale", props)
            );

            editor.ShaderProperty(FindProperty("_DetailMapScale", props), "Scale");
        }
    }
}
