# Buran — Anleitung

Buran verwaltet eine Musikbibliothek: ID3-Tags von MP3- und FLAC-Dateien bearbeiten und einen eigenen Katalog für Künstler, Genres und Moods pflegen. Änderungen an Tags werden sofort in die Datei geschrieben.

## Einstellungen

Oben rechts öffnet „Einstellungen“ ein Menü.

- Sprache: Deutsch, Englisch, Russisch oder Systemsprache. Die Oberfläche und diese Anleitung folgen der Wahl.
- Schriftgröße: Klein, Mittel oder Groß.
- Transparenz: Deckkraft des Hauptfensters.

## Bibliothek

Die linke Spalte ist der Ordnerbaum.

- Ordner hinzufügen: einen Wurzelordner zur Bibliothek nehmen. Mehrere Wurzeln sind möglich; die Liste wird gespeichert.
- Entfernen: die Bibliothek des ausgewählten Ordners aus der Liste nehmen.
- Aktualisieren: Baum neu einlesen.
- Ein Unterordner mit blauem Punkt enthält Audio-Dateien. Ein Klick darauf lädt sie in den ID3-Editor.
- Unterstützte Dateien: MP3 und FLAC.

## ID3-Editor

Oben in der Arbeitsfläche. Kompakte Tabelle (Titel, Künstler, Ordnerpfad, Album, Jahr, Dauer, Bitrate, Samplerate, Bit-Tiefe). Spalten über „Spalten“ ein- und ausblenden. Rechtsklick auf einen Titel: „Ordner öffnen“ wählt den Ordner in der Bibliothek links und listet alle Dateien darin.

„Tags bearbeiten“ blendet den Inspector rechts ein; der Splitter ändert nur die Inspector-Breite. Sammelaktionen gelten nur in diesem Modus für die angehakten Dateien.

„Filter weg“ nach „Titel anzeigen“ lädt wieder den ausgewählten Bibliotheksordner, oder die erste Wurzel, wenn keiner gewählt ist.

Ist der Player geladen, sitzen Transport und Spektrogramm unter der Liste (Splitter zum Vergrößern). Ohne Player bleibt der Editor allein funktionsfähig.

### Auswahl und Sammelaktionen

- Alle / Keine: Dateien an- oder abwählen.
- Künstler / Genres / Moods: Sammeldialog für die Auswahl (siehe unten).
- Kommentare: denselben Kommentar setzen oder alle Kommentare der Auswahl leeren.
- Abspielen / Zur Playlist: Auswahl an das Player-Modul übergeben (nur wenn der Player geladen ist).
- Dateiname aus ID3: ausgewählte Dateien nach den Tags umbenennen.
- ID3 aus Dateiname: Tags aus dem Dateinamen lesen und schreiben.

### Felder pro Datei

- Auswahl-Häkchen, Titel, Album, Dateiname, Erscheinungsjahr, Kommentar.
- Bitrate nur Anzeige.
- Künstler, Genres und Moods als Listen: X entfernt einen Eintrag, + bzw. Enter fügt hinzu. Die Vorschlagsliste kommt aus dem Katalog (bevorzugte Namen und Alternativen).
- Unbekannte Namen werden als bevorzugter Katalogeintrag angelegt, sofern sie nicht blockiert sind. Blockierte Werte kommen auf die Datei, aber nicht in die Datenbank.
- Von ID3 zum Dateinamen / Vom Dateinamen zu ID3: nur diese eine Datei.
- ID3 auf Standard zurücksetzen: Tags der Datei auf den Zustand beim Laden zurücksetzen.

Beim Ordneröffnen werden alternative Schreibweisen in den Dateien auf den bevorzugten Katalognamen umgeschrieben und gespeichert.

### Sammeldialog Künstler, Genres, Moods

Die Liste ist die Vereinigung aller ausgewählten Dateien.

