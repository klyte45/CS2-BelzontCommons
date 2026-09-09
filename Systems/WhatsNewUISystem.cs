using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.IO.AssetDatabase;
using Colossal.OdinSerializer.Utilities;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using Game.UI;
using System;

namespace Belzont.Systems
{
    public partial class WhatsNewUISystem : UISystemBase, IBelzontBindable
    {
        private const string kThumbnailResource = "Properties.Thumbnail.png";

        private Action<string, object[]> m_eventCaller;
        private bool m_pendingShow;
        private bool m_forceShow;
        private bool m_whatsNewEligible;
        private string m_pendingChangelog;
        private string m_pendingVersion;
        private bool m_pendingShowOnNewVersion;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder("whatsNew.setShowOnNewVersion", new Action<bool>(SetShowOnNewVersion));
            callBinder("whatsNew.acknowledge", new Action(AcknowledgeWhatsNew));
            callBinder("whatsNew.pullPending", new Func<object>(PullPendingWhatsNew));
            callBinder("whatsNew.shouldShow", new Func<bool>(ShouldShowWhatsNewNow));
            callBinder("whatsNew.canPresent", new Func<bool>(CanPresentWhatsNewNow));
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            m_eventCaller = eventCaller;
        }

        public void SetupEventBinder(Action<string, Delegate> eventCaller)
        {
        }

        /// <summary>
        /// Force-show the What's New dialog (e.g. from settings), ignoring acknowledge / auto-show prefs
        /// and the in-city WorldReady gate. Still requires embedded <c>changelog.md</c> and UI bindings.
        /// </summary>
        public void RequestShowFromSettings()
        {
            var modData = GetModData();
            if (modData is null || !HasChangelogContent(out var changelog))
            {
                return;
            }

            m_forceShow = true;
            m_pendingVersion = BasicIMod.FullVersion;
            m_pendingChangelog = changelog;
            m_pendingShowOnNewVersion = modData.ShowWhatsNewOnNewVersion;
            m_pendingShow = true;
            TryPresentPending();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // Keep a settings-forced pending show across load transitions.
            if (!m_forceShow)
            {
                m_whatsNewEligible = false;
            }

            if (mode != GameMode.Game && mode != GameMode.Editor)
            {
                return;
            }

            var modData = GetModData();
            if (modData is null || !ShouldShowWhatsNew(modData, out var changelog))
            {
                return;
            }

            m_whatsNewEligible = true;
            m_pendingVersion = BasicIMod.FullVersion;
            m_pendingChangelog = changelog;
            m_pendingShowOnNewVersion = modData.ShowWhatsNewOnNewVersion;
            m_pendingShow = true;
        }

        protected override void OnUpdate()
        {
            TryPresentPending();
        }

        private void TryPresentPending()
        {
            if (!m_pendingShow || m_eventCaller is null)
            {
                return;
            }

            if (!CanPresentWhatsNewNow())
            {
                return;
            }

            var view = GameManager.instance?.userInterface?.view?.View;
            if (view is null || !view.IsReadyForBindings())
            {
                return;
            }

            m_pendingShow = false;
            // Keep m_forceShow until PullPending so a missed event can still be recovered via poll,
            // and so a late canPresent check (if any) still succeeds for force-show.
            m_eventCaller("whatsNew.show", BuildWhatsNewPayload(m_pendingVersion, m_pendingChangelog, m_pendingShowOnNewVersion));
        }

        private static bool HasChangelogContent(out string changelog)
        {
            changelog = null;
            if (!KResourceLoader.ResourceExistsMod("changelog.md"))
            {
                return false;
            }

            changelog = KResourceLoader.LoadResourceStringMod("changelog.md");
            return !changelog.IsNullOrWhitespace();
        }

        private static bool ShouldShowWhatsNew(BasicModData data, out string changelog)
        {
            if (!HasChangelogContent(out changelog))
            {
                return false;
            }

            return data.ShowWhatsNewOnNewVersion
                && data.LastWhatsNewVersionShown != BasicIMod.FullVersion;
        }

        private static BasicModData GetModData() => BasicIMod.ModData;

        private static void SaveModSettings()
        {
            _ = AssetDatabase.global.SaveSettings();
        }

        private void SetShowOnNewVersion(bool value)
        {
            var modData = GetModData();
            if (modData is null)
            {
                return;
            }

            modData.ShowWhatsNewOnNewVersion = value;
            SaveModSettings();
        }

        private void AcknowledgeWhatsNew()
        {
            m_forceShow = false;
            m_pendingShow = false;

            var modData = GetModData();
            if (modData is null)
            {
                return;
            }

            modData.LastWhatsNewVersionShown = BasicIMod.FullVersion;
            SaveModSettings();
        }

        /// <summary>Auto-show after entering a city/editor: world ready and not on the loading overlay.</summary>
        private static bool CanPresentWhatsNewAuto()
        {
            var gameManager = GameManager.instance;
            var overlay = gameManager?.userInterface?.overlayBindings;
            if (overlay is null)
            {
                return false;
            }

            return gameManager.state == GameManager.State.WorldReady
                && overlay.currentlyActiveScreen != OverlayScreen.Loading;
        }

        /// <summary>Settings force-show: any screen except the loading overlay, as long as the UI view exists.</summary>
        private static bool CanPresentWhatsNewAnywhere()
        {
            var gameManager = GameManager.instance;
            if (gameManager?.userInterface?.view?.View is null)
            {
                return false;
            }

            var overlay = gameManager.userInterface.overlayBindings;
            if (overlay is null)
            {
                return true;
            }

            return overlay.currentlyActiveScreen != OverlayScreen.Loading;
        }

        private bool CanPresentWhatsNewNow()
        {
            if (m_forceShow)
            {
                return CanPresentWhatsNewAnywhere();
            }

            return m_whatsNewEligible && CanPresentWhatsNewAuto();
        }

        private bool ShouldShowWhatsNewNow()
        {
            if (m_forceShow)
            {
                return true;
            }

            var modData = GetModData();
            return modData is not null && ShouldShowWhatsNew(modData, out _);
        }

        private object PullPendingWhatsNew()
        {
            if (!CanPresentWhatsNewNow())
            {
                return null;
            }

            var modData = GetModData();
            if (modData is null)
            {
                return null;
            }

            if (m_forceShow)
            {
                if (!HasChangelogContent(out var forcedChangelog))
                {
                    return null;
                }

                m_pendingShow = false;
                m_forceShow = false;
                return BuildWhatsNewPayload(BasicIMod.FullVersion, forcedChangelog, modData.ShowWhatsNewOnNewVersion);
            }

            if (!ShouldShowWhatsNew(modData, out var changelog))
            {
                return null;
            }

            return BuildWhatsNewPayload(BasicIMod.FullVersion, changelog, modData.ShowWhatsNewOnNewVersion);
        }

        private static object[] BuildWhatsNewPayload(string version, string changelog, bool showOnNewVersion)
        {
            return new object[]
            {
                version,
                changelog,
                showOnNewVersion,
                GetWhatsNewThumbnailDataUrl(),
                BasicIMod.Instance?.SimpleName ?? string.Empty,
            };
        }

        private static string GetWhatsNewThumbnailDataUrl()
        {
            if (!KResourceLoader.ResourceExistsMod(kThumbnailResource))
            {
                return null;
            }

            var bytes = KResourceLoader.LoadResourceDataMod(kThumbnailResource);
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
    }
}
