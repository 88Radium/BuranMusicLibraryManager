# Buran — User guide

Buran manages a music library: edit ID3 tags on MP3 and FLAC files, keep your own catalog of artists, genres, and moods, and play files on the side. Tag edits are written to the file immediately.

Your catalog is the source of truth — not MusicBrainz, not iTunes. Preferred spellings, alternatives, and the block list apply automatically the next time you open a folder.

## Typical problems

- The same artist in five spellings (`Eminem`, `EMINEM`, `M&M`): catalog with a preferred name plus alternatives. Opening a folder rewrites the variants automatically.
- `feat. Dido` lands in the comment instead of as an artist: ID3 from filename; separators under Keywords → Collaboration in the DB editor.
- Empty or wrong tags, but clean file names: ID3 from filename (one file or bulk).
- Wild file names, tags are fine: Filename from ID3. Scheme `Artist - Title`.
- Two files would get the same name: Compare files (size, duration, bitrate, …) and keep or delete.
- ID3 comments full of player junk: bulk Comments — set the same text or clear.
- A genre only on some files: bulk Genres. × removes it only where it occurs; add writes it onto every checked file.
- Album with CD1/CD2 in subfolders: Include subfolders, then click the album folder.
- Every track by an artist across the collection: Index library, then in the DB editor right-click → Show tracks.
- Is a “192 kHz” file really hi-res? Spectrogram: energy above 16 kHz visible or dead.
- Accidental rename or ruined tags: Reset in edit mode — tags and file name back to the start of this session.
- Never suggest junk names again: import dialog or DB editor → Block (only for the chosen kind).

## Settings

The Settings button in the top-right opens a menu.

- Language: German, English, Russian, Uzbek (Latin), or system language. The UI and this guide follow that choice.
- Font size: Small, Medium, or Large.
- Transparency: opacity of the main window (from 40 %).
- User guide: this handbook.
- Version: installed version number (also on the splash screen).

## Library

The left column is the folder tree.

- Add folder: add a root folder to the library. Several roots are allowed; the list is saved.
- Remove: drop the selected root from the list. Files on disk stay.
- Refresh: reload the tree, for example after folders changed outside Buran.
- Include subfolders: opening a folder also loads files in subfolders (album with CD1/CD2).
- Index library: write title, artist, album, path into the local index. Later in the DB editor, right-click → Show tracks.
- A subfolder with a blue dot contains audio files. Clicking it loads them into the ID3 editor.
- Supported files: MP3 and FLAC.

## ID3 editor

The first workspace tab. Compact table (title, artists, folder path, album, year, duration, bitrate, sample rate, bit depth). Columns can be shown or hidden via Columns; drag headers to reorder. File name, Comment, Genre, and Mood are off by default. Double-click the splitter between two headers to fit the column on the left to its content, at least as wide as the header text. Click the header itself to sort the rows; the faint arrow pair beside it marks that.

A clicked row is the focused track (inspector, Reset, double-click plays). The checkbox in the first column is multi-select. Focus and checkboxes are independent.

Right-click → Open folder selects that folder on the left. Useful after Show tracks when hits are spread across folders.

Clear (after Show tracks) reloads the selected library folder, or the first library root if none is selected.

If the player module is loaded, transport and the spectrogram dock under this list (drag the splitter to enlarge). Without the player, the editor still works.

Always in the top bar: All / None, Play, and To playlist (player must be loaded). Bulk tag and rename actions only in edit mode.

### Edit tags

Shows the inspector on the right and the bulk bar at the top. When edit mode starts, Buran snapshots tags and file name per track; leaving the mode makes that saved state the new baseline.

### Inspector — one track

Applies to the focused track, not to every checkbox.

- Title, album, year, comment: written to the file immediately.
- Artists, genres, moods: lists with × and + / Enter. Suggestions come from the catalog.
- Name → builds `Artist - Title` from the tags (preferred catalog names). Two artists: `A feat. B`, more: `A feat. B, C & D`.
- ← Name splits the file name and writes the tags.
- Reset restores tags and file name to the start of this edit session.

