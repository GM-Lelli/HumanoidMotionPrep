using UnityEditor;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using gianmarcolelli.BVH2Clip.Editor;

namespace gianmarcolelli.MoMaskUnityBridge.Editor
{
    public class RunPythonCommandEditor : EditorWindow
    {
        private string textPrompt = "A person is running on a treadmill.";
        private string extension = "exp1";
        private string gpuId = "0";
        private string saveDirectory = "Assets/Animations";

        [MenuItem("Tools/MoMask")]
        public static void ShowWindow()
        {
            GetWindow<RunPythonCommandEditor>("MoMask");
        }

        void OnGUI()
        {
            GUILayout.Label("Generate Animation via Python", EditorStyles.boldLabel);

            textPrompt = EditorGUILayout.TextField("Text Prompt", textPrompt);
            extension = EditorGUILayout.TextField("Extension", extension);
            gpuId = EditorGUILayout.TextField("GPU ID", gpuId);
            saveDirectory = EditorGUILayout.TextField("Save Directory", saveDirectory);

            if (GUILayout.Button("Generate Animation"))
            {
                RunPythonScript();
            }
        }

        private void RunPythonScript()
        {
            string pythonPath = "/home/gianmarco/Apps/anaconda3/envs/gm_thesis_4/bin/python";
            string scriptName = "gen_t2m.py";
            string workingDir = Path.GetFullPath("../dependencies/momask-codes-main");

            var commandRunner = new PythonCommandRunner(pythonPath, scriptName, workingDir);

            if (!commandRunner.ValidateWorkingDirectory())
            {
                Debug.LogError("Invalid working directory: " + workingDir);
                return;
            }

            string command = commandRunner.BuildCommand(gpuId, extension, textPrompt);

            try
            {
                string output = commandRunner.ExecuteCommand(command);
                Debug.Log("Python script executed successfully. Output:\n" + output);

                string bvhFilePath = commandRunner.GetSpecificFile(extension, 2);
                Debug.Log("Target file found: " + bvhFilePath);

                BVHConvert converter = new BVHConvert(bvhFilePath, saveDirectory, false, 30.0f);
                var clip = converter.ConvertBVH(bvhFilePath, converter.GetFrameRate());

                Debug.Log("Momask file converted: " + clip.name);
                converter.SaveClip(clip, extension);
                Debug.Log("AnimationClip saved successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("An error occurred while running the Python script:\n" + ex.Message);
            }
        }
    }
}
#endif