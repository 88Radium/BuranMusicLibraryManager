# Buran — Anleitung

Buran verwaltet eine Musikbibliothek: ID3-Tags von MP3- und FLAC-Dateien bearbeiten, einen eigenen Katalog für Künstler, Genres und Moods pflegen und nebenbei abspielen. Änderungen an Tags werden sofort in die Datei geschrieben.

Der eigene Katalog ist die Wahrheit — nicht MusicBrainz, nicht iTunes. Bevorzugte Schreibweise, Alternativen und Blockliste greifen beim nächsten Ordneröffnen von selbst.

## Typische Probleme

- Uneinheitliche Künstlernamen (`Eminem`, `EMINEM`, `M&M`): Katalog mit bevorzugtem Namen plus Alternativen. Beim Ordneröffnen ersetzt Buran die Varianten automatisch.
- `feat. Dido` landet im Kommentar statt als Künstler: ID3 aus Dateiname; Trenner unter Schlüsselwörter → Zusammenarbeit im DB-Editor.
- Tags leer oder falsch, Dateinamen aber sauber: ID3 aus Dateiname (einzeln oder Sammelaktion).
- Dateinamen wild, Tags stimmen: Dateiname aus ID3. Schema `Künstler - Titel`.
- Zwei Dateien würden denselben Namen bekommen: Dateien vergleichen (Größe, Dauer, Bitrate, …) und behalten oder löschen.
- ID3-Kommentare voller Player-Müll: Sammelaktion Kommentare — denselben Text setzen oder leeren.
- Ein Genre nur bei manchen Dateien: Sammeldialog Genres. × entfernt nur dort, wo es vorkommt; Hinzufügen setzt es auf jede angehakte Datei.
- Album mit CD1/CD2 in Unterordnern: Unterordner einbeziehen, dann den Albumordner anklicken.
- Alle Titel eines Künstlers über die Sammlung: Bibliothek indexieren, im DB-Editor Rechtsklick → Titel anzeigen.
- Ist eine „192 kHz“-Datei wirklich Hi-Res? Spektrogramm: Energie oberhalb von 16 kHz sichtbar oder tot.
- Versehentlich umbenannt oder Tags zerschossen: im Bearbeitungsmodus Reset — Tags und Dateiname zurück auf den Stand zu Beginn dieser Session.
- Müllnamen nie wieder vorschlagen: Import-Dialog oder DB-Editor → Blockieren (gilt nur für die gewählte Art).

## Einstellungen

Oben rechts öffnet „Einstellungen“ ein Menü.

- Sprache: Deutsch, Englisch, Russisch, Usbekisch (lateinisch) oder Systemsprache. Oberfläche und diese Anleitung folgen der Wahl.
- Schriftgröße: Klein, Mittel oder Groß.
- Transparenz: Deckkraft des Hauptfensters (ab 40 %).
- Anleitung: dieses Handbuch.
- Version: installierte Versionsnummer (auch auf dem Startbildschirm).

## Bibliothek

Die linke Spalte ist der Ordnerbaum.

- Ordner hinzufügen: einen Wurzelordner zur Bibliothek nehmen. Mehrere Wurzeln sind möglich; die Liste wird gespeichert.
- Entfernen: die gewählte Wurzel aus der Liste nehmen. Dateien auf der Platte bleiben.
- Aktualisieren: Baum neu einlesen, z. B. nach Ordnern außerhalb von Buran.
- Unterordner einbeziehen: beim Öffnen eines Ordners auch Dateien in Unterordnern laden (Album mit CD1/CD2).
- Bibliothek indexieren: Titel, Künstler, Album, Pfad ins lokale Verzeichnis schreiben. Danach im DB-Editor Rechtsklick → Titel anzeigen.
- Ein Unterordner mit blauem Punkt enthält Audio-Dateien. Ein Klick darauf lädt sie in den ID3-Editor.
- Unterstützte Dateien: MP3 und FLAC.

## ID3-Editor

Oben in der Arbeitsfläche. Kompakte Tabelle (Titel, Künstler, Ordnerpfad, Album, Jahr, Dauer, Bitrate, Samplerate, Bit-Tiefe). Spalten über „Spalten“ ein- und ausblenden; Überschriften ziehen ändert die Reihenfolge. Dateiname, Kommentar, Genre und Mood sind standardmäßig aus. Ein Doppelklick auf den Trenner zwischen zwei Überschriften setzt die Spalte links davon auf die Breite des Inhalts, mindestens so breit wie die Überschrift. Ein Klick auf die Überschrift selbst sortiert die Zeilen; das blasse Pfeilpaar daneben zeigt das an.

