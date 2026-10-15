# How to use - WinForms Maintainer

**What it is.** A skill that reviews and safely refactors existing WinForms (.NET Framework) code - the crashes, freezes, and leaks that plague long-lived desktop apps - with **small, behavior-preserving** changes. It is maintenance, not migration: no WPF/MAUI rewrite, no restructuring your forms.

**When to reach for it.** Cross-thread exceptions ("Cross-thread operation not valid"), a UI that freezes on a button click, undisposed dialogs/GDI objects, event-handler memory leaks, flaky `DataGridView`/`BindingSource`, or `async void` handlers that crash the process.

**How to use it.** Point it at your code - no pasting needed. Give it a **form**, a **`Forms/` folder**, a whole **project**, or a **GitHub URL** (it clones and scans). Example prompts:
- "Why does this form throw a cross-thread error, and fix it minimally."
- "This button freezes the UI - make it async without restructuring the form."
- "Find event-handler and dialog leaks in this WinForms app."

**How to get the best out of it.**
- **Share the `.Designer.cs`** when the fix touches control setup - it avoids edits the VS designer would clobber.
- **Describe the symptom** (freeze, crash, slow leak) so it targets the real handler instead of every micro-pattern.
- **Ask for the smallest diff** - that's the default; it won't propose a UI-framework rewrite.

**What it won't do.** It won't migrate you to WPF/MAUI or re-architect the form. It won't swap out load-bearing `DataSet`/`DataTable` binding to "modernize". Anything needing a run (threading assumptions, Designer touch points) comes marked "verify".
