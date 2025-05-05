# DebugDataVisualization

Questo modulo è dedicato alla **visualizzazione dei dati di animazione** applicati a un modello SMPL umanoide all'interno di Unity. L’obiettivo principale è verificare che i dati numerici estratti da un `AnimationClip`, generato dal modello **MoMask**, siano correttamente interpretati e restituiti in forma visiva coerente.

## Contenuto del modulo

### `SkeletonMapper.cs`
Questa classe funge da **mappatura tra i nomi logici dei giunti umanoidi e i loro `Transform` nella scena**. Contiene riferimenti ai principali body part del modello (pelvis, spine, arti, testa) e inizializza una lista ordinata (`bodyPart`) utile per accedere ai giunti in modo sistematico. È usata come supporto strutturale da altre classi.

### `PlayAnimationOnHumanoid.cs`
Questa classe gestisce la **riproduzione visiva di un'animazione**. Riceve in input un set di rotazioni articolari (ad esempio da un manager come `HumanoidCloningManager`), le applica al modello umanoide tramite i `Transform` contenuti in `SkeletonMapper`, e sincronizza il playback con un framerate specifico.

- ✅ Consente la **visualizzazione frame-by-frame** di animazioni articolari.
- ✅ Utile per confrontare i dati di animazione con il risultato visivo.

### `DrawHumanoidSkeletonGizmos.cs`
Questa classe disegna con i **Gizmos la struttura scheletrica** del manichino nella scena, collegando visivamente i vari giunti. È pensata per fornire un riscontro rapido della **topologia e della coerenza della struttura articolare**.

- ✅ Funziona solo in modalità Editor.
- ✅ Utile per debugging della struttura scheletrica.

---

## Utilizzo

1. Assicurati che il tuo GameObject (modello SMPL) abbia un componente `SkeletonMapper` con i `Transform` assegnati correttamente.
2. Aggiungi il componente `PlayAnimationOnHumanoid` per riprodurre un'animazione caricata da un manager esterno.
3. (Facoltativo) Aggiungi `DrawHumanoidSkeletonGizmos` per visualizzare la struttura scheletrica tramite `Gizmos`.

---

## Finalità

Il sistema `DebugDataVisualization` nasce per supportare:
- la validazione visiva di dati di animazione estratti in fase di preprocessing;
- il debugging di struttura e movimento di modelli umanoidi;
- l’integrazione di tool di animazione basati su AI (es. Behavior Cloning, RL).

---

## Dipendenze

- Richiede `HumanoidCloningManager` per il caricamento delle animazioni (vedi `AgentBehavior`).