Eine angeklickte Zeile ist der fokussierte Titel (Inspector, Reset, Doppelklick spielt). Das Häkchen in der ersten Spalte ist die Mehrfachauswahl. Fokus und Häkchen sind unabhängig.

Rechtsklick → Ordner öffnen wählt links den Ordner dieser Datei. Nützlich nach Titel anzeigen, wenn Treffer über viele Ordner verteilt sind.

„Filter weg“ nach „Titel anzeigen“ lädt wieder den ausgewählten Bibliotheksordner, oder die erste Wurzel, wenn keiner gewählt ist.

Ist der Player geladen, sitzen Transport und Spektrogramm unter der Liste (Splitter zum Vergrößern). Ohne Player bleibt der Editor allein funktionsfähig.

Oben in der Leiste immer: Alle / Keine, Abspielen und Zur Playlist (Player muss geladen sein). Sammelaktionen für Tags und Dateinamen nur im Bearbeitungsmodus.

### Tags bearbeiten

Blendet rechts den Inspector ein und oben die Sammelleiste. Beim Start merkt sich Buran pro Titel Tags und Dateiname; beim Verlassen wird der dann gespeicherte Stand die neue Basis.

### Inspector — ein Titel

Gilt für den fokussierten Titel, nicht für alle Häkchen.

- Titel, Album, Jahr, Kommentar: sofort in die Datei.
- Künstler, Genres, Moods: Listen mit × und + / Enter. Vorschläge aus dem Katalog.
- Name → baut `Künstler - Titel` aus den Tags (bevorzugte Katalognamen). Zwei Künstler: `A feat. B`, mehr: `A feat. B, C & D`.
- ← Name zerlegt den Dateinamen und schreibt die Tags.
- Reset stellt Tags und Dateiname auf den Stand zu Beginn dieser Bearbeitungssession zurück.

Unbekannte, nicht blockierte Namen werden als bevorzugter Katalogeintrag angelegt. Blockierte Werte kommen auf die Datei, aber nicht in die Datenbank.

Beim Öffnen eines Ordners ersetzt Buran alternative Schreibweisen durch den bevorzugten Katalognamen und speichert das.

### Sammelaktionen (angehakte Dateien)

Nur im Bearbeitungsmodus, nur für Zeilen mit Häkchen: Künstler, Genres, Moods, Kommentare, Dateiname aus ID3, ID3 aus Dateiname.

Die Liste im Sammeldialog ist die Vereinigung aller ausgewählten Dateien. × löscht nur dort, wo der Eintrag vorkommt. Hinzufügen oder „alle“ setzt ihn auf jede ausgewählte Datei.

Kommentare: ein Textfeld, Vorschau je Datei. Leeres Feld löscht den Kommentar.

### Dateiname aus ID3 und Konflikte

Existiert der Zielname bereits, öffnet sich „Dateien vergleichen“ (Größe, Änderung, Dauer, Bitrate, Abtastrate, Kanäle, Bit-Tiefe, Format).

- Beide behalten: Umbenennung abbrechen.
- Diese Datei behalten: vorhandene Datei löschen, diese umbenennen.
- Vorhandene behalten: diese Datei löschen.

### ID3 aus Dateiname

Muster und Schlüsselwörter zerlegen den Namen (Künstler, Titel, Album, Jahr, Live/Remix). Komma und Semikolon trennen immer.

„feat.“ / „ft.“ / „featuring“ — auch in Klammern, z. B. `Eminem - Stan (feat. Dido).mp3` — werden weitere Künstler, nicht Kommentar. `(Live)` oder `[Remix]` bleiben Kommentar.

Bekannte Katalognamen und Einbuchstaben-Gruppen wie „D & F“ werden nicht zerschnitten. Weitere Trenner stehen unter Schlüsselwörter im DB-Editor.

## Neue Werte für die Datenbank

Nach dem Laden eines Ordners, wenn Tags oder Dateinamen unbekannte, nicht blockierte Namen enthalten.

- Häkchen: übernehmen oder weglassen.
- Bevorzugter Name: neuer Katalogeintrag.
- Alternativname: Schreibweise eines vorhandenen Eintrags.
- Blockieren: nie wieder vorschlagen. Genre „Happy“ blockiert nicht Mood „Happy“.
- Unten: blockierte Werte freigeben, dann erscheinen sie wieder oben.
- Übernehmen schreibt nur angehakte Einträge. Überspringen schließt ohne Änderung.

