using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio; // for VSConstants

namespace AIAssistantEndpoint.Editor
{
    [Guid("D1B2C3D4-E5F6-4711-8899-AABBCCDDEEFF")]
    internal class TabCommandFilter : IVsTextViewFilter, IOleCommandTarget
    {
        private IOleCommandTarget _nextCmdTarget;
        private readonly IWpfTextView _view;

        public TabCommandFilter(IWpfTextView view)
        {
            _view = view;
        }

        public int AddCommandFilter(IOleCommandTarget pNewCmdTarg, out IOleCommandTarget ppNextCmdTarg)
        {
            _nextCmdTarget = pNewCmdTarg;
            ppNextCmdTarg = this;
            return VSConstants.S_OK;
        }

        public int RemoveCommandFilter(IOleCommandTarget pNextCmdTarg)
        {
            if (_nextCmdTarget == pNextCmdTarg) _nextCmdTarget = null;
            return VSConstants.S_OK;
        }

        public int GetWordExtent(int iStart, int iEnd, uint flags, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            pbstrText = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int PreFilterKeyDown(int iVk, int cRepeat, int flags, int vsc, int keyState)
        {
            return VSConstants.S_OK; // continue
        }

        public int PostFilterKeyDown(int iVk, int cRepeat, int flags, int vsc, int keyState)
        {
            return VSConstants.S_OK; // continue
        }

        public int TranslateAccelerator(ref MSG pMsg)
        {
            // Not handling here
            return VSConstants.S_FALSE;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (_nextCmdTarget != null)
                return _nextCmdTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            return VSConstants.S_OK;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                // VS standard command group for Editor commands
                var VSStd2K = VSConstants.VSStd2K;
                // TAB command id
                const uint tabCmdId = (uint)VSConstants.VSStd2KCmdID.TAB;

                if (pguidCmdGroup == VSStd2K && nCmdID == tabCmdId)
                {
                    // If suggestion visible, accept and swallow
                    if (SuggestionManager.HasSuggestion())
                    {
                        SuggestionManager.AcceptSuggestion();
                        return VSConstants.S_OK; // handled
                    }
                }
            }
            catch { }

            if (_nextCmdTarget != null)
                return _nextCmdTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            return VSConstants.S_OK;
        }
    }
}
