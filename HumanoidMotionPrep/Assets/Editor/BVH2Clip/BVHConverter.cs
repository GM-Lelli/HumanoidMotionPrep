using System.IO;
using UnityEditor;
using UnityEngine;
using emilianavt.BVHParserLib;

namespace gianmarcolelli.BVH2Clip.Editor
{

    public class BVHConvert
    {
        private string bvhFolderPath;
        private string saveDirectory;
        private bool respectBVHTime;
        private float frameRate;
        private BVHParser bp;

        public BVHConvert(string bvhFolderPath, string saveDirectory, bool respectBVHTime, float frameRate)
        {
            this.bvhFolderPath = bvhFolderPath;
            this.saveDirectory = saveDirectory;
            this.respectBVHTime = respectBVHTime;
            this.frameRate = frameRate;
        }

        public void ConvertBVHToAnimationClip()
        {
            string[] bvhFiles = Directory.GetFiles(bvhFolderPath, "*.bvh");
            if (bvhFiles.Length == 0)
            {
                Debug.LogError("No BVH files found in the selected folder.");
                return;
            }

            foreach (string bvhFilePath in bvhFiles)
            {
                try
                {
                    var clip = ConvertBVH(bvhFilePath, frameRate);
                    SaveClip(clip);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to convert {bvhFilePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Converts a BVH file into an AnimationClip, processing its data to generate animation curves.
        /// </summary>
        /// <param name="bvhFilePath">The file path of the BVH file to be converted.</param>
        /// <returns>
        /// An AnimationClip generated from the BVH file data, with quaternion continuity ensured.
        /// </returns>
        public AnimationClip ConvertBVH(string bvhFilePath, float frameRate)
        {
            // Read the BVH file data as a string
            string bvhData = File.ReadAllText(bvhFilePath);

            // Initialize the BVHParser with or without respecting the frame time specified in the BVH file
            if (respectBVHTime)
            {
                bp = new BVHParser(bvhData);               // Use the frame time from the BVH file
                frameRate = 1f / bp.frameTime;             // Calculate the frame rate
            }
            else
            {
                bp = new BVHParser(bvhData, 1f / frameRate); // Use the provided frame rate
            }

            // Create a new AnimationClip
            string clipName = Path.GetFileNameWithoutExtension(bvhFilePath); // Extract the clip name from the file name
            AnimationClip clip = new AnimationClip
            {
                name = clipName,       // Set the name of the clip
                frameRate = frameRate, // Set frame rate
                legacy = false         // Indicates whether the clip is a legacy animation (set to false for modern workflows)
            };

            // Create a path prefix for the root bone
            string prefix = bp.root.name;

            // Generate animation curves and add them to the AnimationClip
            GetCurves(prefix, bp.root, clip);

            // Ensure quaternion continuity in the animation clip to avoid abrupt transitions
            clip.EnsureQuaternionContinuity();

            // Return the generated AnimationClip
            return clip;
        }

        /// <summary>
        /// Saves the provided AnimationClip to the specified directory in Unity.
        /// </summary>
        /// <param name="clip">The AnimationClip to save.</param>
        /// <param name="folderName">
        /// Optional parameter to specify a subfolder within the save directory.
        /// If null, the default save directory is used.
        /// </param>
        public void SaveClip(AnimationClip clip, string folderName = null)
        {
            // If a folder name is provided, append it to the save directory
            if (folderName != null)
            {
                saveDirectory = Path.Combine(saveDirectory, folderName);
            }

            // Log the save directory and clip name for debugging purposes
            //Debug.Log("Directory where files will be saved: " + saveDirectory);
            //Debug.Log("Clip name: " + clip.name);

#if UNITY_EDITOR
            // Ensure the save directory exists; create it if necessary
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            // Build the full save path for the AnimationClip
            string fullPath = Path.Combine(saveDirectory, clip.name + ".anim");
            fullPath = fullPath.Replace("\\", "/"); // Normalize the path for cross-platform compatibility
                                                    //Debug.Log("Save path: " + fullPath);

            // Save the AnimationClip as an asset in the Unity Editor
            AssetDatabase.CreateAsset(clip, fullPath);
            AssetDatabase.SaveAssets();
            //Debug.Log($"AnimationClip saved to {fullPath}");
#endif
        }


        private void GetCurves(string path, BVHParser.BVHBone node, AnimationClip clip)
        {
            // Implementazione derivata da getCurves
            bool posX = false, posY = false, posZ = false, rotX = false, rotY = false, rotZ = false;

            float[][] values = new float[6][];
            Keyframe[][] keyframes = new Keyframe[7][];
            string[] props = new string[7];

            for (int channel = 0; channel < 6; channel++)
            {
                if (!node.channels[channel].enabled)
                {
                    continue;
                }

                switch (channel)
                {
                    case 0: posX = true; props[channel] = "localPosition.x"; break;
                    case 1: posY = true; props[channel] = "localPosition.y"; break;
                    case 2: posZ = true; props[channel] = "localPosition.z"; break;
                    case 3: rotX = true; props[channel] = "localRotation.x"; break;
                    case 4: rotY = true; props[channel] = "localRotation.y"; break;
                    case 5: rotZ = true; props[channel] = "localRotation.z"; break;
                }

                keyframes[channel] = new Keyframe[bp.frames];
                values[channel] = node.channels[channel].values;

                if (rotX && rotY && rotZ && keyframes[6] == null)
                {
                    keyframes[6] = new Keyframe[bp.frames];
                    props[6] = "localRotation.w";
                }
            }

            // Crea le curve per posizione e rotazione
            float time = 0f;
            for (int i = 0; i < bp.frames; i++)
            {
                time += 1f / frameRate;

                if (posX && posY && posZ)
                {
                    keyframes[0][i] = new Keyframe(time, -values[0][i]);
                    keyframes[1][i] = new Keyframe(time, values[1][i]);
                    keyframes[2][i] = new Keyframe(time, values[2][i]);
                }

                if (rotX && rotY && rotZ)
                {
                    Vector3 eulerBVH = new Vector3(values[3][i], values[4][i], values[5][i]);
                    Quaternion rot = fromEulerZXY(eulerBVH);
                    keyframes[3][i] = new Keyframe(time, rot.x);
                    keyframes[4][i] = new Keyframe(time, -rot.y);
                    keyframes[5][i] = new Keyframe(time, -rot.z);
                    keyframes[6][i] = new Keyframe(time, rot.w);
                }
            }

            // Aggiunge le curve all'AnimationClip
            if (posX && posY && posZ)
            {
                clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
                clip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
                clip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
            }

            if (rotX && rotY && rotZ)
            {
                clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3]));
                clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
                clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
                clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));
            }

            // Gestione dei figli
            foreach (BVHParser.BVHBone child in node.children)
            {
                GetCurves(path + "/" + child.name, child, clip);
            }
        }

        private Quaternion fromEulerZXY(Vector3 euler)
        {
            return Quaternion.AngleAxis(euler.z, Vector3.forward) * Quaternion.AngleAxis(euler.x, Vector3.right) * Quaternion.AngleAxis(euler.y, Vector3.up);
        }

        public float GetFrameRate()
        {
            return frameRate;
        }
    }
}