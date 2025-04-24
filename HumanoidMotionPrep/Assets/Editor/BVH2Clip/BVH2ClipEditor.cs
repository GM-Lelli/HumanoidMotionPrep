#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_EDITOR
public class BVH2ClipEditor : EditorWindow
{
    private string bvhFolderPath = "";
    private string saveDirectory = "Assets/Animations";
    private bool respectBVHTime = true;
    private float frameRate = 20.0f;

    [MenuItem("Tools/BVH to AnimationClip")]
    public static void ShowWindow()
    {
        GetWindow<BVH2ClipEditor>("BVH to AnimationClip");
    }

    private void OnEnable()
    {
        try
        {
            // Tenta di usare il percorso esistente
            saveDirectory = saveDirectory.Replace("\\", "/");
        }
        catch (Exception ex)
        {
            // Log dell'errore e inizializzazione del percorso di default
            Debug.LogWarning("Percorso di salvataggio non valido. Verrà utilizzato il percorso predefinito. Dettagli: " + ex.Message);
            saveDirectory = Path.Combine(Application.dataPath, "Animations").Replace("\\", "/");
        }

        // Crea la directory se non esiste già
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        Debug.Log("Percorso di salvataggio inizializzato e normalizzato a: " + saveDirectory);
    }

    private void OnGUI()
    {
        GUILayout.Label("BVH to AnimationClip Converter", EditorStyles.boldLabel);

        GUIContent folderButtonContent = new GUIContent("Select BVH Folder", "Select a folder containing BVH files to convert into AnimationClips.");
        // Seleziona la cartella BVH
        if (GUILayout.Button(folderButtonContent))
        {
            bvhFolderPath = EditorUtility.OpenFolderPanel("Select BVH Folder", "", "");
        }
        GUILayout.TextField(bvhFolderPath);

        // Aggiungi uno spazio per una migliore separazione degli elementi
        GUILayout.Space(10);

        // Visualizza la cartella di output predefinita (non modificabile)
        GUILayout.Label($"Output Folder Path (Predefined): {saveDirectory}", EditorStyles.label);

        // Aggiungi uno spazio per una migliore separazione degli elementi
        GUILayout.Space(10);

        // Contenuto del toggle per Respect BVH Frame Time con tooltip
        GUIContent respectBVHTimeToggleContent = new GUIContent("Respect BVH Frame Time", "If enabled, the frame time from the BVH file will be used, otherwise, you can override it with a custom frame rate.");
        respectBVHTime = GUILayout.Toggle(respectBVHTime, respectBVHTimeToggleContent);
        if (!respectBVHTime)
        {
            GUILayout.Label("Override Frame Rate", EditorStyles.label);
            frameRate = EditorGUILayout.FloatField(frameRate);
        }

        // Aggiungi uno spazio per una migliore separazione degli elementi
        GUILayout.Space(10);

        if (GUILayout.Button("Convert BVH to AnimationClip"))
        {
            // Debug della directory di salvataggio
            Debug.Log("Directory dove salvare i file: " + saveDirectory);

            // Controllo della cartella BVH
            if (string.IsNullOrEmpty(bvhFolderPath))
            {
                Debug.LogError("La cartella BVH non è selezionata.");
                return;
            }
            else if (!Directory.Exists(bvhFolderPath))
            {
                Debug.LogError("La cartella BVH non esiste.");
            }

            BVHConvert converter = new BVHConvert(bvhFolderPath, saveDirectory, respectBVHTime, frameRate);
            converter.ConvertBVHToAnimationClip();
        }
    }
}
#endif