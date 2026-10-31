using System.Collections.Generic;
using Kydl.Common;
using Kydl.Ui;
using WaterAndTarPools.Ui;
using UnityEngine;
using DrainTarPit;
using DrainTarPit.Drain;
using WaterAndTarPools.Drain;
using WaterAndTarPools.Visuals;

namespace WaterAndTarPools.Signs
{
    /// <summary>
    /// T-023: the pool sign menu in the game. The window itself is SignMenuView (Unity project, Assets/DTP_UI/Runtime, previewed there);
    /// this is its host: game text (Loc), the fonts loaded in the game, the preview on the sign, saving into the world, the preferred-style
    /// file. While it is open the character does not move (UiInputLock). Escape, X, Cancel or walking 6 m away close it without saving.
    /// </summary>
    internal sealed class PoolSignMenu : ISignMenuHost
    {
        const string WindowId = "poolsign";
        const float MaxDistance = 6f;
        static PoolSignMenu _open;

        readonly PoolSign _sign;
        SignMenuView _view;
        KydlCore.KyWindow _window;

        PoolSignMenu(PoolSign sign) { _sign = sign; }

        internal static bool IsOpen { get { return _open != null; } }

        /// Closes the menu if it is open (T-068: the mod was switched off). Returns 1 when a menu was closed.
        internal static int Shut(string why)
        {
            if (_open == null) return 0;
            _open.Close(why);
            return 1;
        }

        /// Opens the menu for `sign`. Returns 1 when it is open, 0 when UI Toolkit is not available (logged).
        internal static int Open(PoolSign sign)
        {
            if (sign == null) return 0;
            if (_open != null) _open.Close("another sign opened");
            var m = new PoolSignMenu(sign);
            m._view = new SignMenuView(m, sign.SignName, sign.Style);
            m._window = KydlCore.KyUi.Open(Plugin.ModGuid, WindowId, m._view.Window, () => m.Closed());
            if (m._window == null) { ModLog.Warn("PoolSignMenu: the window could not be opened (UI not available)"); return 0; }
            _open = m;
            ModLog.Verbose("PoolSignMenu: opened for '" + sign.SignName + "' (pool " + sign.PoolLabel() + ")");
            return 1;
        }

        /// Called every frame (Plugin.Update): Escape closes, so does walking away or the sign being removed.
        internal static void Tick()
        {
            var m = _open;
            if (m == null) return;
            if (m._sign == null) { m.Close("the sign was removed"); return; }
            var p = Player.m_localPlayer;
            if (p == null || Vector3.Distance(p.transform.position, m._sign.transform.position) > MaxDistance) m.Close("walked away / no player");
        }

        // --- ISignMenuHost -------------------------------------------------------------------------------------------------
        public string T(string key, params string[] args) { return Loc.T(key, args); }
        public IList<string> Fonts { get { return SignFonts.All(); } }
        public int MaxLetters { get { return PoolSign.MaxName; } }
        public void Preview(string name, SignStyle style) { if (_sign != null) _sign.Preview(name, style); }
        public int SavePreferred(SignStyle style) { return SignPrefs.Save(style); }
        public SignStyle LoadPreferred() { return SignPrefs.Load(); }

        public void Save(string name, SignStyle style)
        {
            if (_sign == null) { Close("sign gone before save"); return; }
            string stored = _sign.Save(name, style);
            ModLog.Verbose("PoolSignMenu: saved as '" + stored + "'");
            Close("saved");
        }

        /// Closes without saving: the sign shows its stored look again.
        public void Close(string why)
        {
            if (_window != null) _window.Close(); else Closed();
            ModLog.Verbose("PoolSignMenu: closed (" + why + ")");
        }

        /// The window is gone (Escape, X, Cancel, saved, walked away): the sign shows its stored look again.
        void Closed()
        {
            if (_sign != null) _sign.EndPreview();
            _view = null;
            _window = null;
            if (_open == this) _open = null;
        }
    }
}
