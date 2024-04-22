using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

// ReSharper disable UnusedMember.Local
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
namespace Dalamud.Interface.DragDrop;

#pragma warning disable SA1600 // Elements should be documented
/// <summary> Implements interop enums and function calls to interact with external drag and drop. </summary>
internal partial class DragDropManager
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00000122-0000-0000-C000-000000000046")]
    [ComImport]
    public interface IDropTarget
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void DragEnter([MarshalAs(UnmanagedType.Interface), In] IDataObject pDataObj, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD"), In] uint grfKeyState, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.POINTL"), In] POINTL pt, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD"), In, Out] ref uint pdwEffect);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void DragOver([ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD"), In] uint grfKeyState, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.POINTL"), In] POINTL pt, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD"), In, Out] ref uint pdwEffect);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void DragLeave();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Drop([MarshalAs(UnmanagedType.Interface), In] IDataObject pDataObj, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD"), In] uint grfKeyState, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.POINTL"), In] POINTL pt, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD"), In, Out] ref uint pdwEffect);
    }

    internal struct POINTL
    {
        [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]
        public int X;
        [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]
        public int Y;
    }

    private static class DragDropInterop
    {
        [Flags]
        public enum ModifierKeys
        {
            MK_NONE = 0x00,
            MK_LBUTTON = 0x01,
            MK_RBUTTON = 0x02,
            MK_SHIFT = 0x04,
            MK_CONTROL = 0x08,
            MK_MBUTTON = 0x10,
            MK_ALT = 0x20,
        }

        public enum ClipboardFormat
        {
            CF_TEXT = 1,
            CF_BITMAP = 2,
            CF_DIB = 3,
            CF_UNICODETEXT = 13,
            CF_HDROP = 15,
        }

        [Flags]
        public enum DVAspect
        {
            DVASPECT_CONTENT = 0x01,
            DVASPECT_THUMBNAIL = 0x02,
            DVASPECT_ICON = 0x04,
            DVASPECT_DOCPRINT = 0x08,
        }

        [Flags]
        public enum TYMED
        {
            TYMED_NULL = 0x00,
            TYMED_HGLOBAL = 0x01,
            TYMED_FILE = 0x02,
            TYMED_ISTREAM = 0x04,
            TYMED_ISTORAGE = 0x08,
            TYMED_GDI = 0x10,
            TYMED_MFPICT = 0x20,
            TYMED_ENHMF = 0x40,
        }

        [Flags]
        public enum DropEffects : uint
        {
            None = 0x00_0000_00,
            Copy = 0x00_0000_01,
            Move = 0x00_0000_02,
            Link = 0x00_0000_04,
            Scroll = 0x80_0000_00,
        }

        [DllImport("ole32.dll")]
        public static extern int RegisterDragDrop(nint hwnd, IDropTarget pDropTarget);

        [DllImport("ole32.dll")]
        public static extern int RevokeDragDrop(nint hwnd);

        [DllImport("shell32.dll")]
        public static extern int DragQueryFileW(IntPtr hDrop, uint iFile, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszFile, int cch);
    }
}
#pragma warning restore SA1600 // Elements should be documented
