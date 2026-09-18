<p align="center">
  <img src="BuranUI/Assets/Buran.png" alt="Buran" width="168">
</p>

<h1 align="center">Buran Music Library Manager</h1>

<p align="center">
  <strong>Manage a music library, fix tags, and play files.</strong><br>
  Desktop app for Linux (Fedora / Bazzite) and Windows.
</p>

<p align="center">
  <a href="README.md">Deutsch</a>
  ·
  <strong>English</strong>
  ·
  <a href="README.ru.md">Русский</a>
</p>

<p align="center">
  <a href="https://github.com/88Radium/BuranMusicLibraryManager/releases">Download</a>
  ·
  <a href="#installation">Installation</a>
  ·
  <a href="#features">Features</a>
  ·
  <a href="#build-it-yourself">Build it yourself</a>
</p>

---

Buran is for people who want a large MP3/FLAC collection in order: consistent artist names, clean file names, genres and moods, and playback on the side. **Your catalog is the source of truth** — not MusicBrainz, not iTunes. Preferred spellings, alternatives, and the block list are applied automatically the next time you open a folder.

Tag changes are **written to the file immediately**. There is no extra Save button.

| | |
|---|---|
| Files | MP3 and FLAC |
| Platforms | Linux (RPM, including Fedora / Bazzite) and Windows |
| Player | libVLC is bundled |
| Languages | German, English, Russian, or system language |
| Data | Catalog and settings live in the user folder, not in the music library |

The same guide is available in the app under **Settings → Manual**.

## Table of contents

