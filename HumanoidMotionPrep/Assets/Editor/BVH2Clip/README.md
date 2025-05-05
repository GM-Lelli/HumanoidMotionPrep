# Documentazione del Convertitore BVH

## Panoramica
Questa documentazione descrive il funzionamento del sistema di conversione dei file BVH in `AnimationClip` utilizzando Unity. Il sistema è costituito da due file principali: `BVH2ClipEditor.cs` e `BVHConverter.cs`, che lavorano insieme per consentire agli utenti di importare file BVH e convertirli in animazioni utilizzabili all'interno di Unity.

### Classi principali
1. **BVH2ClipEditor**: Fornisce un'interfaccia utente nell'editor di Unity per configurare e avviare il processo di conversione.
2. **BVHConverter**: Gestisce la logica effettiva per la conversione dei file BVH in `AnimationClip`.

## Classe BVH2ClipEditor
La classe `BVH2ClipEditor` fornisce una finestra nell'editor di Unity per configurare e avviare la conversione dei file BVH in `AnimationClip`. Questa finestra consente agli utenti di selezionare una cartella contenente i file BVH, di specificare la directory di output e di scegliere se rispettare il tempo di frame definito nel file BVH o di utilizzare un frame rate personalizzato.

- **Descrizione**:
  - Fornisce una finestra dell'editor accessibile dal menu "Tools > BVH to AnimationClip".
  - Permette di selezionare una cartella contenente file BVH e di visualizzare il percorso della cartella di output.
  - Consente di scegliere tra rispettare il tempo di frame del file BVH originale (`respectBVHTime`) o impostare un frame rate personalizzato (`frameRate`).
- **Utilizzo**:
  - Per avviare la finestra del convertitore BVH, l'utente deve selezionare "Tools > BVH to AnimationClip" dal menu di Unity.
  - Dopo aver selezionato la cartella BVH, l'utente può avviare la conversione premendo il pulsante "Convert BVH to AnimationClip".

## Classe BVHConverter
La classe `BVHConverter` si occupa della logica di conversione dei file BVH in `AnimationClip`. Prende in ingresso una serie di parametri dalla finestra dell'editor, come il percorso dei file BVH, la directory di salvataggio e il frame rate. Questa classe è responsabile della creazione delle clip di animazione e della gestione delle curve di posizione e rotazione per ogni osso.

- **Descrizione**:
  - Prende in ingresso una cartella contenente file BVH e una directory di output per le animazioni convertite.
  - Converte ogni file BVH in un `AnimationClip` utilizzabile in Unity.
  - Utilizza la classe `BVHParser` per estrarre i dati necessari per la conversione.
- **Metodo principale**:
  ```csharp
  public void ConvertBVHToAnimationClip()
  ```
  Questo metodo gestisce la lettura dei file BVH, la creazione delle `AnimationClip`, l'aggiunta delle curve per la posizione e la rotazione, e infine il salvataggio delle clip nella directory specificata.
- **Funzionalità principali**:
  - **Percorsi dei file**: Legge i file BVH dalla cartella specificata e salva le animazioni nella directory di output.
  - **Conversione frame**: Se `respectBVHTime` è abilitato, la classe utilizza il tempo di frame del file BVH. Altrimenti, viene utilizzato un frame rate personalizzato specificato dall'utente.
  - **Rotazioni e posizioni**: Utilizza i dati estratti per creare curve di animazione per le posizioni e rotazioni locali di ogni osso.
  - **Salvataggio dei file**: Dopo aver creato l'`AnimationClip`, il file viene salvato nella directory specificata utilizzando `AssetDatabase.CreateAsset()`.

### Metodo `GetCurves`
Il metodo `GetCurves` viene utilizzato per generare le curve di animazione per ogni giunto del personaggio.
- **Descrizione**:
  - Prende in ingresso il percorso del giunto, il nodo del parser BVH (`BVHBone`), e l'`AnimationClip` su cui aggiungere le curve.
  - Estrae i valori di posizione e rotazione dai canali del file BVH e li converte in curve di animazione che vengono poi applicate alla clip.
- **Rotazione con `fromEulerZXY`**: Le rotazioni vengono convertite utilizzando l'ordine ZXY, per garantire che i dati delle rotazioni BVH siano correttamente rappresentati come `Quaternion` in Unity.

```csharp
private void GetCurves(string path, BVHParser.BVHBone node, AnimationClip clip)
```
Questo metodo aggiunge le curve di posizione e rotazione per ogni giunto all'`AnimationClip`, permettendo la creazione di animazioni realistiche.

## Sommario
- **Interfaccia utente**: La classe `BVH2ClipEditor` fornisce un'interfaccia grafica nell'editor di Unity per facilitare l'importazione e la conversione dei file BVH.
- **Conversione animazioni**: La classe `BVHConverter` gestisce l'intero processo di conversione, dalla lettura dei file BVH, all'estrazione dei dati, fino alla creazione delle animazioni.

Questi file lavorano insieme per permettere agli utenti di convertire facilmente file BVH in `AnimationClip` e utilizzarli nei loro progetti Unity, consentendo di importare animazioni realistiche da fonti esterne.