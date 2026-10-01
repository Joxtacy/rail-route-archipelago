using System;
using System.IO;
using Game.Context;
using Game.Level;
using RailRouteArchipelago.Core;
using UnityEngine;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// The loaded level's <see cref="SaveApState"/> and its state files: &lt;save file name&gt;.ap.json in the
    /// game's save folder. <see cref="LoadFor"/> runs once per loaded level; the save patches write, delete
    /// and move the files with the game's own. File errors are logged and never reach the game's save path.
    /// </summary>
    internal static class SaveStateStore
    {
        private static Game.Level.Level level;

        // A state file that exists but couldn't be read. Never overwritten, so the player can still inspect it.
        private static string protectedName;

        /// <summary>The loaded level's state. Fresh until <see cref="LoadFor"/> runs.</summary>
        public static SaveApState Current { get; private set; } = new SaveApState();

        /// <summary>Loads the state of <paramref name="saveFile"/>, or starts a fresh one for a new game or a restart.</summary>
        public static void LoadFor(Game.Level.Level loaded, StorageController.SaveFile saveFile)
        {
            level = loaded;
            protectedName = null;
            Current = saveFile == null ? new SaveApState() : Read(saveFile.FileName);
            Log.Info("Save state (" + (saveFile == null ? "new game" : saveFile.FileName) + "): " + Current.Describe());
        }

        /// <summary>Writes <see cref="Current"/> as the state of the save <paramref name="fileName"/>, through a temp file.</summary>
        public static void WriteFor(string fileName)
        {
            try
            {
                if (!ReferenceEquals(level, Ctx.Deps?.LevelController?.CurrentLevel))
                {
                    Log.Warn("Save state not written for " + fileName + ": the state isn't the current level's");
                    return;
                }
                if (fileName == protectedName)
                {
                    Log.Warn("Save state not written for " + fileName + ": its state file is unreadable and kept as it is");
                    return;
                }
                var path = PathFor(fileName);
                var temp = path + ".tmp";
                File.WriteAllText(temp, Current.Serialize());
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
                Log.Info("Save state written for " + fileName + ": " + Current.Describe());
            }
            catch (Exception e)
            {
                Log.Error("Writing the save state for " + fileName + " failed: " + e.Message);
            }
        }

        public static void Delete(string fileName)
        {
            try
            {
                var path = PathFor(fileName);
                if (!File.Exists(path))
                {
                    return;
                }
                File.Delete(path);
                Log.Info("Save state deleted with " + fileName);
            }
            catch (Exception e)
            {
                Log.Error("Deleting the save state of " + fileName + " failed: " + e.Message);
            }
        }

        public static void Move(string from, string to)
        {
            try
            {
                var source = PathFor(from);
                if (from == to || !File.Exists(source))
                {
                    return;
                }
                File.Move(source, PathFor(to));
                if (protectedName == from)
                {
                    protectedName = to;
                }
                Log.Info("Save state moved from " + from + " to " + to);
            }
            catch (Exception e)
            {
                Log.Error("Moving the save state of " + from + " to " + to + " failed: " + e.Message);
            }
        }

        private static SaveApState Read(string fileName)
        {
            string path = null;
            try
            {
                path = PathFor(fileName);
                if (!File.Exists(path))
                {
                    return new SaveApState();
                }
                if (SaveApState.TryParse(File.ReadAllText(path), out var state, out var error))
                {
                    return state;
                }
                Log.Error("Save state file " + path + " can't be read: " + error.TrimEnd('.') + ". The save is refused at login.");
            }
            catch (Exception e)
            {
                Log.Error("Save state file " + (path ?? fileName + SaveApState.FileSuffix) + " can't be read: " + e.Message.TrimEnd('.') + ". The save is refused at login.");
            }
            protectedName = fileName;
            return SaveApState.UnreadableFile();
        }

        // The game's own save folder: persistentDataPath plus StorageController.SaveDirectory.
        private static string PathFor(string fileName)
        {
            var storage = (StorageController)Ctx.Deps.StorageController;
            return Path.Combine(Application.persistentDataPath, storage.SaveDirectory, fileName + SaveApState.FileSuffix);
        }
    }
}