- [Installation](#installation)
- [First launch](#first-launch)
- [Features](#features)
  - [Settings](#settings)
  - [Library](#library)
  - [ID3 editor](#id3-editor)
  - [New catalog values](#new-catalog-values)
  - [Player](#player)
  - [DB editor](#db-editor)
  - [Catalog model](#catalog-model)
- [Where Buran stores data](#where-buran-stores-data)
- [GitHub releases](#github-releases)
- [Build it yourself](#build-it-yourself)

## Installation

Installers are on **[Releases](https://github.com/88Radium/BuranMusicLibraryManager/releases)**.

### Linux (Fedora, RHEL, Bazzite)

1. Download the `.rpm` of the release you want (for example `buran-0.1.3-4.x86_64.rpm`).
2. On **Bazzite / rpm-ostree**:

```bash
sudo rpm-ostree uninstall buran --install /path/to/buran-*.x86_64.rpm
sudo systemctl reboot
```

First install without a previous package:

```bash
sudo rpm-ostree install /path/to/buran-*.x86_64.rpm
sudo systemctl reboot
```

3. Start it from the **Buran Music Library Manager** launcher or with the `buran` command.

Do not run `/usr/lib/buran/BuranUI` directly. The wrapper sets `LD_LIBRARY_PATH` and `VLC_PLUGIN_PATH` so the player finds the bundled VLC libraries.

Debian/Ubuntu: a `.deb` is attached as well. It is a companion package; the Linux libraries inside come from Fedora.

### Windows

1. Download and run `Buran-<Version>-win-x64-setup.exe`.
2. The app is installed to `%LOCALAPPDATA%\Programs\Buran` and the Start menu.
3. Or unzip the portable `.zip` and run `BuranUI.exe`.

Windows may show SmartScreen on first launch (the file is not digitally signed). Choose **More info → Run anyway**.

libVLC and `ffmpeg.exe` (for the spectrogram) are included in the Windows package. A separate VLC or ffmpeg install is not required.

## First launch

1. Open Buran.
2. On the left, **Add folder** and pick the root of your music library (for example `Music`).
3. In the tree, click a subfolder with a blue dot — those folders contain MP3/FLAC files.
4. The files appear on the **ID3 editor** tab.
5. Click **Edit tags** at the top to show the inspector and bulk actions.

Several roots are allowed (an internal drive and a NAS share, for example). Buran remembers the list.

## Features

### Settings

Top right, **Settings**.

| Setting | What it does | Why | How to use it |
|---|---|---|---|
| **Language** | Switches the UI and the built-in manual. | German, English, Russian, or the system language. | Open the list, pick a language. Applies immediately. |
| **Font size** | Small / Medium / Large. | Long tag lists and 4K monitors. | Open the list, pick a size. |
| **Transparency** | Window opacity (from 40 %). | Let the desktop show through. | Slider. |
| **Manual** | Opens the in-app handbook. | The same feature overview, without GitHub. | **Manual** button. |

### Library

The left column is the folder tree of your collection.

#### Add folder

**What:** Adds a folder as a library root.  
**Why:** You see the structure like a file manager, without picking it every time.  
**How:**

1. **Add folder**.
2. Choose the top music folder.
3. Expand and collapse the tree.

#### Remove

**What:** Removes the selected root from the list. Files on disk stay.  
**Why:** An old USB disk or a renamed path should no longer appear.  
**How:** Select the root → **Remove**, or right-click in the tree.

#### Refresh

**What:** Re-reads the tree.  
**Why:** After you created or renamed folders outside Buran.  
**How:** **Refresh**.

#### Include subfolders

**What:** Opening a folder also loads files in subfolders.  
**Why:** An album with `CD1` / `CD2`, or “everything under Metal at once”.  
**How:** Tick the box, then click the folder in the tree.

#### Index library

**What:** Writes title, artist, album, path, and so on into the local catalog index.  
**Why:** In the DB editor, **Show tracks** lists every indexed piece for an artist, genre, or mood, including folder path.  
**How:**

1. Add at least one root.
2. **Index library**.
3. Later in the DB editor: right-click → **Show tracks**.

#### Blue dot

A subfolder with a dot contains audio files. A click loads them into the ID3 editor.

### ID3 editor

Main workspace. This is where you live when tags and file names should match.

#### The table

**What:** A compact list of every file in the chosen folder: title, artists, folder path, album, year, duration, bitrate, sample rate, bit depth.  
**Why:** Overview, sorting, finding the right file without opening each one.  
**How:**

1. Pick a folder with audio on the left.
2. Click a row — that is the **focused track** (inspector, Reset, play on double-click).
3. The **checkbox** in the first column is **multi-select** for bulk actions. Focus and checkboxes are independent: you can inspect track A while B and C are checked.

**Columns**

- **Columns** at the top: tick or untick to show or hide columns.
- Drag headers to reorder, drag the edge to resize.
- **File name**, **Genre**, and **Mood** are off by default so the table stays usable.

**Right-click → Open folder**

Selects that file’s folder on the left and lists every track in it. Useful after **Show tracks** from the DB editor when hits are spread across folders.

**Clear filter**

If the list is filtered (for example “tracks of this artist”), the button loads the currently selected library folder again.

**Double-click** plays the focused track (when the player module is loaded).

#### Edit tags (edit mode)

**What:** Shows the inspector on the right and the bulk bar at the top.  
**Why:** Without this mode the table stays slim for browsing. With it you can change tags, rename, and reset.  
**How:**

1. **Edit tags** — the button then reads **Editing**.
2. Drag the splitter between list and inspector if you need more room for tags.
3. Click again to leave the mode.

When **edit mode starts**, Buran snapshots each track (tags **and** file name). When you **leave** it, the saved state becomes the new baseline.

#### Inspector — one track

The inspector always applies to the **focused** track (clicked row), not to every checkbox.

| Field / button | What it does | Why | How to use it |
|---|---|---|---|
| **Title, album, year, comment** | Writes the value to the file immediately. | Typos, missing year, junk in comments. | Click the field, change the text, leave the field — done. |
| **Artists / genres / moods** | Lists with × to remove and + / Enter to add. Suggestions come from the catalog. | Several artists (`feat.`), several genres, mood separate from genre. | Type a name, pick from the list or create it, + or Enter. |
| **Name →** (ID3 → file name) | Builds `Artist - Title.mp3` from the tags and renames the file. Preferred catalog names are used. Two artists become `A feat. B`, more become `A feat. B, C & D`. | File name and tags should match. | Focus the track, **Name →**. The row stays selected. |
| **← Name** (file name → ID3) | Splits the file name with patterns (artist, title, album, year, live/remix …) and writes the tags. | Clean names, empty or wrong tags. | Focus the track, **← Name**. |
| **Reset** | Restores tags **and** file name to the state **at the start of this edit session**. | Accidental rename or ruined tags, even after jumping to another track and back. | Stay in the same edit session, focus the track again, **Reset**. |

Unknown names that are not blocked become a preferred catalog entry. Blocked values still go onto the file, but not into the database.

When **opening a folder**, Buran replaces alternative spellings in the files with the preferred catalog name and saves that.

#### Bulk actions (checked files)

Edit mode only, and only for rows with a check mark.

| Button | What it does | Why | How |
|---|---|---|---|
| **All / None** | Check or uncheck every file. | Touch a whole folder at once. | **All**, pick an action, **None** when you are done. |
| **Artists / genres / moods** | Opens the bulk dialog. The list is the **union** of all selected files. | Remove “Pop” only where it exists; add “Freestyle” to **every** file. | Check files → button → × or add → **Apply**. |
| **Comments** | The same text on every selected file, or clear all comments. | ID3 comments full of player junk. | Type text and **Apply**, or leave the field empty / **Clear**. |
| **Play** | Hands the selection to the player. | Audition without hunting for the Player tab. | Check → **Play**. |
| **Add to playlist** | Appends the selection to the current playlist. | A mix that crosses folders. | Check → **Add to playlist**. |
| **File name from ID3** | Like **Name →**, for every check mark. | Rename a folder to the tag scheme. | Check → button. |
| **ID3 from file name** | Like **← Name**, for every check mark. | Tags from clean file names for the whole folder. | Check → button. |

**Bulk dialog for artists / genres / moods**

1. Check files.
2. **Artists**, **Genres**, or **Moods**.
3. × removes the entry **only where it occurs**. The other file keeps it.
4. Adding a name (or the **all** hint) puts it on **every** selected file — even if it already appeared in the union list. You can take a genre off the list and then assign it to everyone on purpose.
5. **Load from selection** rebuilds the list if you changed check marks in between.
6. **Apply** writes the tags.

#### File name from ID3 — name conflict

If the target file already exists, **Compare files** opens: size, modified time, duration, bitrate, sample rate, channels, bit depth, format.

| Button | Effect |
|---|---|
| **Keep both** | Cancel the rename. Both files stay. |
| **Keep this file** | Delete the existing file and rename the current one. |
| **Keep existing** | Delete the current file and keep the existing one. |

#### ID3 from file name — how the parser thinks

- Patterns and keywords split the name into artist, title, album, year, and version hints.
- Comma and semicolon always split.
- `feat.` / `ft.` / `featuring` — including in parentheses, e.g. `Eminem - Stan (feat. Dido).mp3` — become **extra artists**, not a comment.
- `(Live)` or `[Remix]` stay a comment.
- Known catalog names and one-letter groups such as `D & F` are not split into `D` and `F`.
- Extra separators are maintained in the DB editor under **Keywords**.

### New catalog values

**What:** Dialog after loading a folder, when tags or file names contain names that are not in the catalog and not blocked.  
**Why:** You decide once whether “Gwen Stefani” is a new artist, an alternative spelling of something known, or junk you never want to see again.  
**How:**

1. Open a folder. The dialog appears only if there is something unknown.
2. Per entry, tick: take it or skip it.
3. **Preferred name** — new catalog entry (the spelling that should land in files).
4. **Alternative name** — typo or other spelling of an existing entry. For an artist you can also create the preferred name at the same time.
5. **Block** — never suggest it again. Genre `Happy` does **not** block mood `Happy`.
6. At the bottom: **Unblock** already blocked values so they appear above again for import.
7. **Apply** writes only ticked entries. **Skip** closes without catalog changes.

### Player

Its own module. Plays MP3 and FLAC through **libVLC** (bundled). Docks under the editor list (splitter to resize) and also as a **Player** tab.

| Feature | What it does | Why | How |
|---|---|---|---|
| **Play / Pause / Stop / Previous / Next** | Transport. | Normal listening. | Bar under the list or on the Player tab. |
| **Position / volume** | Seek and level. | Jump to a point. | Sliders. |
| **Spectrogram** | Frequency over time, computed once (similar to Spek). Axes: Hz, time, dB up to the file’s Nyquist frequency. | See whether a “192 kHz” file has any energy above 16 kHz; spot noise and cuts. | Click or drag to seek. Splitter changes the height. |
| **Folder queue** | Left list on the Player tab: files of the library folder. | Listen through the folder. | Double-click plays, **Add to playlist** copies them. |
| **Playlists** | Right list: named lists that may cross folders. | Mixes, “to tag”, favorites. | **New**, set a name, add tracks. Rename in the name field. **Delete**. |
| **Import / export M3U** | Absolute paths. Missing files stay marked. | Exchange with other players. | Import/export on the Player tab. |
| **Repeat** | Off / all / one / once. | Loop, or stop after this track. | Repeat button on the bar. |
| **Swap player and tabs** | Dock at the top or bottom. | More room for the list or the spectrogram. | Swap button. |

Playback from a **playlist** continues when you change folder. The **folder queue** stops if the current track is no longer in the new folder.

Without the player module, the ID3 editor still works fully.

### DB editor

Second tab. This is the **full catalog**: create, rename, attach as alternative, block, unblock. Everything the import dialog can do, you can maintain here permanently.

#### Search and refresh

- The search box filters artists, alternative names, and keywords.
- **Clear search** resets the filter.
- **Refresh** reloads the catalog.

#### Artists

**What:** List with preferred name and optional legal name (`Marshall Mathers` for `Eminem`).  
**Why:** One canonical spelling that all variants fold into.  
**How:**

- At the bottom, type a **preferred name**, optionally a legal name, **Add**.
- **Alternative name:** attach the typed name to an existing artist. Typing in the field filters (e.g. `E` → Eminem).
- **Block:** hide the typed name. If it is already in the catalog, it is removed. Blocking a preferred name also blocks its alternatives.
- Right-click: **Remove**, **Block**, **Clear selection**, **Show tracks** (indexed pieces in the ID3 editor).

#### Alternative names

Column of all alternatives, or only those of the selected artist. Adding requires a selected artist. Remove and block via right-click or the buttons.

#### Memberships

Only with a selected artist. Members of this group, or groups this artist belongs to (`Eminem` in `D12`). Names must already exist as artists. A group cannot be a member of itself.

#### Genres and moods

Same controls, separate lists. Moods are feelings (`Happy`, `Dark`), not substitute genres.

- Checkbox: assign the genre or mood to the **selected artist** (what repertoire this act has).
- Right-click: Remove, Block, Clear selection, **Filter by selection** (artist list), clear filter, **Show tracks**.
- Alternative spellings: variants of the selected entry (`Danc` → `Dance`).
- At the bottom: create a preferred name, attach an alternative, block.

#### Keywords

Separators and recognition words for file names and tags.

| Type | Purpose | Examples |
|---|---|---|
| **Collaboration** | Splits artists. | `feat`, `ft`, `vs`, `with`, `and`, `&`, `+`, `/` |
| **Version** | Detects hints that stay a comment. | `Live`, `Remix`, `Remaster` |

Comma and semicolon always split. You can remove `&` here if an artist name `D & F` must not be split.

#### Blocked values

Bottom bar, separate for artist, genre, and mood. **Unblock** allows the name as a suggestion again. The catalog entry does not come back by itself — you add it again on purpose.

Adding the same spelling to the catalog later lifts the block for that spelling only.

### Catalog model

Three separate kinds: **artist**, **genre**, **mood**.

| Kind | Meaning |
|---|---|
| **Preferred name** | The spelling that should land in files. |
| **Alternative** | Another spelling of the same entry. Replaced automatically when a folder is loaded. |
| **Blocked** | Does not appear in the import dialog and is not silently added to the catalog. |

Blocking applies only to the chosen kind. The same string can be blocked as a genre and allowed as a mood.

## Where Buran stores data

Buran only touches music files when you write tags or rename. Catalog and settings are separate:

| System | Location |
|---|---|
| Linux | `~/.local/share/Buran` |
| Windows | `%LOCALAPPDATA%\Buran` |

The SQLite file is still named `CerberusMusicManager.db` for historical reasons. Uninstalling the app leaves this folder alone so the catalog survives.

## GitHub releases

Each release with a new version number in `Directory.Build.props` builds, after a push to `master`:

- Linux `.rpm` (and `.deb`)
- Windows `setup.exe` and portable `.zip`

Plain code pushes without a version bump only compile (green **Build** check). Installers are produced when the version is new, when `packaging/` or the package workflow changes, or when you run **Actions → Package → Run workflow** by hand.

## Build it yourself

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`).

```bash
dotnet build BuranMusicLibraryManager.sln -c Release
```

In JetBrains Rider, open the solution and run / compile as usual.

Linux packages on a Fedora/Bazzite machine (VLC libraries are bundled):

```bash
bash packaging/pack.sh linux
```

Output under `dist/` (gitignored), for example `buran-<Version>-4.x86_64.rpm`.

In Rider, the same steps are **Run → Run…** configurations:

| Configuration | Action |
|---|---|
| **Pack Linux** | `packaging/pack.sh linux` |
| **Install Linux RPM** | layers `dist/buran-<Version>-*.rpm` with rpm-ostree (Bazzite; root/pkexec) |
| **Pack + Install Linux RPM** | pack, then install |

`packaging/rider-install-linux.sh` and `packaging/rider-pack-and-install-linux.sh` are the scripts behind the two new configurations.

The Windows setup is built in GitHub Actions (Inno Setup). Locally on Windows, if `iscc` is on PATH:

```bash
bash packaging/pack.sh windows
```

---

Buran writes tags immediately, remembers your catalog, and plays through bundled libVLC. If something is still unclear: in the app, **Settings → Manual**.
