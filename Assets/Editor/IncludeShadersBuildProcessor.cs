using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public class IncludeShadersBuildProcessor : IPreprocessBuildWithReport
{
    // InitializeOnLoad sayesinde editör açıldığında veya kod derlendiğinde anında çalışır.
    static IncludeShadersBuildProcessor()
    {
        FixShaders();
    }

    // Build tuşuna basıldığında (APK çıkarılırken) da emin olmak için çalışır.
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        FixShaders();
    }

    [MenuItem("Tools/Fix Shaders For Build")]
    public static void FixShaders()
    {
        string[] requiredShaders = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Standard",
            "Sprites/Default",
            "UI/Default",
            "Unlit/Transparent",
            "Unlit/Color",
            "Hidden/Universal Render Pipeline/Lit",
            "Hidden/Universal Render Pipeline/FallbackError"
        };

        var graphicsSettingsObj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (graphicsSettingsObj == null || graphicsSettingsObj.Length == 0) return;

        SerializedObject graphicsSettings = new SerializedObject(graphicsSettingsObj[0]);
        SerializedProperty alwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

        bool modified = false;

        foreach (string shaderName in requiredShaders)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) continue;

            bool exists = false;
            for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
            {
                if (alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                alwaysIncludedShaders.arraySize++;
                alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1).objectReferenceValue = shader;
                modified = true;
            }
        }

        if (modified)
        {
            graphicsSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[IncludeShadersBuildProcessor] Required runtime shaders successfully added to Always Included Shaders list!");
        }
    }
}