Unknown names that are not blocked become a preferred catalog entry. Blocked values still go onto the file, but not into the database.

When a folder is opened, alternative spellings in the files are rewritten to the preferred catalog name and saved.

### Bulk actions (checked files)

Edit mode only, checked rows only: artists, genres, moods, comments, filename from ID3, ID3 from filename.

The bulk-dialog list is the union of all selected files. × deletes only where the entry occurs. Add or “all” writes it onto every selected file.

Comments: one text box and a per-file preview. An empty field clears the comment.

### Filename from ID3 and conflicts

If the target name already exists, Compare files opens (size, modified time, duration, bitrate, sample rate, channels, bit depth, format).

- Keep both: cancel the rename.
- Keep this file: delete the existing file and rename this one.
- Keep existing: delete this file.

### ID3 from filename

Patterns and keywords split the name (artists, title, album, year, Live/Remix). Comma and semicolon always separate.

“feat.” / “ft.” / “featuring” — including in parentheses, e.g. `Eminem - Stan (feat. Dido).mp3` — become extra artists, not a comment. `(Live)` or `[Remix]` stay comments.

Known catalog names and one-letter groups such as “D & F” are not split. Extra separators live under Keywords in the DB editor.

## New values for the database

After a folder loads, when tags or filenames contain names that are not in the catalog and are not blocked.

- Checkbox: import or skip.
- Preferred name: a new catalog entry.
- Alternative name: a spelling of an existing entry.
- Block: never suggest this value again. Blocking genre “Happy” does not block mood “Happy”.
- At the bottom: unblock already blocked values so they appear above again.
- Apply writes only checked items. Skip closes without changes.

## Player

A separate module. Plays MP3 and FLAC through libVLC. With the ID3 editor, transport and spectrogram sit under the editor list; the Player tab keeps folder queue and playlists.

- Play / Pause / Stop / Previous / Next, position, volume.
- Repeat: off / all / one / once.
- Swap player and tabs: dock at the top or bottom.
- Spectrogram (computed once, Spek-style). Axes: Hz, time, dB up to the Nyquist frequency. Click or drag seeks. Use it to see whether a 192 kHz file has energy above 16 kHz.
- Left list: files in the library folder. Double-click plays; Add to playlist copies them.
- Right list: named playlists that may span folders. Create, rename, delete.
- Import and export M3U (absolute paths). Missing files stay marked.
- From the ID3 editor: Play and To playlist for the selection.

Playlist playback continues when you change folder. The folder queue stops if the current track is not in the new folder.

## DB editor

The second tab. The full catalog: add, rename, attach as an alternative, block, and unblock.

### Search and refresh

Search filters artists, alternative names, and keywords. Clear search resets it. Refresh reloads the catalog.

### Artists

List of preferred name and legal name. Right-click: Remove, Block, Clear selection, Show tracks (indexed tracks including folder path).

Add at the bottom: preferred name (optional legal name), attach an alternative to an existing artist, or Block. Typing in the field filters (e.g. “E”).

Blocking a preferred name also blocks its alternatives.

### Alternative names

Column of all alternatives, or only those of the selected artist. Adding requires a selected artist.

### Memberships

Only with an artist selected. Members of this group, or groups this artist belongs to. Names must already exist as artists. A group cannot be a member of itself.

### Genres and moods

The same controls, separate lists. Moods are feelings (`Happy`, `Dark`), not substitute genres.

- Checkbox: assign to the selected artist.
- Right-click: Remove, Block, Clear selection, Filter by selection, clear the filter, Show tracks.
- Alternative spellings of the selected entry.
- At the bottom: create a preferred name, attach an alternative, block.

### Keywords

- Collaboration: splits artists (feat, ft, vs, with, and, and symbols such as & + /).
- Version: matches Live, Remix, Remaster.

Comma and semicolon always remain separators. Remove & here if “D & F” must not split.

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
