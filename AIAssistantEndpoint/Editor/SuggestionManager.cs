using System;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;

namespace AIAssistantEndpoint.Editor
{
    internal static class SuggestionManager
    {
        // map IWpfTextView to GhostTextAdornment
        private static readonly ConcurrentDictionary<IWpfTextView, GhostTextAdornment> _adornments = new ConcurrentDictionary<IWpfTextView, GhostTextAdornment>();

        internal static void RegisterAdornment(IWpfTextView view, GhostTextAdornment adornment)
        {
            if (view == null || adornment == null) return;
            _adornments[view] = adornment;
        }

        internal static void UnregisterAdornment(IWpfTextView view)
        {
            if (view == null) return;
            GhostTextAdornment removed;
            _adornments.TryRemove(view, out removed);
        }

        private static IWpfTextView GetActiveWpfTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textMgr = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            if (textMgr == null) return null;
            IVsTextView vTextView = null;
            textMgr.GetActiveView(1, null, out vTextView);
            if (vTextView == null) return null;
            var adapter = (IVsEditorAdaptersFactoryService)Package.GetGlobalService(typeof(IVsEditorAdaptersFactoryService));
            if (adapter == null) return null;
            var wpfView = adapter.GetWpfTextView(vTextView);
            return wpfView;
        }

        internal static void ShowSuggestion(string suggestion)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var view = GetActiveWpfTextView();
                if (view == null) return;
                GhostTextAdornment adornment;
                if (!_adornments.TryGetValue(view, out adornment))
                {
                    adornment = new GhostTextAdornment(view);
                    _adornments[view] = adornment;
                }
                adornment.UpdateSuggestion(suggestion);
            }
            catch { }
        }

        internal static void ClearSuggestion()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var view = GetActiveWpfTextView();
                if (view == null) return;
                GhostTextAdornment adornment;
                if (_adornments.TryGetValue(view, out adornment))
                {
                    adornment.Clear();
                }
            }
            catch { }
        }

        internal static void AcceptSuggestion()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var view = GetActiveWpfTextView();
                if (view == null) return;
                GhostTextAdornment adornment;
                if (_adornments.TryGetValue(view, out adornment))
                {
                    adornment.Accept();
                }
            }
            catch { }
        }

        internal static bool HasSuggestion()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var view = GetActiveWpfTextView();
                if (view == null) return false;
                GhostTextAdornment adornment;
                if (_adornments.TryGetValue(view, out adornment))
                {
                    return adornment.HasSuggestion;
                }
            }
            catch { }
            return false;
        }
    }
}
