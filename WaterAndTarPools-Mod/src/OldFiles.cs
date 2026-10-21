using System.IO;
using BepInEx;
using WaterAndTarPools.Logging;

namespace WaterAndTarPools
{
    /// <summary>
    /// T-069: the files this mod keeps in the BepInEx config folder were named DrainTarPit.* while it was part of DrainTarPit. They are renamed
    /// once (only when the new name does not exist yet), so the saved water look, preferred sign style and personal pool styles stay.
    /// </summary>
    internal static class OldFiles
    {
        /// Returns the number of files / folders renamed.
        internal static int Carry()
        {
            int n = 0;
            n += Move("DrainTarPit.WaterLook.txt", "WaterAndTarPools.WaterLook.txt", false);
            n += Move("DrainTarPit.SignStyle.txt", "WaterAndTarPools.SignStyle.txt", false);
            n += Move("DrainTarPit.PoolStyles", "WaterAndTarPools.PoolStyles", true);
            ModLog.Verbose("OldFiles: " + n + " file(s) / folder(s) carried over from the DrainTarPit names");
            return n;
        }

        static int Move(string oldName, string newName, bool folder)
        {
            string from = Path.Combine(Paths.ConfigPath, oldName), to = Path.Combine(Paths.ConfigPath, newName);
            try
            {
                if (folder ? !Directory.Exists(from) || Directory.Exists(to) : !File.Exists(from) || File.Exists(to)) return 0;
                if (folder) Directory.Move(from, to); else File.Move(from, to);
                ModLog.Info("OldFiles: " + oldName + " renamed to " + newName);
                return 1;
            }
            catch (IOException e) { ModLog.Warn("OldFiles: " + oldName + " could not be renamed to " + newName + ": " + e.Message); return 0; }
            catch (System.UnauthorizedAccessException e) { ModLog.Warn("OldFiles: no permission to rename " + oldName + ": " + e.Message); return 0; }
        }
    }
}
