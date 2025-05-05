using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using gianmarcolelli.BVHTools;

namespace gianmarcolelli.AgentBehavior
{
    internal class HumanoidCloningManager : MonoBehaviour
    {

        internal int currentFrame { get; set; }                                                                         // Frame processato
        internal List<string> humStructureOrder { get; set; }                                                           // debug per avere l'ordine con cui sono stati estratti i giunti
        internal Quaternion[,] animationRot;                                                                            // Animazione processata
        internal Vector3[] comPos;                                                                                      // Posizione globale del centro di massa per ogni frame
        internal Vector3[,] animGlobalPos;                                                                              // Posizione globale dei giunti per tutta l'animazione
        internal Vector3[,] animAngularVelocity;                                                                        // Velocita' angolare per ogni coppia di giunti
        //internal (Vector3, Vector3)[] eofPosition;                                                                      // Posizione degli end effector "piedi" per ogni frame
        //internal Vector3 previusPelvisPos;                                                                              // Posizione del centro di massa al frame precedente
        internal DataLoader dataLoader { get; set; }                                                                    // Classe che permette l'estrazione dei dati in fase di Behavior Cloning
        internal bool dataLoaded { get; set; }                                                                          // Flag per verificare se i dati AnimationClip sono stati caricati
        //public RunPythonCommand gui;                                                                                          // Per estrarre il nome della cartella da dove caricare la clip
        public string animationName = "graspingObject";                                                                       // Utile per la scena di visualizzazione delle animazioni

        /*
        [Header("Training Type")]
        [Tooltip("Se è vero, l'agente viene penalizzato per aver allontanato l'end effector dal target")]
        public bool earlyTraining = false;
        */

        void Awake()
        {
            currentFrame = 0;
            dataLoader = new DataLoader();
            dataLoaded = false;
        }

        internal void OnEpisodeBeginConf()
        {
            currentFrame = 0;
            // RandomCurrentFrame();
        }

