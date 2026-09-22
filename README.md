<p align="center">
  <img src="BuranUI/Assets/Buran.png" alt="Buran" width="168">
</p>

<h1 align="center">Buran Music Library Manager</h1>

<p align="center">
  <strong>Musikbibliothek verwalten, Tags korrigieren und hören.</strong><br>
  Desktop-App für Linux (Fedora / Bazzite) und Windows.<br>
  <em>A desktop music library manager: ID3 tags, a personal catalog, and playback with spectrogram.</em>
</p>

<p align="center">
  <strong>Deutsch</strong>
  ·
  <a href="README.en.md">English</a>
  ·
  <a href="README.ru.md">Русский</a>
  ·
  <a href="README.uz.md">O'zbekcha</a>
</p>

<p align="center">
  <a href="https://github.com/88Radium/BuranMusicLibraryManager/releases">Download</a>
  ·
  <a href="#installation">Installation</a>
  ·
  <a href="#funktionen">Funktionen</a>
  ·
  <a href="#selbst-bauen">Selbst bauen</a>
</p>

---

Buran ist für Leute, die eine große MP3-/FLAC-Sammlung in Ordnung halten wollen: einheitliche Künstlernamen, saubere Dateinamen, Genres und Moods, und nebenbei abspielen. Der **eigene Katalog** ist die Wahrheit — nicht MusicBrainz, nicht iTunes. Was du einmal festlegst (bevorzugte Schreibweise, Alternative, Blockliste), wendet Buran beim nächsten Ordner automatisch an.

Änderungen an Tags werden **sofort in die Datei geschrieben**. Es gibt keinen extra Speichern-Knopf.

| | |
|---|---|
| Dateien | MP3 und FLAC |
| Plattformen | Linux (RPM, u. a. Fedora / Bazzite) und Windows |
| Player | libVLC ist im Paket enthalten |
| Sprachen | Deutsch, Englisch, Russisch, Usbekisch (lateinisch), Systemsprache |
| Daten | Katalog und Einstellungen liegen im Benutzerordner, nicht in der Musikbibliothek |

In der App selbst steht dieselbe Anleitung unter **Einstellungen → Anleitung**.

## Inhaltsverzeichnis