## Player

Eigenes Modul. MP3 und FLAC über libVLC. Mit ID3-Editor docken Transport und Spektrogramm unter die Editor-Liste; der Player-Reiter behält Ordner-Warteschlange und Playlists.

- Play / Pause / Stop / Zurück / Weiter, Position, Lautstärke.
- Repeat: aus / alle / einer / einmal.
- Player und Tabs tauschen: Dock oben oder unten.
- Spektrogramm (einmal berechnet, ähnlich Spek). Achsen: Hz, Zeit, dB bis zur Nyquist-Frequenz. Klick oder Ziehen sucht. Damit siehst du, ob eine 192-kHz-Datei oberhalb von 16 kHz Energie hat.
- Linke Liste: Dateien des Bibliotheksordners. Doppelklick spielt, Zur Playlist übernimmt.
- Rechte Liste: benannte Playlists über Ordnergrenzen. Neu, umbenennen, löschen.
- M3U importieren und exportieren (absolute Pfade). Fehlende Dateien bleiben markiert.
- Vom ID3-Editor: Abspielen und Zur Playlist für die Auswahl.

Wiedergabe aus einer Playlist läuft weiter, wenn du den Ordner wechselst. Die Ordner-Warteschlange stoppt, wenn der aktuelle Titel nicht mehr im neuen Ordner liegt.

## DB-Editor

Zweiter Reiter. Gesamter Katalog: anlegen, umbenennen, als Alternative zuordnen, blockieren, freigeben.

### Suche und Aktualisieren

Suche filtert Künstler, Alternativnamen und Schlüsselwörter. Suche leeren setzt sie zurück. Aktualisieren lädt den Katalog neu.

### Künstler

Liste mit bevorzugtem Namen und bürgerlichem Namen. Rechtsklick: Entfernen, Blockieren, Auswahl aufheben, Titel anzeigen (indizierte Titel inkl. Ordnerpfad).

Unten hinzufügen: bevorzugter Name (optional bürgerlicher Name), Alternativname einem vorhandenen Künstler zuordnen, oder Blockieren. Tippen ins Feld filtert (z. B. „E“).

Bei einem bevorzugten Namen werden auch seine Alternativen blockiert.

### Alternative Namen

Spalte aller Alternativen oder nur die des ausgewählten Künstlers. Hinzufügen braucht einen ausgewählten Künstler.

### Mitgliedschaften

Nur mit ausgewähltem Künstler. Mitglieder dieser Gruppe bzw. Gruppen, zu denen der Künstler gehört. Namen müssen bereits als Künstler existieren. Eine Gruppe kann nicht Mitglied von sich selbst sein.

### Genres und Moods

Gleiche Bedienung, getrennte Listen. Moods sind Stimmungen (`Happy`, `Dark`), keine Ersatz-Genres.

- Häkchen: dem ausgewählten Künstler zuordnen.
- Rechtsklick: Entfernen, Blockieren, Auswahl aufheben, Nach Auswahl filtern, Filter aufheben, Titel anzeigen.
- Alternative Schreibweisen des ausgewählten Eintrags.
- Unten: bevorzugten Namen anlegen, Alternativname zuordnen, Blockieren.

### Schlüsselwörter

- Zusammenarbeit: splittet Künstler (feat, ft, vs, with, and, sowie & + /).
- Version: erkennt Live, Remix, Remaster.

Komma und Semikolon bleiben immer Trenner. & kannst du hier entfernen, wenn „D & F“ nicht splitten soll.

### Blockierte Werte

Untere Leiste, getrennt nach Künstler, Genre und Mood. Freigeben erlaubt den Namen wieder als Vorschlag. Der Katalogeintrag kommt dadurch nicht von allein zurück.

Ein späteres Hinzufügen derselben Schreibweise hebt die Blockierung für genau diese Schreibweise auf.

## Katalogmodell

Drei Arten, jeweils getrennt: Künstler, Genre, Mood.

- Bevorzugter Name: die Schreibweise, die in Dateien landen soll.
- Alternative: andere Schreibweise desselben Eintrags. Wird beim Laden eines Ordners automatisch ersetzt.
- Blockiert: erscheint nicht im Import-Dialog und wird nicht still in den Katalog übernommen.

Blockieren gilt nur für die gewählte Art. Dieselbe Zeichenkette kann als Genre blockiert und als Mood erlaubt sein.

Der DB-Editor kann alles, was der Import-Dialog kann: bevorzugten Namen anlegen, als Alternative zuordnen, blockieren und freigeben.