- Eintrag entfernen: wird nur dort gelöscht, wo er vorkommt.
- Hinzufügen oder „alle“: setzt den Eintrag auf jede ausgewählte Datei, auch wenn er schon in der Liste steht.
- Aus Auswahl laden: Liste neu aus den Dateien aufbauen.
- Übernehmen schreibt die Tags. Unbekannte Namen landen im Katalog, blockierte nicht.

### Sammeldialog Kommentare

Ein Textfeld, Vorschau je Datei. Übernehmen ersetzt den Kommentar jeder ausgewählten Datei. Leeres Feld löscht ihn.

### Dateiname aus ID3 und Konflikte

Existiert der Zielname bereits, öffnet sich „Dateien vergleichen“ (Größe, Änderung, Dauer, Bitrate, Abtastrate, Kanäle, Bit-Tiefe, Format).

- Beide behalten: Umbenennung abbrechen, beide Dateien bleiben.
- Diese Datei behalten: vorhandene Datei löschen, diese Datei umbenennen.
- Vorhandene behalten: diese Datei löschen, die vorhandene bleibt.

### ID3 aus Dateiname

Der Dateiname wird mit Mustern und Schlüsselwörtern zerlegt (Künstler, Titel, Album, Jahr, Versionshinweise wie Live oder Remix). Komma und Semikolon trennen immer. Weitere Trenner stehen unter Schlüsselwörter im DB-Editor.

„feat.“ / „ft.“ / „featuring“ im Dateinamen — auch in Klammern, z. B. `Eminem - Stan (feat. Dido).mp3` — werden als weitere Künstler gelesen, nicht als Kommentar. `(Live)` oder `[Remix]` bleiben Kommentar.

Bekannte Katalognamen und einbuchstabige Bruchstücke (z. B. „D & F“) werden nicht zerschnitten.

## Neue Werte für die Datenbank

Nach dem Laden eines Ordners erscheint dieser Dialog, wenn ID3-Tags oder Dateinamen Namen enthalten, die noch nicht im Katalog stehen und nicht blockiert sind.

Pro Eintrag:

- Häkchen: übernehmen oder weglassen.
- Bevorzugter Name: neuer Katalogeintrag.
- Alternativname: Schreibweise eines vorhandenen bevorzugten Eintrags. Beim Künstler kann der bevorzugte Name auch direkt mitangelegt werden.
- Blockieren: nie wieder vorschlagen. Genre „Happy“ blockiert nicht Mood „Happy“.

Unten: blockierte Werte mit Freigeben. Freigeben setzt den Namen wieder in die Liste oben, damit du ihn sofort übernehmen kannst.

Übernehmen schreibt nur angehakte Einträge. Überspringen schließt ohne Änderung.

## Player

Eigenes Modul. Spielt MP3 und FLAC über libVLC. Mit ID3-Editor docken Transport und Spektrogramm unter die Editor-Liste; der Player-Reiter behält Ordner-Warteschlange und Playlists. Ohne Editor bleibt der Player-Reiter vollständig.

- Play / Pause / Stop / Zurück / Weiter, Position, Lautstärke.
- Spektrogramm (wie Spek, einmal berechnet). Achsen: Hz, Zeit, dB bis zur Nyquist-Frequenz der Datei. Klick oder Ziehen sucht. Splitter ändert die Höhe. Damit siehst du, ob eine 192-kHz-Datei oberhalb von 16 kHz überhaupt Energie hat.
- Linke Liste: Dateien des gewählten Bibliotheksordners. Doppelklick spielt, „Zur Playlist“ übernimmt.
- Rechte Liste: benannte Playlists, die Ordner übergreifen dürfen. Neu, umbenennen, löschen.
- M3U importieren und exportieren (absolute Pfade). Fehlende Dateien bleiben in der Playlist markiert.
- Vom ID3-Editor: „Abspielen“ und „Zur Playlist“ für die Auswahl.

