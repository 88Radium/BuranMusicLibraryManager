# Buran — User guide

Buran manages a music library: edit ID3 tags on MP3 and FLAC files, and maintain your own catalog of artists, genres, and moods. Tag edits are written to the file immediately.

## Settings

The Settings button in the top-right opens a menu.

- Language: German, English, Russian, or system language. The UI and this guide follow that choice.
- Font size: Small, Medium, or Large.
- Transparency: opacity of the main window.

## Library

The left column is the folder tree.

- Add folder: add a root folder to the library. Several roots are allowed; the list is saved.
- Remove: drop the selected folder’s library from the list.
- Refresh: reload the tree.
- A subfolder with a blue dot contains audio files. Clicking it loads them into the ID3 editor.
- Supported files: MP3 and FLAC.

## ID3 editor

The first workspace tab. Shows every music file in the selected folder as a compact table (title, artists, folder path, album, year, duration, bitrate, sample rate, bit depth). Columns can be shown or hidden via Columns. Right-click a track: Open folder selects that folder in the library on the left and lists its files.

Edit tags switches to the inspector on the right; the splitter resizes only the inspector. Bulk actions (artists, genres, moods, comments, filename) appear only in that mode, for the checked selection.

Clear (after Show tracks) reloads the selected library folder, or the first library root if none is selected.

If the player module is loaded, transport and the spectrogram dock under this list (drag the splitter to enlarge the spectrogram). Without the player, the editor still works.

### Selection and bulk actions

- All / None: select or clear every file.
- Artists / Genres / Moods: bulk dialog for the selection (see below).
- Comments: set the same comment or clear comments on the selection.
- Play / To playlist: send the selection to the player module (only if the player is loaded). Double-click a row to play the folder queue.
- Filename from ID3: rename selected files from ID3 tags.
- ID3 from filename: read tags from the filename and write them.

### Per-file fields (inspector)

- Selection checkbox, title, album, filename, release year, comment.
- Bitrate is display-only.
- Artists, genres, and moods are lists: X removes an entry, + or Enter adds one. Suggestions come from the catalog (preferred names and alternatives).
- Unknown names are added as a preferred catalog entry unless they are blocked. Blocked values still go onto the file, but not into the database.
- From ID3 to filename / From filename to ID3: this file only.
- Reset ID3 to default: restore the tags this file had when it was loaded.

When a folder is opened, alternative spellings in the files are rewritten to the preferred catalog name and saved.

### Bulk dialog for artists, genres, moods

The list is the union of all selected files.

- Remove an entry: it is deleted only where it already exists.
- Add or “all”: writes the entry onto every selected file, even if it is already in the list.
- Load from selection: rebuild the list from the files.
- Apply writes the tags. Unknown names are stored in the catalog; blocked names are not.

### Bulk comments

One text box and a per-file preview. Apply replaces the comment on every selected file. An empty field clears it.

### Filename from ID3 and conflicts

If the target name already exists, Compare files opens (size, modified time, duration, bitrate, sample rate, channels, bit depth, format).

- Keep both: cancel the rename; both files stay.
- Keep this file: delete the existing file and rename this one.
- Keep existing: delete this file and keep the one that is already there.

### ID3 from filename

The filename is split with patterns and keywords (artists, title, album, year, version hints such as Live or Remix). Comma and semicolon always separate. Further separators live under Keywords in the DB editor.

“feat.” / “ft.” / “featuring” in the filename — including parentheses, e.g. `Eminem - Stan (feat. Dido).mp3` — are read as extra artists, not as a comment. `(Live)` or `[Remix]` stay comments.

Known catalog names and one-letter fragments (e.g. “D & F”) are not split.

## New values for the database

After a folder loads, this dialog appears when ID3 tags or filenames contain names that are not in the catalog and are not blocked.

Per item:

- Checkbox: import or skip.
- Preferred name: a new catalog entry.
- Alternative name: a spelling of an existing preferred entry. For artists you can also create the preferred name inline.
- Block: never suggest this value again. Blocking genre “Happy” does not block mood “Happy”.

At the bottom: blocked values with Unblock. Unblock puts the name back into the list above so you can import it immediately.

Apply writes only checked items. Skip closes without changes.

## Player

A separate module. Plays MP3 and FLAC through libVLC. If the ID3 editor is also loaded, transport and spectrogram sit under the editor list; the Player tab keeps folder queue and playlists. Without the editor, the player tab contains everything.

- Play / Pause / Stop / Previous / Next, position, volume.
- Spectrogram (Spek-style: frequency over time, computed once). Axes show Hz, time, and dB up to the file's Nyquist frequency. Click or drag seeks. Drag the splitter to resize. Use it to see whether a 192 kHz file actually has energy above 16 kHz.
- Left list: files in the selected library folder. Double-click plays; Add to playlist copies them.
- Right list: named playlists that may span folders. Create, rename, delete.
- Import and export M3U (absolute paths). Missing files stay in the playlist, marked.
- From the ID3 editor: Play and To playlist for the selection.

Playlist playback continues when you change folder. The folder queue stops if the current track is not in the new folder.

## DB editor

The second tab. This is the full catalog: add, rename, attach as an alternative, block, and unblock.

### Search and refresh

Search filters artists, alternative names, and keywords. Clear search resets it. Refresh reloads the catalog.

### Artists

List of preferred name and legal name. Right-click: Remove, Block, Clear selection, Show tracks (indexed tracks for this artist in the ID3 editor, including folder path).

Add at the bottom:

- Preferred name: a new artist, optionally with a legal name.
- Alternative name: attach the typed name to an existing preferred artist. Type in the field (e.g. “E”) to filter the list.
- Block: hide the typed name permanently. If it is already in the catalog, it is removed. Blocking a preferred name also blocks its alternatives.

### Alternative names

Column of all alternatives, or only those of the selected artist. Adding requires a selected artist. Remove and Block via right-click or the buttons.

### Memberships

Only with an artist selected. Members of this group, or groups this artist belongs to. Names must already exist as artists. A group cannot be a member of itself.

### Genres and moods

The same controls for both.

- Checkbox: assign the genre or mood to the selected artist.
- Right-click: Remove, Block, Clear selection, Filter by selection (artist list), clear the filter, Show tracks.
- Alternative spellings: variants of the selected entry. + adds one; Block hides the typed variant.
- Add at the bottom: preferred name or alternative of an existing entry, plus Block.

### Keywords

Separators and recognition words for filenames and tags.

- Collaboration: splits artists (feat, ft, vs, with, and, and symbols such as & + /).
- Version: matches hints such as Live, Remix, Remaster.

Comma and semicolon always remain separators. You can add or delete words and symbols in this list; remove & here if it should not split.

### Blocked values

Bottom strip, separate lists for artist, genre, and mood. Unblock allows the name as a suggestion again. It does not restore the catalog row by itself.

Adding that same spelling to the catalog later clears the block for that spelling only.

## Catalog model

Three kinds, kept separate: artist, genre, mood.

- Preferred name: the spelling that should end up in files.
- Alternative: another spelling of the same entry. On folder load it is rewritten to the preferred name.
- Blocked: no longer offered in the import dialog and not silently inserted into the catalog.

A block applies only to the chosen kind. The same string can be blocked as a genre and allowed as a mood.

The DB editor can do everything the import dialog can: create a preferred name, attach an alternative, block, and unblock.
