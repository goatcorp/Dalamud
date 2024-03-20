using System.Runtime.InteropServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Hooking.WndProcHook;

/// <summary>
/// Event arguments for <see cref="WndProcEventDelegate"/>,
/// and the manager for individual WndProc hook.
/// </summary>
internal sealed unsafe class WndProcEventArgs
{
    private readonly WndProcHookManager owner;
    private readonly delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT> oldWndProcW;
    private readonly WndProcDelegate myWndProc;

    private GCHandle gcHandle;
    private bool released;

    /// <summary>
    /// Initializes a new instance of the <see cref="WndProcEventArgs"/> class.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="hwnd">The handle of the target window of the message.</param>
    /// <param name="viewportId">The viewport ID.</param>
    internal WndProcEventArgs(WndProcHookManager owner, HWND hwnd, int viewportId)
    {
        this.Hwnd = hwnd;
        this.owner = owner;
        this.ViewportId = viewportId;
        this.myWndProc = this.WndProcDetour;
        this.oldWndProcW = (delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT>)SetWindowLongPtrW(
            hwnd,
            GWLP.GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(this.myWndProc));
        this.gcHandle = GCHandle.Alloc(this);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate LRESULT WndProcDelegate(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam);

    /// <summary>
    /// Gets the handle of the target window of the message.
    /// </summary>
    public HWND Hwnd { get; }

    /// <summary>
    /// Gets the ImGui viewport ID.
    /// </summary>
    public int ViewportId { get; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public uint Message { get; set; }

    /// <summary>
    /// Gets or sets the WPARAM.
    /// </summary>
    public WPARAM WParam { get; set; }

    /// <summary>
    /// Gets or sets the LPARAM.
    /// </summary>
    public LPARAM LParam { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress calling the next WndProc in the chain.<br />
    /// Does nothing if changed from <see cref="WndProcHookManager.PostWndProc"/>.
    /// </summary>
    public bool SuppressCall { get; set; }

    /// <summary>
    /// Gets or sets the return value.<br />
    /// Has the return value from next window procedure, if accessed from <see cref="WndProcHookManager.PostWndProc"/>.
    /// </summary>
    public LRESULT ReturnValue { get; set; }

    /// <summary>
    /// Sets <see cref="SuppressCall"/> to <c>true</c> and sets <see cref="ReturnValue"/>.
    /// </summary>
    /// <param name="returnValue">The new return value.</param>
    public void SuppressWithValue(LRESULT returnValue)
    {
        this.ReturnValue = returnValue;
        this.SuppressCall = true;
    }

    /// <summary>
    /// Sets <see cref="SuppressCall"/> to <c>true</c> and sets <see cref="ReturnValue"/> from the result of
    /// <see cref="DefWindowProcW"/>.
    /// </summary>
    public void SuppressWithDefault()
    {
        this.ReturnValue = DefWindowProcW(this.Hwnd, this.Message, this.WParam, this.LParam);
        this.SuppressCall = true;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    internal void InternalRelease()
    {
        if (this.released)
            return;

        this.released = true;
        SendMessageW(this.Hwnd, WM.WM_NULL, 0, 0);
        this.FinalRelease();
    }

    private void FinalRelease()
    {
        if (!this.gcHandle.IsAllocated)
            return;

        this.gcHandle.Free();
        SetWindowLongPtrW(this.Hwnd, GWLP.GWLP_WNDPROC, (nint)this.oldWndProcW);
        this.owner.OnHookedWindowRemoved(this);
    }

    private LRESULT WndProcDetour(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        if (hwnd != this.Hwnd)
            return CallWindowProcW(this.oldWndProcW, hwnd, uMsg, wParam, lParam);

        this.SuppressCall = false;
        this.ReturnValue = 0;
        this.Message = uMsg;
        this.WParam = wParam;
        this.LParam = lParam;
        this.owner.InvokePreWndProc(this);

        if (!this.SuppressCall)
            this.ReturnValue = CallWindowProcW(this.oldWndProcW, hwnd, uMsg, wParam, lParam);

        this.owner.InvokePostWndProc(this);

        if (uMsg == WM.WM_NCDESTROY || this.released)
            this.FinalRelease();

        return this.ReturnValue;
    }
}
