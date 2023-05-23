using System;
using Microsoft.VisualStudio.OLE.Interop;

namespace Dalamud.Interface.DragDrop;

internal partial class DragDropManager : IDropTarget
{
    public void DragEnter(IDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect)
    {
    }

    public void DragOver(uint grfKeyState, POINTL pt, ref uint pdwEffect)
    {
    }

    public void DragLeave()
    {
    }

    public void Drop(IDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect)
    {
    }
}
