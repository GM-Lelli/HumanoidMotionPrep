# MoMaskUnityBridge

Questa cartella contiene gli script necessari per integrare Unity con uno script Python esterno appartenente al progetto [MoMask](https://github.com/EricGuo5513/momask-codes), al fine di generare animazioni a partire da prompt testuali e convertirle in `AnimationClip` utilizzabili in Unity.

## Contenuto

### 1. `PythonCommandRunner.cs`

Questa classe è una utility che si occupa di:
- Eseguire uno script Python con i parametri specificati.
- Costruire il comando da inviare al terminale.
- Validare la directory di lavoro dello script.
- Recuperare un file `.bvh` generato da MoMask nella directory di output.

**Costruttore:**
```csharp
PythonCommandRunner(string pythonPath, string scriptName, string workingDirectory)
````

**Metodi principali:**

* `ValidateWorkingDirectory()`: Verifica l'esistenza della directory di lavoro.
* `BuildCommand(gpuId, extension, textPrompt)`: Costruisce il comando da eseguire.
* `ExecuteCommand(arguments)`: Esegue il comando Python e ne restituisce l'output.
* `GetSpecificFile(extension, fileIndex)`: Ritorna il path del file `.bvh` generato.

---

### 2. `RunPythonCommandEditor.cs`

Finestra editor di Unity accessibile dal menu `Tools > MoMask`, che consente di:

1. Inserire un prompt testuale, estensione, GPU e directory di salvataggio.
2. Avviare l'esecuzione dello script `gen_t2m.py`.
3. Convertire il file `.bvh` generato in un `AnimationClip`.
4. Salvare il `Clip` nella cartella specificata.

**Percorso dello script Python e working directory**
Sono attualmente hardcoded in:

```csharp
string pythonPath = "/home/gianmarco/Apps/anaconda3/envs/gm_thesis_4/bin/python";
string scriptName = "gen_t2m.py";
string workingDir = Path.GetFullPath("../dependencies/momask-codes-main");
```

Assicurati che questi percorsi siano validi sul tuo sistema.

**Dipendenze esterne:**

* Lo script `gen_t2m.py` deve essere disponibile nella directory specificata.
* Richiede la presenza della classe `BVHConvert` per convertire il file `.bvh` in `AnimationClip`.

---

## Utilizzo

1. Apri Unity.
2. Vai su `Tools > MoMask`.
3. Inserisci un prompt (es. "A person is running on a treadmill").
4. Clicca su "Generate Animation".
5. Attendi l'esecuzione del processo e il salvataggio dell'animazione.

## Note

* È consigliato testare prima l’esecuzione manuale dello script Python per assicurarsi che le dipendenze siano soddisfatte.
* Assicurati che il tuo ambiente Python sia correttamente configurato e accessibile.

---

## Autore

Script sviluppati da Gian Marco Lelli per l'integrazione tra MoMask e Unity.