- [Installation](#installation)
- [Erster Start](#erster-start)
- [Typische Probleme](#typische-probleme)
- [Funktionen](#funktionen)
  - [Einstellungen](#einstellungen)
  - [Bibliothek](#bibliothek)
  - [ID3-Editor](#id3-editor)
  - [Neue Werte für die Datenbank](#neue-werte-für-die-datenbank)
  - [Player](#player)
  - [DB-Editor](#db-editor)
  - [Katalogmodell](#katalogmodell)
- [Wo Buran Daten speichert](#wo-buran-daten-speichert)
- [Releases auf GitHub](#releases-auf-github)
- [Selbst bauen](#selbst-bauen)

## Installation

Fertige Installer liegen unter **[Releases](https://github.com/88Radium/BuranMusicLibraryManager/releases)**.

### Linux (Fedora, RHEL, Bazzite)

1. Die `.rpm` des gewünschten Releases herunterladen (z. B. `buran-0.1.6-4.x86_64.rpm`).
2. Auf **Bazzite / rpm-ostree**:

```bash
sudo rpm-ostree uninstall buran --install /pfad/zu/buran-*.x86_64.rpm
sudo systemctl reboot
```

Erstinstallation ohne vorheriges Paket:

```bash
sudo rpm-ostree install /pfad/zu/buran-*.x86_64.rpm
sudo systemctl reboot
```

3. Starten über den Starter **Buran Music Library Manager** oder den Befehl `buran`.

Nicht direkt `/usr/lib/buran/BuranUI` aufrufen. Der Wrapper setzt `LD_LIBRARY_PATH` und `VLC_PLUGIN_PATH`, damit der Player die mitgelieferten VLC-Bibliotheken findet.

Debian/Ubuntu: zusätzlich liegt ein `.deb` am Release. Das ist ein Beipack; die Linux-Bibliotheken im Paket stammen von Fedora.

### Windows

1. `Buran-<Version>-win-x64-setup.exe` herunterladen und ausführen.
2. Die Installationssprache wählen: Deutsch, Englisch, Russisch oder Usbekisch (lateinisch).
3. Die App landet unter `%LOCALAPPDATA%\Programs\Buran` und im Startmenü.
4. Alternativ: das portable `.zip` entpacken und `BuranUI.exe` starten.

Windows kann beim ersten Start SmartScreen zeigen (die Datei ist nicht digital signiert). Dann **Weitere Informationen → Trotzdem ausführen**.

libVLC und `ffmpeg.exe` (für das Spektrogramm) sind im Windows-Paket enthalten. Ein extra VLC- oder ffmpeg-Install ist nicht nötig.

## Erster Start

1. Buran öffnen.
2. Links **Ordner hinzufügen** und den Wurzelordner der Musikbibliothek wählen (z. B. `Musik`).
3. Im Baum einen Unterordner mit blauem Punkt anklicken — das sind Ordner, in denen MP3/FLAC liegen.
4. Die Dateien erscheinen im Reiter **ID3-Editor**.
5. Oben **Tags bearbeiten**, um den Inspector und die Sammelaktionen einzublenden.

Mehrere Wurzelordner sind möglich (z. B. eine interne Platte und eine NAS-Freigabe). Die Liste merkt sich Buran.

## Typische Probleme

Welche Funktion hilft bei welchem Durcheinander:

| Problem | Funktion |
|---|---|
| Derselbe Künstler in fünf Schreibweisen (`Eminem`, `EMINEM`, `M&M`) | Katalog: **bevorzugter Name** plus **Alternativen**. Beim nächsten Ordneröffnen ersetzt Buran die Varianten automatisch. |
| `feat. Dido` landet im Kommentar statt als Künstler | **ID3 aus Dateiname**; Trenner unter **Schlüsselwörter → Zusammenarbeit** im DB-Editor. |
| Tags leer oder falsch, Dateinamen aber sauber | **ID3 aus Dateiname** (einzeln oder als Sammelaktion). |
| Dateinamen wild, Tags stimmen | **Dateiname aus ID3**. Schema: `Künstler - Titel`. |
| Zwei Dateien würden denselben Namen bekommen | **Dateien vergleichen** (Größe, Dauer, Bitrate, …) und behalten oder löschen. |
| ID3-Kommentare voller Player-Müll | Sammelaktion **Kommentare** — denselben Text setzen oder leeren. |
| Ein Genre nur bei manchen Dateien im Ordner | Sammeldialog **Genres**: × entfernt nur dort, wo es vorkommt; Hinzufügen setzt es auf **jede** angehakte Datei. |
| Album mit `CD1`/`CD2` in Unterordnern | **Unterordner einbeziehen**, dann den Albumordner anklicken. |
| Alle Titel eines Künstlers über die ganze Sammlung | **Bibliothek indexieren**, im DB-Editor Rechtsklick → **Titel anzeigen**. |
| Ist eine „192 kHz“-Datei wirklich Hi-Res? | **Spektrogramm**: Energie oberhalb von 16 kHz sichtbar oder tot. |
| Versehentlich umbenannt oder Tags zerschossen | Im Bearbeitungsmodus **Reset** — Tags **und** Dateiname zurück auf den Stand zu Beginn dieser Session. |
| Müllnamen nie wieder vorschlagen | Import-Dialog oder DB-Editor: **Blockieren** (gilt nur für die gewählte Art: Künstler, Genre oder Mood). |

## Funktionen

### Einstellungen

Oben rechts, Knopf **Einstellungen**.

| Einstellung | Was sie macht | Wozu | So nutzt du sie |
|---|---|---|---|
| **Sprache** | Stellt Oberfläche und die eingebaute Anleitung um. | Deutsch, Englisch, Russisch, Usbekisch (lateinisch) oder die Systemsprache. | Ausklappen, Sprache wählen. Wirkt sofort. |
| **Schriftgröße** | Klein / Mittel / Groß. | Lange Tag-Listen und 4K-Monitore. | Ausklappen, Größe wählen. |
| **Transparenz** | Deckkraft des Fensters (ab 40 %). | Desktop durchscheinen lassen. | Schieberegler. |
| **Anleitung** | Öffnet das eingebaute Handbuch. | Dieselbe Funktionsübersicht, ohne GitHub. | Knopf **Anleitung**. |
| **Version** | Zeigt die installierte Versionsnummer. | Prüfen, ob das Paket aktuell ist. | Nur Anzeige, unten im Einstellungsmenü und auf dem Startbildschirm. |

### Bibliothek

Die linke Spalte ist der Ordnerbaum deiner Sammlung.

#### Ordner hinzufügen

**Was:** Nimmt einen Ordner als Wurzel der Bibliothek.  
**Wozu:** Du siehst die Struktur wie im Dateimanager, ohne jedes Mal neu wählen zu müssen.  
**So geht’s:**

1. **Ordner hinzufügen**.
2. Den obersten Musikordner wählen.
3. Im Baum auf- und zuklappen.

#### Entfernen

**Was:** Nimmt die gewählte Wurzel aus der Liste. Die Musikdateien auf der Platte bleiben.  
**Wozu:** Eine alte USB-Platte oder ein umbenannter Pfad soll nicht mehr auftauchen.  
**So geht’s:** Wurzel markieren → **Entfernen**, oder Rechtsklick im Baum.

#### Aktualisieren

**Was:** Liest den Baum neu ein.  
**Wozu:** Nachdem du Ordner außerhalb von Buran angelegt oder umbenannt hast.  
**So geht’s:** **Aktualisieren**.

#### Unterordner einbeziehen

**Was:** Beim Öffnen eines Ordners werden auch Dateien in Unterordnern geladen.  
**Wozu:** Ein Album mit `CD1` / `CD2`, oder „alles unter Metal auf einmal sehen“.  
**So geht’s:** Häkchen setzen, dann den Ordner im Baum anklicken.

#### Bibliothek indexieren

**Was:** Schreibt Titel, Künstler, Album, Pfad usw. in den lokalen Katalogindex.  
**Wozu:** Im DB-Editor **Titel anzeigen** — alle indizierten Stücke eines Künstlers, Genres oder Moods, inkl. Ordnerpfad.  
**So geht’s:**

1. Mindestens eine Wurzel hinzufügen.
2. **Bibliothek indexieren**.
3. Später im DB-Editor Rechtsklick → **Titel anzeigen**.

#### Blauer Punkt

Ein Unterordner mit Punkt enthält Audio-Dateien. Ein Klick lädt sie in den ID3-Editor.

### ID3-Editor

Oberer Arbeitsbereich. Hier lebst du, wenn Tags und Dateinamen stimmen sollen.

#### Die Tabelle

**Was:** Eine kompakte Liste aller Dateien im gewählten Ordner: Titel, Künstler, Ordnerpfad, Album, Jahr, Dauer, Bitrate, Abtastrate, Bit-Tiefe.  
**Wozu:** Überblick, Sortieren, die richtige Datei finden, ohne jede Datei zu öffnen.  
**So geht’s:**

1. Links einen Ordner mit Audio wählen.
2. Eine Zeile anklicken — das ist der **fokussierte Titel** (Inspector, Reset, Abspielen per Doppelklick).
3. Das **Häkchen** in der ersten Spalte ist die **Mehrfachauswahl** für Sammelaktionen. Fokus und Häkchen sind unabhängig: du kannst Titel A ansehen und B+C angehakt haben.

**Spalten**

- **Spalten** oben: Häkchen setzen oder entfernen blendet Spalten ein und aus.
- Überschriften ziehen ändert die Spaltenreihenfolge, Ziehen am Rand die Breite. Doppelklick auf den Trenner passt die linke Spalte dem Inhalt an.
- Klick auf eine Überschrift sortiert die Zeilen, nochmal klicken dreht die Richtung. Ein blasses Pfeilpaar an der Überschrift zeigt, dass sie sortierbar ist.
- **Dateiname**, **Kommentar**, **Genre** und **Mood** sind standardmäßig aus, weil die Tabelle sonst zu breit wird.

**Rechtsklick → Ordner öffnen**

Wählt links den Ordner dieser Datei und listet alle Stücke darin. Nützlich nach **Titel anzeigen** aus dem DB-Editor, wenn die Treffer über viele Ordner verteilt sind.

**Filter weg**

Wenn die Liste gefiltert ist (z. B. „Titel dieses Künstlers“), lädt der Knopf wieder den aktuell gewählten Bibliotheksordner.

**Doppelklick** spielt den fokussierten Titel (wenn der Player geladen ist).

Oben in der Leiste **immer** (nicht nur im Bearbeitungsmodus): **Alle / Keine**, **Abspielen** und **Zur Playlist** (Player muss geladen sein). Sammelaktionen für Tags und Dateinamen erscheinen erst nach **Tags bearbeiten**.

#### Tags bearbeiten (Bearbeitungsmodus)

**Was:** Blendet rechts den Inspector ein und oben die Sammelleiste.  
**Wozu:** Ohne diesen Modus bleibt die Tabelle schlank zum Stöbern. Mit ihm kannst du Tags ändern, umbenennen, zurücksetzen.  
**So geht’s:**

1. **Tags bearbeiten** — der Knopf heißt danach **Bearbeitung**.
2. Den Splitter zwischen Liste und Inspector ziehen, wenn du mehr Platz für Tags brauchst.
3. Nochmal klicken, um den Modus zu verlassen.

Beim **Start** des Bearbeitungsmodus merkt sich Buran pro Titel den aktuellen Stand (Tags **und** Dateiname). Beim **Verlassen** wird der dann gespeicherte Stand die neue Basis.

#### Inspector — ein Titel

Der Inspector gilt immer für den **fokussierten** Titel (angeklickte Zeile), nicht für alle Häkchen.

| Feld / Knopf | Was er macht | Wozu | So nutzt du ihn |
|---|---|---|---|
| **Titel, Album, Jahr, Kommentar** | Schreibt den Wert sofort in die Datei. | Tippfehler, fehlendes Jahr, Müll in Kommentaren. | Feld anklicken, Text ändern, Fokus verlassen — fertig. |
| **Künstler / Genres / Moods** | Listen mit × zum Entfernen und + / Enter zum Hinzufügen. Vorschläge kommen aus dem Katalog. | Mehrere Künstler (`feat.`), mehrere Genres, Stimmung getrennt vom Genre. | Namen tippen, aus der Liste wählen oder neu anlegen, + oder Enter. |
| **Name →** (ID3 → Dateiname) | Baut `Künstler - Titel.mp3` aus den Tags und benennt die Datei um. Bevorzugte Katalognamen werden verwendet. Zwei Künstler werden zu `A feat. B`, mehr zu `A feat. B, C & D`. | Dateiname und Tags sollen zusammenpassen. | Titel fokussieren, **Name →**. Die Zeile bleibt ausgewählt. |
| **← Name** (Dateiname → ID3) | Zerlegt den Dateinamen mit Mustern (Künstler, Titel, Album, Jahr, Live/Remix …) und schreibt die Tags. | Du hast saubere Namen, aber leere oder falsche Tags. | Titel fokussieren, **← Name**. |
| **Reset** | Stellt Tags **und** Dateiname auf den Stand **zu Beginn dieser Bearbeitungssession** zurück. | Versehentlich umbenannt oder Tags zerschossen, auch nach einem Sprung zu einem anderen Titel und wieder zurück. | Im selben Bearbeitungsmodus bleiben, Titel wieder fokussieren, **Reset**. |

Unbekannte Namen, die nicht blockiert sind, werden als bevorzugter Katalogeintrag angelegt. Blockierte Werte kommen auf die Datei, aber nicht in die Datenbank.

Beim **Öffnen eines Ordners** ersetzt Buran alternative Schreibweisen in den Dateien durch den bevorzugten Katalognamen und speichert das.

#### Sammelaktionen (angehakte Dateien)

Nur im Bearbeitungsmodus, nur für Zeilen mit Häkchen.

| Knopf | Was er macht | Wozu | So geht’s |
|---|---|---|---|
| **Künstler / Genres / Moods** | Öffnet den Sammeldialog. Die Liste ist die **Vereinigung** aller ausgewählten Dateien. | „Pop“ nur dort löschen, wo es vorkommt; „Freestyle“ an **jede** Datei hängen. | Häkchen setzen → Knopf → Einträge × oder hinzufügen → **Übernehmen**. |
| **Kommentare** | Derselbe Text auf alle ausgewählten Dateien, oder alle Kommentare leeren. | ID3-Kommentare voller Player-Müll. | Text eingeben und **Übernehmen**, oder Feld leer lassen / **Leeren**. |
| **Dateiname aus ID3** | Wie **Name →**, aber für alle Häkchen. | Einen Ordner nach dem Tag-Schema umbenennen. | Häkchen → Knopf. |
| **ID3 aus Dateiname** | Wie **← Name**, aber für alle Häkchen. | Tags aus sauberen Dateinamen für den ganzen Ordner. | Häkchen → Knopf. |

**Sammeldialog Künstler / Genres / Moods im Detail**

1. Dateien anhäken.
2. **Künstler**, **Genres** oder **Moods**.
3. × entfernt den Eintrag **nur dort, wo er vorkommt**. Die andere Datei behält ihn.
4. Einen Namen hinzufügen (oder den Hinweis **alle**) setzt ihn auf **jede** ausgewählte Datei — auch wenn er in der Vereinigungsliste schon stand. So kannst du ein Genre erst aus der Liste nehmen und bewusst wieder allen zuweisen.
5. **Aus Auswahl laden** baut die Liste neu, falls du zwischendurch Dateien umgehakt hast.
6. **Übernehmen** schreibt die Tags.

#### Dateiname aus ID3 — Namenskonflikt

Existiert die Zieldatei schon, öffnet sich **Dateien vergleichen**: Größe, Änderung, Dauer, Bitrate, Abtastrate, Kanäle, Bit-Tiefe, Format.

| Knopf | Wirkung |
|---|---|
| **Beide behalten** | Umbenennung abbrechen. Beide Dateien bleiben. |
| **Diese Datei behalten** | Die vorhandene Datei wird gelöscht, die aktuelle umbenannt. |
| **Vorhandene behalten** | Die aktuelle Datei wird gelöscht, die vorhandene bleibt. |

#### ID3 aus Dateiname — wie der Parser denkt

- Muster und Schlüsselwörter zerlegen den Namen in Künstler, Titel, Album, Jahr und Versionshinweise.
- Komma und Semikolon trennen immer.
- `feat.` / `ft.` / `featuring` — auch in Klammern, z. B. `Eminem - Stan (feat. Dido).mp3` — werden **weitere Künstler**, nicht Kommentar.
- `(Live)` oder `[Remix]` bleiben Kommentar.
- Bekannte Katalognamen und Einbuchstaben-Gruppen wie `D & F` werden nicht in `D` und `F` zerschnitten.
- Weitere Trenner pflegst du im DB-Editor unter **Schlüsselwörter**.

### Neue Werte für die Datenbank

**Was:** Dialog nach dem Laden eines Ordners, wenn Tags oder Dateinamen Namen enthalten, die noch nicht im Katalog stehen und nicht blockiert sind.  
**Wozu:** Du entscheidest einmal, ob „Gwen Stefani“ ein neuer Künstler ist, eine alternative Schreibweise von etwas Bekanntem, oder Müll, den du nie wieder sehen willst.  
**So geht’s:**

1. Ordner öffnen. Der Dialog kommt nur, wenn es Unbekanntes gibt.
2. Pro Eintrag Häkchen: übernehmen oder weglassen.
3. **Bevorzugter Name** — neuer Katalogeintrag (die Schreibweise, die in Dateien landen soll).
4. **Alternativname** — Tippfehler oder andere Schreibweise eines vorhandenen Eintrags. Beim Künstler kannst du den bevorzugten Namen bei Bedarf mit anlegen.
5. **Blockieren** — nie wieder vorschlagen. Genre `Happy` blockiert **nicht** Mood `Happy`.
6. Unten: schon blockierte Werte **Freigeben**, dann erscheinen sie wieder oben zum Übernehmen.
7. **Übernehmen** schreibt nur angehakte Einträge. **Überspringen** schließt ohne Katalogänderung.

### Player

Eigenes Modul. Spielt MP3 und FLAC über **libVLC** (im Paket). Sitzt als Dock unter der Editor-Liste (Splitter zum Vergrößern) und zusätzlich als Reiter **Player**.

| Funktion | Was sie macht | Wozu | So geht’s |
|---|---|---|---|
| **Play / Pause / Stop / Zurück / Weiter** | Transport. | Normales Hören. | Leiste unter der Liste oder im Player-Reiter. |
| **Position / Lautstärke** | Suchen und Pegel. | Eine Stelle anspringen. | Schieberegler. |
| **Spektrogramm** | Frequenz über der Zeit, einmal berechnet (ähnlich Spek). Achsen: Hz, Zeit, dB bis zur Nyquist-Frequenz der Datei. | Sehen, ob eine „192 kHz“-Datei oberhalb von 16 kHz überhaupt Energie hat; Rauschen und Cuts erkennen. | Klick oder Ziehen sucht in der Datei. Splitter ändert die Höhe. |
| **Ordner-Warteschlange** | Linke Liste im Player-Reiter: Dateien des Bibliotheksordners. | Den Ordner durchhören. | Doppelklick spielt, **Zur Playlist** übernimmt. |
| **Playlists** | Rechte Liste: benannte Listen, die Ordner übergreifen dürfen. | Mixes, „zu taggen“, Favoriten. | **Neu**, Name setzen, Titel hinzufügen. Umbenennen im Namensfeld. **Löschen**. |
| **M3U importieren / exportieren** | Absolute Pfade. Fehlende Dateien bleiben markiert. | Austausch mit anderen Playern. | Import/Export im Player-Reiter. |
| **Repeat** | Aus / alle / einer / einmal. | Loop oder nach dem Titel Schluss. | Repeat-Knopf in der Leiste. |
| **Player und Tabs tauschen** | Dock oben oder unten. | Mehr Platz für die Liste oder für das Spektrogramm. | Tausch-Knopf. |

Wiedergabe aus einer **Playlist** läuft weiter, wenn du den Ordner wechselst. Die **Ordner-Warteschlange** stoppt, wenn der aktuelle Titel im neuen Ordner nicht mehr vorkommt.

Ohne Player-Modul bleibt der ID3-Editor voll nutzbar.

### DB-Editor

Zweiter Reiter. Hier liegt der **gesamte Katalog**: anlegen, umbenennen, als Alternative zuordnen, blockieren, freigeben. Alles, was der Import-Dialog kann, kannst du hier dauerhaft pflegen.

#### Suche und Aktualisieren

- Das Suchfeld filtert Künstler, Alternativnamen und Schlüsselwörter.
- **Suche leeren** setzt den Filter zurück.
- **Aktualisieren** lädt den Katalog neu.

#### Künstler

**Was:** Liste mit bevorzugtem Namen und optional bürgerlichem Namen (`Marshall Mathers` zu `Eminem`).  
**Wozu:** Eine kanonische Schreibweise, unter der alle Varianten zusammenlaufen.  
**So geht’s:**

- Unten **bevorzugten Namen** eintragen, optional bürgerlichen Namen, **Hinzufügen**.
- **Alternativname:** getippten Namen einem vorhandenen Künstler zuordnen. Ins Feld tippen filtert (z. B. `E` → Eminem).
- **Blockieren:** getippten Namen ausblenden. Steht er schon im Katalog, wird er entfernt. Bei einem bevorzugten Namen werden auch seine Alternativen blockiert.
- Rechtsklick: **Entfernen**, **Blockieren**, **Auswahl aufheben**, **Titel anzeigen** (indizierte Stücke im ID3-Editor).

#### Alternative Namen

Spalte aller Alternativen oder nur die des ausgewählten Künstlers. Hinzufügen braucht einen ausgewählten Künstler. Entfernen und Blockieren per Rechtsklick oder Buttons.

#### Mitgliedschaften

Nur mit ausgewähltem Künstler. Mitglieder dieser Gruppe bzw. Gruppen, zu denen der Künstler gehört (`Eminem` in `D12`). Namen müssen bereits als Künstler existieren. Eine Gruppe kann nicht Mitglied von sich selbst sein.

#### Genres und Moods

Gleiche Bedienung, getrennte Listen. Moods sind Stimmungen (`Happy`, `Dark`), keine Ersatz-Genres.

- Häkchen: Genre oder Mood dem **ausgewählten Künstler** zuordnen (welches Repertoire hat dieser Act).
- Rechtsklick: Entfernen, Blockieren, Auswahl aufheben, **Nach Auswahl filtern** (Künstlerliste), Filter aufheben, **Titel anzeigen**.
- Alternative Schreibweisen: Varianten des ausgewählten Eintrags (`Danc` → `Dance`).
- Unten: bevorzugten Namen anlegen, Alternativname zuordnen, Blockieren.

#### Schlüsselwörter

Trennzeichen und Erkennungswörter für Dateinamen und Tags.

| Typ | Zweck | Beispiele |
|---|---|---|
| **Zusammenarbeit** | Splittet Künstler. | `feat`, `ft`, `vs`, `with`, `and`, `&`, `+`, `/` |
| **Version** | Erkennt Hinweise, die Kommentar bleiben. | `Live`, `Remix`, `Remaster` |

Komma und Semikolon bleiben immer Trenner. `&` kannst du hier entfernen, wenn ein Künstlername `D & F` nicht zerlegt werden soll.

#### Blockierte Werte

Untere Leiste, getrennt nach Künstler, Genre und Mood. **Freigeben** erlaubt den Namen wieder als Vorschlag. Der Katalogeintrag kommt dadurch nicht von allein zurück — den legst du danach bewusst wieder an.

Ein späteres Hinzufügen derselben Schreibweise ins Katalog hebt die Blockierung für genau diese Schreibweise auf.

### Katalogmodell

Drei getrennte Arten: **Künstler**, **Genre**, **Mood**.

| Art | Bedeutung |
|---|---|
| **Bevorzugter Name** | Die Schreibweise, die in Dateien landen soll. |
| **Alternative** | Andere Schreibweise desselben Eintrags. Wird beim Laden eines Ordners automatisch ersetzt. |
| **Blockiert** | Erscheint nicht im Import-Dialog und wird nicht still in den Katalog übernommen. |

Blockieren gilt nur für die gewählte Art. Dieselbe Zeichenkette kann als Genre blockiert und als Mood erlaubt sein.

## Wo Buran Daten speichert

Musikdateien rührt Buran nur an, wenn du Tags schreibst oder umbenennst. Katalog und Einstellungen liegen getrennt:

| System | Ort |
|---|---|
| Linux | `~/.local/share/Buran` |
| Windows | `%LOCALAPPDATA%\Buran` |

Die SQLite-Datei heißt historisch `CerberusMusicManager.db`. Deinstallation der App lässt diesen Ordner in Ruhe, damit der Katalog erhalten bleibt.

## Releases auf GitHub

Jedes Release mit neuer Versionsnummer in `Directory.Build.props` erzeugt nach einem Push auf `master` automatisch:

- Linux `.rpm` (und `.deb`)
- Windows `setup.exe` und portable `.zip`

Reine Code-Pushes ohne Versionsbump kompilieren nur (grüner Haken **Build**). Installer entstehen, wenn die Version neu ist, sich `packaging/` bzw. der Package-Workflow ändert, oder du unter **Actions → Package → Run workflow** von Hand startest.

## Selbst bauen

Voraussetzung: [.NET 10 SDK](https://dotnet.microsoft.com/download) (siehe `global.json`).

```bash
dotnet build BuranMusicLibraryManager.sln -c Release
```

In JetBrains Rider die Solution öffnen und ganz normal starten / kompilieren.

Linux-Pakete auf einer Fedora-/Bazzite-Maschine (VLC-Bibliotheken werden mit eingepackt):

```bash
bash packaging/pack.sh linux
```

Ergebnis unter `dist/` (gitignored), z. B. `buran-<Version>-4.x86_64.rpm`.

In Rider dieselben Schritte über **Run → Run…**:

| Konfiguration | Aktion |
|---|---|
| **Pack Linux** | `packaging/pack.sh linux` |
| **Install Linux RPM** | legt `dist/buran-<Version>-*.rpm` per rpm-ostree ein; vorhandenes buran wird ersetzt. Passwort im Rider-Terminal (nicht als Extra-Fenster). |
| **Pack + Install Linux RPM** | packen und danach installieren (inkl. Ersetzen) |

`packaging/rider-install-linux.sh` und `packaging/rider-pack-and-install-linux.sh` sind die Skripte hinter den beiden neuen Konfigurationen.

Windows-Setup entsteht in GitHub Actions (Inno Setup). Lokal auf Windows, wenn `iscc` im PATH liegt:

```bash
bash packaging/pack.sh windows
```

---

Buran schreibt Tags sofort, merkt sich deinen Katalog und spielt über gebündeltes libVLC. Wenn etwas unklar bleibt: in der App **Einstellungen → Anleitung**.