Wiedergabe aus einer Playlist läuft weiter, wenn du den Ordner wechselst. Die Ordner-Warteschlange stoppt, wenn der aktuelle Titel nicht mehr im neuen Ordner liegt.

## DB-Editor

Zweiter Reiter. Hier liegt der gesamte Katalog: anlegen, umbenennen, als Alternative zuordnen, blockieren und freigeben.

### Suche und Aktualisieren

Die Suche filtert Künstler, Alternativnamen und Schlüsselwörter. „Suche leeren“ setzt sie zurück. „Aktualisieren“ lädt den Katalog neu.

### Künstler

Liste mit bevorzugtem Namen und bürgerlichem Namen. Rechtsklick: Entfernen, Blockieren, Auswahl aufheben, Titel anzeigen (indizierte Titel dieses Künstlers im ID3-Editor, inkl. Ordnerpfad).

Unten hinzufügen:

- Bevorzugter Name: neuer Künstler, optional mit bürgerlichem Namen.
- Alternativname: den getippten Namen einem vorhandenen bevorzugten Künstler zuordnen. Ins Feld tippen (z. B. „E“) filtert die Liste.
- Blockieren: den getippten Namen dauerhaft ausblenden. Steht er schon im Katalog, wird er entfernt. Bei einem bevorzugten Namen werden auch seine Alternativen blockiert.

### Alternative Namen

Spalte aller Alternativen, oder nur die des ausgewählten Künstlers. Hinzufügen braucht einen ausgewählten Künstler. Entfernen und Blockieren per Rechtsklick oder über die Buttons.

### Mitgliedschaften

Nur mit ausgewähltem Künstler. Mitglieder dieser Gruppe bzw. Gruppen, zu denen der Künstler gehört. Namen müssen bereits als Künstler existieren. Eine Gruppe kann nicht Mitglied von sich selbst sein.

### Genres und Moods

Gleiche Bedienung für beide.

- Häkchen: Genre oder Mood dem ausgewählten Künstler zuordnen.
- Rechtsklick: Entfernen, Blockieren, Auswahl aufheben, Nach Auswahl filtern (Künstlerliste), Filter aufheben, Titel anzeigen.
- Alternative Schreibweisen: Varianten des ausgewählten Eintrags. + legt an, Blockieren blendet die getippte Variante aus.
- Unten hinzufügen: bevorzugter Name oder Alternativname eines vorhandenen Eintrags, plus Blockieren.

### Schlüsselwörter

Trennzeichen und Erkennungswörter für Dateinamen und Tags.

- Zusammenarbeit: splittet Künstler (feat, ft, vs, with, and, sowie Zeichen wie & + /).
- Version: erkennt Hinweise wie Live, Remix, Remaster.

Komma und Semikolon bleiben immer Trenner. Wörter und Zeichen in dieser Liste kannst du selbst ergänzen oder löschen; & kannst du hier entfernen, wenn es nicht splitten soll.

### Blockierte Werte

Untere Leiste, getrennt nach Künstler, Genre und Mood. Freigeben erlaubt den Namen wieder als Vorschlag. Der Katalogeintrag kommt dadurch nicht automatisch zurück.

Ein späteres Hinzufügen desselben Namens ins Katalog hebt die Blockierung für genau diese Schreibweise auf.

## Katalogmodell

Drei Arten, jeweils getrennt: Künstler, Genre, Mood.

- Bevorzugter Name: die Schreibweise, die in Dateien landen soll.
- Alternative: andere Schreibweise desselben Eintrags. Wird beim Laden eines Ordners automatisch auf den bevorzugten Namen ersetzt.
- Blockiert: erscheint nicht mehr im Import-Dialog und wird nicht still in den Katalog übernommen.

Blockieren gilt nur für die gewählte Art. Dieselbe Zeichenkette kann als Genre blockiert und als Mood erlaubt sein.

Der DB-Editor kann alles, was der Import-Dialog kann: bevorzugten Namen anlegen, als Alternative zuordnen, blockieren und freigeben.
