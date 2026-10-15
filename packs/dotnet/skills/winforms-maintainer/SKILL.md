---
name: winforms-maintainer
description: (.NET) Review and safely refactor existing WinForms (.NET Framework) code without rewriting it. Use this skill whenever the user shares a WinForms Form / UserControl / event handler, or mentions cross-thread exceptions, InvokeRequired / BeginInvoke, a frozen or unresponsive UI, undisposed controls, event-handler leaks, DataGridView / BindingSource issues, long-running work on a button click, or "why does my WinForms app hang / crash with a cross-thread error". Always use this skill for WinForms maintenance instead of answering from memory; it applies a fixed safety checklist and proposes minimal, behavior-preserving changes - never a rewrite to another UI framework.
category: Legacy (.NET)
---

# WinForms Maintainer

Keep an existing WinForms app alive and healthy with **small, safe, behavior-preserving** changes. This is maintenance, not migration: no WPF/MAUI rewrite, no restructuring the whole form. Fix the things that actually cause crashes, hangs, and leaks in long-lived WinForms apps, and leave the rest alone.

## Input - point it at your code

You don't need pasted snippets. Work from whatever target the user gives:

- **A single form** - `MainForm.cs` (grab its `.Designer.cs` too if control setup matters)
- **A folder** - a `Forms/` or `UI/` directory
- **A whole project or solution** - a `.csproj` / `.sln` or the repo root
- **A GitHub URL** - clone first: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

Find the WinForms code yourself with Glob/Grep/Bash instead of asking for a paste:
```
grep -rl "System.Windows.Forms\|: Form\|InitializeComponent" --include=*.cs <target> | grep -vi "/obj/\|/bin/"
grep -rn "new Thread\|InvokeRequired\|async void\|ShowDialog\|+= " --include=*.cs <target>
```
Prioritize handlers with cross-thread access, blocking calls, or leaks, and tell the user which forms you scanned.

## When this runs

The user maintains a WinForms app on .NET Framework and wants a specific handler/form reviewed or fixed - a cross-thread crash, a UI that freezes, a leak, a flaky grid. They paste a `Form`/`UserControl` or describe the symptom. If you don't have the code, ask for the handler and the `.Designer.cs` if control setup is relevant.

## Guardrail
**Do not propose rewriting to another UI stack or restructuring the form.** Every change must be droppable into the existing form with minimal diff. If a real fix requires bigger surgery, describe it and let the user decide - default to the smallest safe change.

## The checklist

Apply every item. For each finding: problem -> impact -> minimal fix.

### 1. Cross-thread UI access (the #1 WinForms crash)
Touching a control from a non-UI thread throws `InvalidOperationException` ("Cross-thread operation not valid").
- **Detect:** a `Thread`, `Task.Run`, `BackgroundWorker.DoWork`, timer callback, or async continuation that sets `.Text`, `.DataSource`, `.Items`, `.Enabled`, etc. on a control.
- **Fix (modern):** prefer `async/await` - do the work with `await Task.Run(...)` and update the UI *after* the await, which resumes on the UI thread via the sync context. No `Invoke` needed.
- **Fix (when you must marshal):** `if (control.InvokeRequired) control.BeginInvoke(() => control.Text = value); else control.Text = value;`. Prefer `BeginInvoke` (async, won't deadlock) over `Invoke` (sync, can deadlock if the UI thread is waiting on your worker).

### 2. Blocking the UI thread (frozen app)
Long work on the UI thread makes the window "Not Responding".
- **Detect:** DB calls, HTTP, file I/O, `Thread.Sleep`, big loops directly inside a `Click`/`Load` handler; `.Result` / `.Wait()` on a Task in a UI handler (also a deadlock risk with the sync context).
- **Fix:** make the handler `async` and `await` the work off-thread (`await Task.Run(...)` for CPU-bound; real async APIs for I/O). Disable the triggering control and show busy state while it runs; re-enable in a `finally`. Never `.Result`/`.Wait()` on the UI thread.

### 3. Undisposed controls / dialogs / GDI objects (leaks)
- **Detect:** `new Form()` / `new SomeDialog()` shown without `using`; `Font`, `Brush`, `Pen`, `Bitmap`, `Graphics` created in `OnPaint`/handlers without disposal; controls added/removed dynamically but never disposed.
- **Fix:** `using var dlg = new EditForm(); dlg.ShowDialog(this);`. Dispose GDI objects (`Font`/`Brush`/`Pen`/`Bitmap`) with `using`, or cache long-lived ones as fields and dispose in the form's `Dispose`. Note: controls added to `Controls` are disposed by the parent - ones you remove and drop are not.

### 4. Event-handler leaks (the quiet memory grower)
Long-lived publishers keep short-lived subscribers alive via `+=` that's never `-=`'d.
- **Detect:** a form/control subscribing to an event on a longer-lived object (a static, a singleton service, a parent) with no matching unsubscribe on close/dispose.
- **Fix:** unsubscribe in `FormClosed`/`Dispose` for every `+=` to an external/long-lived source. (In-form Designer wiring to the form's own controls is fine - it dies with the form.)

### 5. DataGridView / BindingSource patterns
- **Detect:** rebuilding `DataSource` on every refresh causing flicker/reset; editing the grid's backing list directly instead of through a `BindingSource`; heavy formatting per cell.
- **Fix:** bind through a `BindingSource`; mutate the list via the `BindingSource` so the grid updates; for bulk changes `SuspendLayout()`/`ResumeLayout()` or set `DataSource = null` then rebind once. Keep `DataTable`/`DataSet` binding if that's what's already load-bearing - don't swap the data model.

### 6. async void
- **Detect:** `async void` handlers that can throw - an unobserved exception crashes the process.
- **Fix:** `async void` is acceptable **only** for event handlers, and only if the whole body is wrapped in `try/catch` that surfaces errors to the user/log. Everywhere else, `async Task`.

### 7. Designer-file safety
- **Detect:** proposed edits that would need to touch `*.Designer.cs`.
- **Fix:** avoid hand-editing generated Designer code where possible; if unavoidable, make the change idempotent and call it out so the VS designer won't clobber or choke on it.

## Reference: safe long-running handler

```csharp
private async void btnLoad_Click(object sender, EventArgs e)
{
    btnLoad.Enabled = false;                 // guard against double-click / re-entry
    UseWaitCursor = true;
    try
    {
        // off the UI thread
        List<OrderDto> orders = await _service.GetOrdersAsync(_customerId, CancellationToken.None);

        // back on the UI thread automatically after await - safe to touch controls
        ordersBindingSource.DataSource = orders;
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        // and log it - async void must not let exceptions escape
    }
    finally
    {
        UseWaitCursor = false;
        btnLoad.Enabled = true;
    }
}
```

## Output format

```
## Summary
<one paragraph: crash/hang/leak headline findings>

## Findings
1. [Cross-thread] <handler/line> - <problem> -> <minimal fix>
2. [UI-thread block] ... -> ...
3. [Leak] ... -> ...

## Patched code
<the minimally changed handler(s), ready to paste>

## Verify on your side
<Designer.cs touch points, threading assumptions, anything needing a run>
```

## Principles
- **Maintenance, not migration.** Smallest safe diff; never propose a UI-framework rewrite.
- **await first, Invoke second.** Modern async removes most `InvokeRequired` boilerplate; use `BeginInvoke` when you must marshal.
- **Never block or `.Result` on the UI thread.** That's freezes and deadlocks.
- **Every `+=` to a long-lived source needs a `-=`.** That's your slow leak.
- **Respect load-bearing DataSet/DataTable binding.** Don't swap the data model to "modernize".
