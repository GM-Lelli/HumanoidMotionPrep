#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace gianmarcolelli.BVHTools
{
    // Struttura per contenere le curve di posizione e rotazione
    public class JointCurves
    {
        public AnimationCurve positionX;
        public AnimationCurve positionY;
        public AnimationCurve positionZ;
        public AnimationCurve rotationX;
        public AnimationCurve rotationY;
        public AnimationCurve rotationZ;
        public AnimationCurve rotationW;
    }

    public static class AnimationClipDataExtractor
    {
        private static bool debug = false;

        /// <summary>
        /// Estrae i dati di posizione e rotazione per ogni giunto di un AnimationClip, suddivisi per frame.
        /// </summary>
        /// <param name="clip">L'AnimationClip da cui estrarre i dati di animazione.</param>
        /// <returns>Un dizionario che associa a ogni giunto una lista di tuple, dove ogni tupla rappresenta la posizione (Vector3) e la rotazione (Quaternion) del giunto per ciascun frame.</returns>
        /// <exception cref="NullReferenceException">Sollevata se il parametro clip è null.</exception>
        public static Dictionary<string, List<(Vector3 position, Quaternion rotation)>> ExtractAnimationData(AnimationClip clip, out int totalFrames)
        {

            float sampleRate = clip.frameRate;
            // Ottieni il numero totale di frame basato sulla durata dell'animazione e sulla frequenza di campionamento
            totalFrames = Mathf.CeilToInt(clip.length * sampleRate) - 1;

            // Dizionario per memorizzare le curve di ogni giunto
            Dictionary<string, JointCurves> jointCurves = new Dictionary<string, JointCurves>();

            // Dizionario per salvare i dati di posizione e rotazione per ogni giunto a ogni frame
            Dictionary<string, List<(Vector3 position, Quaternion rotation)>> jointData = new Dictionary<string, List<(Vector3, Quaternion)>>();

            jointCurves = ExtractJointCurves(clip);

            // Itera su ogni frame
            for (int i = 0; i < totalFrames; i++)
            {
                // Tempo in secondi
                float time = i / sampleRate;

                foreach (var kvp in jointCurves)
                {
                    string jointName = kvp.Key;
                    JointCurves curves = kvp.Value;

                    // Inizializza la lista per il giunto se non è ancora presente in jointData
                    if (!jointData.ContainsKey(jointName))
                    {
                        jointData[jointName] = new List<(Vector3, Quaternion)>();
                    }

                    // Campiona la posizione e la rotazione dal tempo specifico
                    Vector3 position = new Vector3(
                        curves.positionX != null ? curves.positionX.Evaluate(time) : 0,
                        curves.positionY != null ? curves.positionY.Evaluate(time) : 0,
                        curves.positionZ != null ? curves.positionZ.Evaluate(time) : 0
                    );

                    Quaternion rotation = new Quaternion(
                        curves.rotationX != null ? curves.rotationX.Evaluate(time) : 0,
                        curves.rotationY != null ? curves.rotationY.Evaluate(time) : 0,
                        curves.rotationZ != null ? curves.rotationZ.Evaluate(time) : 0,
                        curves.rotationW != null ? curves.rotationW.Evaluate(time) : 1
                    );

                    // Aggiungi i dati al dizionario o alla struttura in cui stai salvando i dati per frame
                    jointData[jointName].Add((position, rotation));
                }
            }

            // Chimata per il metodo Debug
            if (debug)
            {
                Debug.Log($"Nome dell'animazione processata{clip.name}");
                //DebugJointCurves(jointCurves);
                Debug.Log("==========================================");
                Debug.Log($"Numero totale di frame calcolati: {totalFrames} ad un framerate {sampleRate}");
                Debug.Log("==========================================");
                DebugJointData(jointData);
            }

            return jointData;
        }

        /// <summary>
        /// Stampa il contenuto del dizionario jointData, visualizzando l'ordine di processamento dei giunti, 
        /// la lunghezza della lista associata a ciascun giunto, e la rotazione del primo frame per ogni giunto.
        /// </summary>
        /// <param name="jointData">Dizionario contenente i dati di posizione e rotazione per ogni giunto.</param>
        private static void DebugJointData(Dictionary<string, List<(Vector3 position, Quaternion rotation)>> jointData)
        {
            int i = 0;
            foreach (var kvp in jointData)
            {
                string jointName = kvp.Key;
                List<(Vector3 position, Quaternion rotation)> frames = kvp.Value;

                // Stampa la rotazione del primo frame, se disponibile
                if (frames.Count > 0)
                {
                    Quaternion firstFrameRotation = frames[0].rotation;
                    var eulerRot = Quaternion.Normalize(firstFrameRotation).eulerAngles;
                    Debug.Log($" Giunto: n{i} - {jointName} Rotazione primo frame - X: {(eulerRot.x > 180 ? eulerRot.x - 360 : eulerRot.x)}, Y: {(eulerRot.y > 180 ? eulerRot.y - 360 : eulerRot.y)}, Z: {(eulerRot.z > 180 ? eulerRot.z - 360 : eulerRot.z)}");
                    i++;
                }
                else
                {
                    Debug.LogWarning($"  Nessun frame disponibile per il giunto: {jointName}");
                }
            }
        }


        /// <summary>
        /// Estrae le curve di posizione e rotazione per ogni giunto nel AnimationClip specificato.
        /// </summary>
        /// <param name="clip">L'AnimationClip da cui estrarre le curve dei giunti.</param>
        /// <returns>Un dizionario in cui ogni chiave è il nome del giunto e il valore è un oggetto JointCurves che contiene
        /// le curve di posizione (X, Y, Z) e rotazione (X, Y, Z, W) per quel giunto.</returns>
        private static Dictionary<string, JointCurves> ExtractJointCurves(AnimationClip clip)
        {
            Dictionary<string, JointCurves> curves = new Dictionary<string, JointCurves>();
#if UNITY_EDITOR
            // Per ogni giunto, ottieni la posizione e la rotazione
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                // Ottieni il nome del giunto
                string jointPath = binding.path;
                string jointName = jointPath.Substring(jointPath.LastIndexOf('/') + 1);

                // Aggiungi il giunto se non esiste già nel dizionario
                if (!curves.ContainsKey(jointName))
                {
                    curves[jointName] = new JointCurves
                    {
                        positionX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalPosition.x")),
                        positionY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalPosition.y")),
                        positionZ = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalPosition.z")),
                        rotationX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalRotation.x")),
                        rotationY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalRotation.y")),
                        rotationZ = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalRotation.z")),
                        rotationW = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(jointPath, typeof(Transform), "m_LocalRotation.w"))
                    };
                }
            }
#endif
            return curves;
        }

        /// <summary>
        /// Debugs the joint curves by printing the order of joints and checking if all curves are assigned.
        /// </summary>
        /// <param name="curves">Dictionary containing the joint curves to debug.</param>
        private static void DebugJointCurves(Dictionary<string, JointCurves> curves)
        {
            Debug.Log($"La dimensione del dizionario è {curves.Count}");
            foreach (var joint in curves)
            {
                string jointName = joint.Key;
                JointCurves jointCurves = joint.Value;

                // Check if all curves are assigned
                bool hasAllCurves = jointCurves.rotationX != null &&
                                    jointCurves.rotationY != null &&
                                    jointCurves.rotationZ != null &&
                                    jointCurves.rotationW != null;

                // Print debug information
                Debug.Log($"Joint: {jointName}");
                Debug.Log($"  All Rotation Curves Assigned: {hasAllCurves}");

                if (!hasAllCurves)
                {
                    Debug.LogWarning($"  Missing Curves for Joint: {jointName}");
                    if (jointCurves.rotationX == null) Debug.LogWarning("    Missing rotationX curve");
                    if (jointCurves.rotationY == null) Debug.LogWarning("    Missing rotationY curve");
                    if (jointCurves.rotationZ == null) Debug.LogWarning("    Missing rotationZ curve");
                    if (jointCurves.rotationW == null) Debug.LogWarning("    Missing rotationW curve");
                }
                else
                {
                    Debug.Log("  All curves assigned correctly.");
                }
            }
        }
    }
}