#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace gianmarcolelli.BVHTools
{

    /// <summary>
    /// Eccezione personalizzata lanciata quando si verifica un errore durante il caricamento dei dati di animazione.
    /// Utilizzata per segnalare problemi specifici di caricamento, come file mancanti, permessi insufficienti o errori di formattazione.
    /// </summary>
    public class DataLoadException : Exception
    {
        public DataLoadException(string message) : base(message) { }
        public DataLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Classe che si occupa del caricamento dei dati da file AnimationClip a
    /// DataLoader estrae i dati e memorizzali in una struttura animationDataset.
    /// </summary>
    public class DataLoader
    {
        private const string ANIM_FILE_PATH = "Assets/Animations";                                                      // Percorso dove si trovano le animazioni
        public Dictionary<string, List<(Vector3 position, Quaternion rotation)>> animationData;                         // Dataset estratto
        private float fps { get; set; }                                                                                 // FPS per ogni animazione
        private int totalFrames { get; set; }                                                                           // Numero totale di frame che compongono l'animazione

        public DataLoader()
        {
            animationData = null;
            fps = 0;
            totalFrames = 0;
        }

        /// <summary>
        /// Carica l'animazione generata dalla directory specificata e la memorizza nella struttura dati.
        /// Lancia eccezioni se la directory non esiste, se non vengono trovati file di animazione, 
        /// o in caso di altri problemi durante il caricamento dei file.
        /// </summary>
        public void LoadAnimationClip(string clipName)
        {
            string folderPath = $"{ANIM_FILE_PATH}/{clipName}";

            // Verifica se la directory esiste
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            // Ottieni tutti i file AnimationClip nella directory specificata
            string[] animationFiles = Directory.GetFiles(folderPath, "*.anim");

            // Controlla se sono stati trovati file
            if (animationFiles.Length == 0)
            {
                throw new FileNotFoundException($"No animation files found in directory: {folderPath}");
            }

            string animationClip = animationFiles[0];
            try
            {
                // Carica l'AnimationClip da file
                AnimationClip clip = LoadAnimationClipFromFile(animationClip);

                if (clip == null)
                {
                    throw new DataLoadException($"Failed to load AnimationClip from file: {animationClip}");
                }

                int numFrames;
                // Usa AnimationClipDataExtractor per estrarre i dati
                animationData = AnimationClipDataExtractor.ExtractAnimationData(clip, out numFrames);
                fps = clip.frameRate;
                totalFrames = numFrames;
            }

            catch (Exception ex)
            {
                // Gestione specifica per altri problemi di caricamento
                throw new DataLoadException($"An error occurred while processing the animation file: {animationClip}", ex);
            }
        }

        /// <summary>
        /// Carica un'`AnimationClip` dal percorso specificato se eseguito in modalità editor di Unity.
        /// </summary>
        /// <param name="filePath">Percorso del file di animazione da caricare.</param>
        /// <returns>Ritorna l'`AnimationClip` caricato dal percorso specificato.</returns>
        /// <exception cref="ArgumentException">Lanciata se il percorso del file è nullo o vuoto.</exception>
        /// <exception cref="FileNotFoundException">Lanciata se il file non esiste al percorso specificato.</exception>
        /// <exception cref="InvalidOperationException">Lanciata se il metodo viene chiamato fuori dalla modalità editor.</exception>
        private AnimationClip LoadAnimationClipFromFile(string filePath)
        {
#if UNITY_EDITOR
            // Usa AssetDatabase solo in modalità editor
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(filePath);
#else
            throw new InvalidOperationException("LoadAnimationClipFromFile can only be used in Unity Editor mode.");
#endif
        }

        /// <summary>
        /// Ottiene le rotazioni di tutti i giunti per un frame specifico.
        /// </summary>
        /// <param name="currentFrame">L'indice del frame per cui ottenere le rotazioni dei giunti.</param>
        /// <returns>
        /// Una lista di <see cref="Quaternion"/> contenente le rotazioni di tutti i giunti per il frame specificato.
        /// </returns>
        /// <exception cref="ArgumentException">Lanciata se il nome dell'animazione è nullo o vuoto.</exception>
        /// <exception cref="KeyNotFoundException">Lanciata se l'animazione specificata non è presente nel dataset.</exception>
        /// <exception cref="IndexOutOfRangeException">Lanciata se l'indice del frame è fuori dall'intervallo disponibile per uno dei giunti.</exception>
        public List<(string, Quaternion)> GetJointRotations(int currentFrame)
        {
            // Lista per memorizzare le rotazioni di tutti i giunti
            List<(string, Quaternion)> jointRotations = new List<(string, Quaternion)>();

            foreach (KeyValuePair<string, List<(Vector3 position, Quaternion rotation)>> bp in animationData)
            {
                List<(Vector3 position, Quaternion rotation)> jointValues = bp.Value;
                // Verifica che il frame esista nella lista jointValues
                if (currentFrame < jointValues.Count)
                {
                    Quaternion rotation = jointValues[currentFrame].rotation;
                    jointRotations.Add((bp.Key, rotation));
                }
                else
                {
                    throw new IndexOutOfRangeException($"Frame index {currentFrame} is out of range for joint '{bp.Key}'.");
                }
            }
            return jointRotations;
        }

        /// <summary>
        /// Retrieves the animation data containing joint positions and rotations.
        /// </summary>
        /// <returns>
        /// A dictionary where the key is the joint name, and the value is a list of tuples. 
        /// Each tuple contains the position (Vector3) and rotation (Quaternion) for a joint at each frame.
        /// </returns>

        public Dictionary<string, List<(Vector3 position, Quaternion rotation)>> GetAnimation()
        {
            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is not loaded correctelly");
            }
            return animationData;
        }

        /// <summary>
        /// Restituisce il framerate dell'animazione corrente utilizzando la chiave dell'animazione dall'insieme di dati.
        /// </summary>
        /// <returns>Il framerate (FPS) dell'animazione corrente come valore float.</returns>
        public float GetFrameRate()
        {
            return fps;
        }

        /// <summary>
        /// Restituisce la lunghezza dell'animazione.
        /// </summary>
        /// <returns>La lunghezza della lista di frame.</returns>
        public int GetAnimationLength()
        {
            return totalFrames;
        }
    }
}
