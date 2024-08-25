using Belzont.Interfaces;
using Colossal.PSI.Common;
using Game.PSI;
using Game.UI.Localization;
using Game.UI.Menu;
using System;
using System.Collections.Generic;

namespace Belzont.Utils
{
    public static class NotificationHelper
    {
        public static string GetModDefaultNotificationTitle(string identifier) => NotificationUISystem.GetTitle($"K45::{BasicIMod.Instance.Acronym}.{identifier}");
        public static string GetModDefaultNotificationText(string identifier) => NotificationUISystem.GetText($"K45::{BasicIMod.Instance.Acronym}.{identifier}");
        public static void NotifyProgress(string identifier, int progress, string titleI18n = null, string textI18n = null, Dictionary<string, ILocElement> argsTitle = null, Dictionary<string, ILocElement> argsText = null)
        {
            var notificationId = $"K45::{BasicIMod.Instance.Acronym}.{identifier}";
            var notificationTitle = new LocalizedString(GetModDefaultNotificationTitle(titleI18n ?? identifier), null, argsTitle);
            var notificationText = new LocalizedString(GetModDefaultNotificationText(textI18n ?? identifier), null, argsText);
            NotificationSystem.Push(notificationId, notificationTitle, notificationText, null, null, null, ProgressState.Progressing, progress, null);
            if (progress >= 100)
            {
                NotificationSystem.Pop(notificationId, 5f, notificationTitle, notificationText, null, null, null, ProgressState.Complete, progress, null);
            }
        }
        public static void NotifyWithCallback(string identifier, ProgressState progress, Action callback, string titleI18n = null, string textI18n = null, Dictionary<string, ILocElement> argsTitle = null, Dictionary<string, ILocElement> argsText = null)
        {
            var notificationId = $"K45::{BasicIMod.Instance.Acronym}.{identifier}";
            var notificationTitle = new LocalizedString(GetModDefaultNotificationTitle(titleI18n ?? identifier), null, argsTitle);
            var notificationText = new LocalizedString(GetModDefaultNotificationText(textI18n ?? identifier), null, argsText);
            NotificationSystem.Push(notificationId, notificationTitle, notificationText, null, null, null, progress, null, callback);
        }
        public static void RemoveNotification(string identifier)
        {
            var notificationId = $"K45::{BasicIMod.Instance.Acronym}.{identifier}";
            NotificationSystem.Pop(notificationId, 0, null, null, null, null, null, null, null);
        }
    }
}