        /// <summary>
        /// Loads the animation data by fetching it from the specified source, 
        /// processing it into a structured format, and marking the data as loaded.
        /// </summary>
        internal void LoadAnimation()
        {
            try
            {
                // Controllo se GUI è null
                /*if (gui == null)
                {
                    Debug.LogError("GUI reference is null");
                    return;
                }

                // Retrieve the animation name from the RunPythonCommand component of the GUI
                var animationName = gui.extension;
                if (string.IsNullOrEmpty(animationName))
                {
                    Debug.LogError("animationName is null or empty.");
                    return;
                }*/

                // Load the animation clip using the DataLoader
                dataLoader.LoadAnimationClip(animationName);

                // Retrieve the animation data from the DataLoader
                var animation = dataLoader.GetAnimation();

                // Extract the keys (joint names) from the animation in the correct hierarchical order
                humStructureOrder = ExtractKeysInOrder(animation);

                // Convert the animation dictionary to a matrix format for efficient processing
                animationRot = ConvertDictionaryToMatrix(animation);

                dataLoaded = true;
                if (dataLoaded)
                {
                    DebugLogger.Log(
                        $"Animation Loaded Successfully:\n" +
                        $"- Animation Name: {animationName}\n" +
                        $"- Humanoid Structure Order: {string.Join(", ", humStructureOrder)}\n" +
                        $"- Animation Rotation Matrix Size: {animationRot.GetLength(0)} x {animationRot.GetLength(1)}",
                        "LoadAnimation",
                        LogType.Log
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log($"Exception in LoadAnimation: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Extracts the global positions of the pelvis (Hips) joint from the provided animation data.
        /// </summary>
        /// <param name="animation">
        /// A dictionary containing animation data, where the key represents the joint name 
        /// and the value is a list of tuples. Each tuple contains the global position (Vector3) 
        /// and rotation (Quaternion) of the joint for each frame.
        /// </param>
        /// <param name="hipPositionsBuffer">
        /// Un array preallocato di Vector3 che verrà riempito con le posizioni del pelvis.
        /// La lunghezza dell'array deve corrispondere al numero di frame nell'animazione.
        /// </param>
        private void ExtractPelvisGlobalPosition(Dictionary<string, List<(Vector3 position, Quaternion rotation)>> animation, Vector3[] hipPositionsBuffer)
        {
            var hipData = animation["Hips"];

            if (hipPositionsBuffer.Length != hipData.Count)
            {
                Debug.LogError("Buffer size mismatch: Ensure the buffer length matches the number of frames in the animation.");
                return;
            }

            for (int i = 0; i < hipData.Count; i++)
            {
                hipPositionsBuffer[i] = hipData[i].position;
            }
        }

        /// <summary>
        /// Extracts all keys from a dictionary and returns them as a list in the same order
        /// as they appear in the dictionary. This ensures that the order of keys in the 
        /// resulting list matches the insertion order of the dictionary. Use it for debug.
        ///
        /// <param name="animationDictionary">
        /// A dictionary where the keys are strings representing body parts (e.g., "Hips", "Spine"), 
        /// and the values are lists of tuples containing position (Vector3) and rotation (Quaternion) 
        /// data for animation frames.
        /// </param>
        ///
        /// <returns>
        /// A list of strings containing the keys from the dictionary in their original order.
        /// </returns>
        public List<string> ExtractKeysInOrder(Dictionary<string, List<(Vector3 position, Quaternion rotation)>> animationDictionary)
        {
            var structure = new List<string>();

            // Iterate through the dictionary and add each key to the list
            foreach (var key in animationDictionary.Keys)
            {
                structure.Add(key);
            }
            return structure;
        }

        /// <summary>
        /// Converts animation data stored in a dictionary into a 2D rotation matrix where:
        /// - Rows represent frames of the animation.
        /// - Columns represent body parts.
        ///
        /// The method retrieves animation data from a dictionary, calculates the number 
        /// of body parts (columns) and frames (rows), and initializes a 2D matrix to store
        /// rotation quaternions for each frame and body part. The rotation data is extracted 
        /// from the dictionary and populated into the matrix for further use.
        ///
        /// <param name="animation">
        /// A dictionary containing animation data where the key is the body part name (string), 
        /// and the value is a list of tuples. Each tuple consists of a position (Vector3) and 
        /// a rotation (Quaternion) for each frame of the animation.
        /// </param>
        ///
        /// <returns>
        /// A 2D array of quaternions representing the rotation matrix, where each row corresponds 
        /// to a frame of the animation and each column corresponds to a body part.
        /// </returns>

        internal Quaternion[,] ConvertDictionaryToMatrix(Dictionary<string, List<(Vector3 position, Quaternion rotation)>> animation)
        {
            // Get the number of columns (body parts) and rows (frames)
            int numBodyParts = animation.Keys.Count;
            int numFrames = animation.Values.Max(frames => frames.Count);

            // Create a matrix to store rotations
            animationRot = new Quaternion[numFrames, numBodyParts];

            // Fill the matrix with rotation data
            int columnIndex = 0;
            foreach (var bodyPart in animation)
            {
                for (int frameIndex = 0; frameIndex < bodyPart.Value.Count; frameIndex++)
                {
                    animationRot[frameIndex, columnIndex] = bodyPart.Value[frameIndex].rotation;
                }
                columnIndex++;
            }

            return animationRot;
        }

        /// <summary>
        /// Restituisce la lunghezza della lista di frame per una determinata animazione,
        /// data la struttura delle animazioni.
        /// </summary>
        /// <returns>La lunghezza della lista di frame per l'animazione specificata, oppure -1 se l'animazione non esiste.</returns>
        internal int GetAnimationLength()
        {
            return dataLoader.GetAnimationLength();
        }

        /// <summary>
        /// Restituisce il frameRate utilizzatyo per estrarre le animazioni. Esso e' un vaslore staticho che dovrebbe essere automatizzato.
        /// Attualmente il frameRate e' pari a 20fps valore estratto dai file BVH usarti per la realizzazione del dataset.
        /// </summary>
        /// <returns>Frame rate dell'animazione</returns>
        internal float GetFrameRate()
        {
            return dataLoader.GetFrameRate();
        }

        /// <summary>
        /// Redefines the animation joint structure by removing unwanted joints, recalculating local rotations 
        /// for specific joints, and updating the animation rotation matrix and joint hierarchy.
        /// 
        /// The method performs the following operations:
        /// 1. Eliminates unwanted joints from the animation structure.
        /// 2. Recalculates the local rotations of critical joints (Spine1, LeftArm, RightArm, Head) 
        ///    relative to their respective parent joints.
        /// 3. Updates the animation rotation matrix to reflect the new joint hierarchy and rotations.
        /// 4. Updates the joint structure order to match the modified hierarchy.
        ///
        /// <remarks>
        /// The input `animationRot` matrix represents the animation data with rows as frames and columns as joints.
        /// Unwanted joints are identified and removed using a predefined set of joint indices.
        /// Local rotations for critical joints are recalculated based on global rotation chains.
        /// </remarks>
        ///
        /// </summary>
        internal void JointRedefination()
        {

            int numFrames = animationRot.GetLength(0);
            int numJoints = animationRot.GetLength(1);
            int numNewJoints = numJoints - 7;

            DebugLogger.Log($"Starting JointRedefination - Frames: {numFrames}, Joints: {numJoints}", "JointRedefination", LogType.Log);

            Quaternion[,] newAnimationRot = new Quaternion[numFrames, numNewJoints];
            var jointToDelete = new HashSet<int> { 4, 8, 9, 11, 12, 14, 18 };

            DebugLogger.Log($"Joints to delete: {string.Join(", ", jointToDelete.Select(index => humStructureOrder[index]).ToList())}", "JointRedefination", LogType.Log);

            // lista di giunti che devono rimanere alla fine della ricostruzione della struttura
            var structure = DeleteUnwantedJoint(new List<string>(humStructureOrder), jointToDelete);

            DebugLogger.Log($"New Humanoid Structure Order:\n -{string.Join("\n -", structure)}", "JointRedefination", LogType.Log);

            // Get id of every relevant joint
            int spine1Id = GetIdByValue(structure, "Spine1");
            int leftArmId = GetIdByValue(structure, "LeftArm");
            int rightArmId = GetIdByValue(structure, "RightArm");
            int headId = GetIdByValue(structure, "Head");

            // Iterate through each frame
            for (int i = 0; i < numFrames; i++)
            {

                // compute local rotation of spine1 respect hips
                List<Quaternion> spine1Structure = new List<Quaternion>() { animationRot[i, 0], animationRot[i, 9], animationRot[i, 10] };
                List<Quaternion> spine1StructGlobalRot = ComputeGlobalRotations(spine1Structure);
                Quaternion spine1LocRot = ComputeLocalRotation(spine1StructGlobalRot[0], spine1StructGlobalRot[2]);

                // compute local rotation of Head respect spine1
                List<Quaternion> headStructure = new List<Quaternion>() { spine1StructGlobalRot[2], animationRot[i, 11], animationRot[i, 12], animationRot[i, 13] };
                List<Quaternion> headStructGlobalRot = ComputeGlobalRotations(headStructure);
                Quaternion headLocRot = ComputeLocalRotation(headStructGlobalRot[0], headStructGlobalRot[3]);

                // compute local rotation of LeftArm respect spine1
                List<Quaternion> leftArmStructure = new List<Quaternion>() { spine1StructGlobalRot[2], animationRot[i, 11], animationRot[i, 14], animationRot[i, 15] };
                List<Quaternion> leftArmStructGlobalRot = ComputeGlobalRotations(leftArmStructure);
                Quaternion leftArmLocRot = ComputeLocalRotation(leftArmStructGlobalRot[0], leftArmStructGlobalRot[3]);

                // compute local rotation of RightArm respect spine1
                List<Quaternion> rightArmStructure = new List<Quaternion>() { spine1StructGlobalRot[2], animationRot[i, 11], animationRot[i, 18], animationRot[i, 19] };
                List<Quaternion> rightArmStructGlobalRot = ComputeGlobalRotations(rightArmStructure);
                Quaternion rightArmLocRot = ComputeLocalRotation(rightArmStructGlobalRot[0], rightArmStructGlobalRot[3]);

                // Create and populate the tmp list with items from animationRot
                var tmp = Enumerable.Range(0, animationRot.GetLength(1)).Select(j => animationRot[i, j]).ToList();

                // eliminate unwanted joints
                tmp = (List<Quaternion>)DeleteUnwantedJoint(tmp, jointToDelete);

                // sobstitude old joint with new global rotation joint
                tmp[spine1Id] = spine1LocRot;
                tmp[leftArmId] = leftArmLocRot;
                tmp[rightArmId] = rightArmLocRot;
                tmp[headId] = headLocRot;

                // Replace row
                for (int j = 0; j < newAnimationRot.GetLength(1); j++)
                {
                    newAnimationRot[i, j] = tmp[j];
                }
            }

            // Aggiorno la struttura globale
            animationRot = newAnimationRot;
            humStructureOrder = (List<string>)structure;

            DebugLogger.Log(
                $"JointRedefination Completed!\n" +
                $"- Frames Processed: {numFrames}\n" +
                $"- Initial Joints: {numJoints}\n" +
                $"- Final Joints: {numNewJoints}\n" +
                $"- Removed Joints: {string.Join(", ", jointToDelete)}\n" +
                $"- Final Humanoid Structure: {string.Join(", ", humStructureOrder)}",
                "JointRedefination",
                LogType.Log
            );
        }

        /// <summary>
        /// Retrieves the index of the first occurrence in a list where the value contains the specified substring.
        /// </summary>
        /// <param name="structure">The list of strings to search.</param>
        /// <param name="v">The substring to search for within the list elements.</param>
        /// <returns>
        /// The index of the first element in the list that contains the specified substring, 
        /// or 0 if no such element is found.
        /// </returns>
        /// <remarks>
        /// This method uses LINQ to pair each element with its index, filters elements containing the substring,
        /// and selects the index of the first match. If no match is found, it returns the default value (0).
        /// </remarks>
        private int GetIdByValue(IList<string> structure, string v)
        {
            return structure.Select((value, idx) => new { value, idx }) // Pair values with their indices
                                        .Where(item => item.value.Contains(v)) // Filter by the condition
                                        .Select(item => item.idx) // Get the index of the match
                                        .FirstOrDefault();
        }

        /// <summary>
        /// Removes elements from a collection at specified indices.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="quaternions">The input collection from which elements will be removed.</param>
        /// <param name="hashSet">A set of indices indicating the positions of elements to be removed.</param>
        /// <returns>A new list with the specified elements removed.</returns>
        private IList<T> DeleteUnwantedJoint<T>(IList<T> quaternions, HashSet<int> hashSet)
        {
            var tmp = new List<T>(quaternions);
            // Remove items by iterating in reverse
            for (int j = tmp.Count - 1; j >= 0; j--)
            {
                if (hashSet.Contains(j))
                {
                    tmp.RemoveAt(j);
                }
            }
            return tmp;
        }

        /// <summary>
        /// Computes the local rotation of a child object relative to its parent.
        /// This is achieved by multiplying the inverse of the parent's rotation 
        /// with the child's global rotation.
        ///
        /// <param name="child">The global rotation of the child object as a Quaternion.</param>
        /// <param name="parent">The global rotation of the parent object as a Quaternion.</param>
        /// <returns>
        /// A Quaternion representing the child's local rotation relative to its parent.
        /// </returns>
        private Quaternion ComputeLocalRotation(Quaternion g_parent, Quaternion g_child)
        {
            return Quaternion.Inverse(g_parent) * g_child;
        }

        /// <summary>
        /// Computes the global rotations of a hierarchy based on a list of local rotations.
        /// The global rotation of the root is assumed to be its local rotation, and each subsequent 
        /// global rotation is calculated by combining the parent's global rotation with the local rotation.
        ///
        /// <param name="structureRot">
        /// A list of Quaternions representing the local rotations of each joint in a hierarchy,
        /// ordered from root to the last child.
        /// </param>
        /// <returns>
        /// A list of Quaternions representing the global rotations of each joint in the hierarchy,
        /// maintaining the same order as the input.
        /// </returns>
        private List<Quaternion> ComputeGlobalRotations(List<Quaternion> structureRot)
        {
            // Create a list to store the global rotations
            List<Quaternion> globalRotations = new List<Quaternion>();

            // Iterate through the list
            for (int i = 0; i < structureRot.Count; i++)
            {
                if (i == 0)
                {
                    // The root's global rotation is its local rotation
                    globalRotations.Add(structureRot[i]);
                }
                else
                {
                    // Compute the global rotation by combining the parent's global rotation with the local rotation
                    Quaternion globalRotation = globalRotations[i - 1] * structureRot[i];
                    globalRotations.Add(globalRotation);
                }
            }

            return globalRotations;
        }

        /// <summary>
        /// Computes the angular velocity of each joint between consecutive frames
        /// based on quaternion rotations stored in a 2D matrix.
        /// 
        /// The method calculates the relative rotation between consecutive frames for each joint, 
        /// extracts the vector part of the relative quaternion, and scales it by the time interval 
        /// between frames to derive the angular velocity.
        /// 
        /// Results are stored in a 2D matrix where rows represent frames and columns represent joints.
        /// </summary>
        internal void ComputeAngularVelocity(float deltaTime)
        {
            int frames = animationRot.GetLength(0); // Number of rows (frames)
            int joints = animationRot.GetLength(1); // Number of columns (joints)

            if (frames > 2)
            {
                // Resultant angular velocity matrix
                Vector3[,] angularVelocities = new Vector3[frames, joints];

                for (int frame = 1; frame < frames; frame++)
                {
                    for (int joint = 0; joint < joints; joint++)
                    {
                        Quaternion q1 = animationRot[frame - 1, joint];
                        Quaternion q2 = animationRot[frame, joint];

                        // Controllo stabilità del quaternione
                        if (Quaternion.Dot(q1, q1) < 1e-6f || Quaternion.Dot(q2, q2) < 1e-6f)
                        {
                            Debug.LogWarning($"Frame {frame}, Joint {joint}: Quaternione instabile, salto il calcolo.");
                            continue;
                        }

                        // Compute relative quaternion q_rel = q2 * q1^(-1)
                        Quaternion qRel = q2 * Quaternion.Inverse(q1);

                        // Estrai angolo e asse
                        qRel.ToAngleAxis(out float angleDeg, out Vector3 axis);
                        if (axis.magnitude < 1e-6f) axis = new Vector3(1, 0, 0); // Usa asse standard

                        float angleRad = angleDeg * Mathf.Deg2Rad;

                        if (Mathf.Abs(deltaTime) < 1e-6f)
                        {
                            Debug.LogWarning($"Frame {frame}, Joint {joint}: deltaTime troppo piccolo, salto il calcolo.");
                            continue;
                        }

                        // Calcolo della velocità angolare
                        Vector3 angularVelocity = axis.normalized * (angleRad / deltaTime);

                        // Debug dettagliato
                        //Debug.Log($"Frame {frame}, Joint {joint}: q1={q1}, q2={q2}, qRel={qRel}, angleDeg={angleDeg}, axis={axis}, angularVelocity={angularVelocity}");

                        angularVelocities[frame, joint] = angularVelocity;
                    }
                }

                DebugLogger.Log(
                    $"ComputeAngularVelocity Completed!\n" +
                    $"- Frames Processed: {frames - 1}\n" +
                    $"- Joints Processed: {joints}\n" +
                    $"- DeltaTime: {deltaTime}\n" +
                    $"- Resultant Matrix Size: {angularVelocities.GetLength(0)} x {angularVelocities.GetLength(1)}\n",
                    "AngularVelocity",
                    LogType.Log
                );

                animAngularVelocity = angularVelocities;
            }
            else
            {
                Debug.LogWarning("Not enough frames to compute angular velocity");
                return;
            }
        }

        /// <summary>
        /// Computes the center of mass (CoM) position for each frame of an animation sequence.
        /// The method iterates through all animation frames, updating the local rotations of the 
        /// provided body parts and calculating the weighted center of mass based on the rigidbodies' masses.
        /// The computed CoM positions are stored in the `comPos` array.
        /// </summary>
        /// <param name="bodyParts">A list of BodyPart objects, each containing a Rigidbody, used for CoM calculation.</param>
        /// 
        /*internal void ComputeAnimComPosition(List<BodyPart> bodyParts)
        {
            int animationLength = GetAnimationLength();
            comPos = new Vector3[animationLength];
            var currentPose = new Quaternion[bodyParts.Count];

            while (currentFrame < animationLength)
            {
                GetCurrentDataJointRotation(currentPose);
                //Muovo il manichino al frame corrente
                for (int i = 1; i < currentPose.Length; i++)
                {
                    if (float.IsNaN(currentPose[i].x) || float.IsNaN(currentPose[i].y) || float.IsNaN(currentPose[i].z) || float.IsNaN(currentPose[i].w))
                    {
                        Debug.LogError($"ComputeAnimComPosition: Il quaternion del giunto {i} ha valori NaN!");
                    }
                    bodyParts[i].rb.transform.localRotation = currentPose[i];
                }
                var com = ComputeComPosition(bodyParts);
                comPos[currentFrame] = com;
                currentFrame++;
            }
            currentFrame = 0;
            DebugLogger.Log(
                $"ComputeComPosition Completed!\n" +
                $"- Frames Processed: {animationLength}\n" +
                $"- Com Value estratti: {comPos.Length}",
                "ComputeComPosition",
                LogType.Log
            );
        }*/

        /// <summary>
        /// Computes the Center of Mass (CoM) position for a given set of body parts.
        /// The CoM is calculated as the mass-weighted average of the world-space positions
        /// of all rigid bodies in the provided list of body parts.
        ///
        /// <param name="bodyParts">A list of BodyPart objects containing rigid bodies (rb).</param>
        /// <returns>The world-space position of the computed Center of Mass.</returns>

        /*internal Vector3 ComputeComPosition(List<BodyPart> bodyParts)
        {
            Vector3 sum = Vector3.zero;
            float totalMass = 0f;

            foreach (var joint in bodyParts)
            {
                sum += joint.rb.worldCenterOfMass * joint.rb.mass;
                totalMass += joint.rb.mass;
            }
            return sum / totalMass;
        }*/

        /// <summary>
        /// Loads a specific frame's data from a 2D matrix into a preallocated buffer.
        /// This method extracts data from the given `data` matrix at the specified `currentFrame`
        /// and stores it into the provided `frameBuffer` array to avoid unnecessary memory allocations.
        ///
        /// <typeparam name="T">The type of data stored in the matrix (e.g., Quaternion, Vector3).</typeparam>
        /// <param name="data">The 2D array containing animation or joint data.</param>
        /// <param name="currentFrame">The index of the frame to be extracted.</param>
        /// <param name="frameBuffer">
        /// The preallocated array where the extracted frame data will be stored.
        /// Its length must match the number of columns in the `data` matrix.
        /// </param>
        private void LoadFrame<T>(T[,] data, int currentFrame, T[] frameBuffer)
        {
            for (int i = 0; i < data.GetLength(1); i++)
            {
                frameBuffer[i] = data[currentFrame, i];
            }
        }

        /// <summary>
        /// Retrieves the joint rotations for the current animation frame and stores them 
        /// in the provided preallocated array to avoid unnecessary memory allocations.
        /// </summary>
        /// <param name="jointRotations">An array where the joint rotations for the current frame will be stored.</param>
        internal void GetCurrentDataJointRotation(Quaternion[] jointRotations)
        {
            LoadFrame(animationRot, currentFrame, jointRotations);
        }

        /// <summary>
        /// Retrieves the angular velocities of the joints for the current animation frame 
        /// and stores them in the provided preallocated array to optimize memory usage.
        /// </summary>
        /// <param name="jointVelocities">An array where the joint angular velocities for the current frame will be stored.</param>
        internal void GetCurrentDataJointVelocity(Vector3[] jointVelocities)
        {
            LoadFrame(animAngularVelocity, currentFrame, jointVelocities);
        }

        /// <summary>
        /// Restituisce la posizione attuale del centro di massa (CoM) per il frame corrente.
        /// </summary>
        /// <returns>Un <c>Vector3</c> che rappresenta la posizione del centro di massa nel frame attuale.</returns>
        internal Vector3 GetCurrentDataCom()
        {
            return comPos[currentFrame];
        }

        /// <summary>
        /// Seleziona casualmente un frame all'interno della lunghezza dell'animazione e lo imposta come frame corrente.
        /// </summary>
        /// <returns>Il numero del frame selezionato casualmente.</returns>
        internal void RandomCurrentFrame()
        {
            currentFrame = Random.Range(0, dataLoader.GetAnimationLength());
        }


        /*
        internal void ComputeAnimBpPosition(List<BodyPart> bodyParts)
        {
            int frames = animationRot.GetLength(0);
            int joints = animationRot.GetLength(1);

            if (joints != bodyParts.Count)
            {
                Debug.LogWarning("Data mismatch, the number of joints in the animation is different than the number of joints in the scene");
                return;
            }

            if (frames < 2)
            {
                Debug.LogWarning("Not enough frames to compute angular velocity");
                return;
            }

            // Resultant angular velocity matrix
            animGlobalPos = new Vector3[frames, joints];
            var currentPose = new Quaternion[joints];

            while (currentFrame < frames)
            {
                GetCurrentDataJointRotation(currentPose);
                //Muovo il manichino al frame corrente
                for (int i = 0; i < currentPose.Length; i++)
                {
                    bodyParts[i].ForceRotation(currentPose[i]);
                    animGlobalPos[currentFrame, i] = bodyParts[i].rb.position;
                }
                currentFrame++;
            }
            currentFrame = 0;
        }

        /// <summary>
        /// Computes the End of Foot (EOF) positions for the right and left ankle joints across all animation frames. 
        /// This method reconstructs the hierarchical transformations for the lower body joints, starting from the pelvis 
        /// and propagating rotations through the hip and knee to the ankle. 
        /// The computed ankle positions are converted to the local coordinate space of the provided pelvis transform.
        /// </summary>
        /// <param name="rPelvisToHip">Offset from pelvis to right hip.</param>
        /// <param name="rHipToKnee">Offset from right hip to right knee.</param>
        /// <param name="rKneeToAnkle">Offset from right knee to right ankle.</param>
        /// <param name="lPelvisToHip">Offset from pelvis to left hip.</param>
        /// <param name="lHipToKnee">Offset from left hip to left knee.</param>
        /// <param name="lKneeToAnkle">Offset from left knee to left ankle.</param>
        /// <param name="pelvis">The reference Transform representing the pelvis in the Unity scene. Used for converting global ankle positions to local space.</param>
        internal void ComputeEofPosition(ref Vector3 rPelvisToHip, ref Vector3 rHipToKnee, ref Vector3 rKneeToAnkle,
                                         ref Vector3 lPelvisToHip, ref Vector3 lHipToKnee, ref Vector3 lKneeToAnkle,
                                         Transform pelvis)
        {
            // Get the total number of animation frames from the data loader
            int animationLength = dataLoader.GetAnimationLength();

            // Initialize an array to store the computed ankle positions for each frame
            eofPosition = new (Vector3, Vector3)[animationLength];

            // Iterate through all animation frames to compute ankle positions
            for (int frame = 0; frame < animationLength; frame++)
            {
                // Retrieve the global position and rotation of the root joint (typically the hips)
                Vector3 root_globalPos = hipGlobalPos[frame];
                Quaternion root_globalRot = animationRot[frame, 0];

                // Compute left hip's global position and rotation
                Vector3 lhip_globalPos = root_globalPos + (root_globalRot * lPelvisToHip);
                Quaternion lhip_globalRot = root_globalRot * animationRot[frame, 1];

                // Compute left knee's global position and rotation
                Vector3 lknee_globalPos = lhip_globalPos + (lhip_globalRot * lHipToKnee);
                Quaternion lknee_globalRot = lhip_globalRot * animationRot[frame, 2];

                // Compute right hip's global position and rotation
                Vector3 rhip_globalPos = root_globalPos + (root_globalRot * rPelvisToHip);
                Quaternion rhip_globalRot = root_globalRot * animationRot[frame, 4];

                // Compute right knee's global position and rotation
                Vector3 rknee_globalPos = rhip_globalPos + (rhip_globalRot * rHipToKnee);
                Quaternion rknee_globalRot = rhip_globalRot * animationRot[frame, 5];

                // Compute right and left ankle's global position
                Vector3 rankle_globalPos = rknee_globalPos + (rknee_globalRot * rKneeToAnkle);
                Vector3 lankle_globalPos = lknee_globalPos + (lknee_globalRot * lKneeToAnkle);

                // Convert ankle global positions to local space with respect to the pelvis transform
                Vector3 rankle_localPos_wrt_Pelvis = Quaternion.Inverse(pelvis.rotation) * (rankle_globalPos - pelvis.position);
                Vector3 lankle_localPos_wrt_Pelvis = Quaternion.Inverse(pelvis.rotation) * (lankle_globalPos - pelvis.position);

                if (frame == 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==== CALCOLO DELLE POSIZIONI RISPETTO PELVIS ====");
                    sb.AppendLine($"Pelvis Global Position = {root_globalPos}");
                    sb.AppendLine($"Pelvis Global Rotation = {root_globalRot}");

                    sb.AppendLine($"Hip L Global Position = {lhip_globalPos}");
                    sb.AppendLine($"Hip L Global Rotation = {lhip_globalRot}");

                    sb.AppendLine($"Knee L Global Position = {lknee_globalPos}");
                    sb.AppendLine($"Knee L Global Rotation = {lknee_globalRot}");

                    sb.AppendLine($"Hip R Global Position = {rhip_globalPos}");
                    sb.AppendLine($"Hip R Global Rotation = {rhip_globalRot}");

                    sb.AppendLine($"Knee R Global Position = {rknee_globalPos}");
                    sb.AppendLine($"Knee R Global Rotation = {rknee_globalRot}");

                    sb.AppendLine($"Ankle R Global Position = {rankle_globalPos}");
                    sb.AppendLine($"Ankle L Global Position = {lankle_globalPos}");

                    sb.AppendLine($"Ankle R local Position = {rankle_localPos_wrt_Pelvis}");
                    sb.AppendLine($"Ankle L local Position = {lankle_localPos_wrt_Pelvis}");
                    Debug.Log(sb.ToString());
                }

                // Store computed local ankle positions for the current frame
                eofPosition[frame] = (rankle_localPos_wrt_Pelvis, lankle_localPos_wrt_Pelvis);
            }
        }
        */

        /// <summary>
        /// Retrieves the current End-of-Frame (EOF) position data for both the right and left ankle.
        /// </summary>
        /// <returns>
        /// A tuple containing two Vector3 values:
        /// - Item1: The current position of the right ankle.
        /// - Item2: The current position of the left ankle.
        /// </returns>
        /*
        internal (Vector3, Vector3) GetCurrentDataEofPosition()
        {
            return (eofPosition[currentFrame].Item1, eofPosition[currentFrame].Item2);
        }
        */

        /// <summary>
        /// Computes the deviations of the center of mass (CoM) between consecutive frames.
        /// The deviations are calculated as the difference between the global hip position
        /// at the current frame and the previous frame, and stored in a list.
        /// </summary>
        /*internal void ComputeComDeviations()
        {
            comDeviation = new Vector3[(hipGlobalPos.Length - 1)];
            for (int frame = 1; frame < hipGlobalPos.Length; frame++)
            {
                comDeviation[frame - 1] = hipGlobalPos[frame] - hipGlobalPos[frame - 1];
            }
        }*/

        /*internal void ComputeComPosition(List<BodyPart> bodyPartsList)
        {
            Vector3 sum = Vector3.zero;
            float totalMass = 0f;

            foreach (var rb in bodyPartsList)
            {
                sum += rb.worldCenterOfMass * rb.mass;
                totalMass += rb.mass;
            }

            return sum / totalMass;
        }*/
    }
}