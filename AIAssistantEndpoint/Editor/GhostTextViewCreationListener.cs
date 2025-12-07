using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.OLE.Interop;

namespace AIAssistantEndpoint.Editor
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")] // apply to all text content types; you may restrict to code types
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class GhostTextViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            // create adornment and register
            var adornment = new GhostTextAdornment(textView);
            SuggestionManager.RegisterAdornment(textView, adornment);

            var methodAdornment = new MethodActionAdornment(textView);

            // attach Tab command filter to IVsTextView
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var vsTextView = AdapterService.GetViewAdapter(textView);
                if (vsTextView != null)
                {
                    var filter = new TabCommandFilter(textView);
                    IOleCommandTarget next;
                    vsTextView.AddCommandFilter(filter, out next);
                }
            }
            catch { }

            // when view is closed/unloaded, unregister
            textView.Closed += (s, e) => SuggestionManager.UnregisterAdornment(textView);
        }
    }
}
