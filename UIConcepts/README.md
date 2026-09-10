# UI-Konzepte: Library = Player + ID3

Keine App-Änderungen, nur Mockups. Neu rendern: `python3 UIConcepts/render_mockups.py`

## Die Fusion

Ein Tab **Library**. Catalog (DB-Editor) bleibt ein eigener Tab.

Hören und Taggen sind **Modi** derselben Trackliste. Bulk-Dialoge (Artists, Genres, Moods, Comments) und das Hinzufügen per Autocomplete bleiben im Edit-Modus — sie wandern nur aus der Kartenzeile in Toolbar / Inspector.

## Screenshots

| Datei | Was du siehst |
|---|---|
| `01-hoeren.png` | Standard: kompakte Tabelle, Player+Spektrogramm unten |
| `02-spalten.png` | Spaltenwahl wie Explorer |
| `03-bearbeiten-karten.png` | Edit klappt Zeilen zu ID3-Karten auf |
| `04-bearbeiten-inspektor.png` | Ein Track: Inspector mit Add-Feldern, Bulk in der Toolbar |
| `05-inline-bulk.png` | Mehrere Tracks: Bulk-Leiste + Inspector (Chips + Add bleiben) |

## Edit-Modus, Funktionserhalt

- **Ein Track:** Inspector rechts. Chips mit ×, darunter Autocomplete + für Artist / Genre / Mood. Title, Album, Year, Comment. From filename / Reset.
- **Mehrere Tracks:** cyan Bulk-Leiste (dieselben Dialoge wie heute) plus Inspector für den fokussierten Track in der Auswahl.
- Toolbar im Edit: Bulk artists / genres / moods / Comments, ID3 ↔ name, Select all.

## Layout, kein Skin-System

Farben, Schrift, Transparenz sind schon Settings. Ein zweites komplettes UI (Winamp-Skins) verdoppelt die Arbeit und zerbricht Bindings.

Sinnvoll ist höchstens ein **Layout-Wechsel** später: Inspector (Default) oder Karten. Das ist ein DataTemplate, kein Skin-Engine. Erst bauen, wenn Inspector im Alltag sitzt.
