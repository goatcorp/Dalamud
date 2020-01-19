using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.DXGI
{
    public class DXHookD3D11
    {
        const int DXGI_SWAPCHAIN_METHOD_COUNT = 18;
        const int D3D11_DEVICE_METHOD_COUNT = 43;

        public static SharpDX.DXGI.SwapChainDescription CreateSwapChainDescription(IntPtr renderForm)
        {
            return new SharpDX.DXGI.SwapChainDescription
            {
                BufferCount = 1,
                Flags = SharpDX.DXGI.SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new SharpDX.DXGI.ModeDescription(100, 100, new Rational(60, 1), SharpDX.DXGI.Format.R8G8B8A8_UNorm),
                OutputHandle = renderForm,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput
            };
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            List<IntPtr> vtblAddresses = new List<IntPtr>();
            IntPtr vTable = Marshal.ReadIntPtr(pointer);
            for (int i = startIndex; i < startIndex + numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable, i * IntPtr.Size)); // using IntPtr.Size allows us to support both 32 and 64-bit processes

            return vtblAddresses.ToArray();
        }

        List<IntPtr> _d3d11VTblAddresses = null;
        List<IntPtr> _dxgiSwapChainVTblAddresses = null;

        #region Internal device resources
        SharpDX.Direct3D11.Device _device;
        SharpDX.DXGI.SwapChain _swapChain;
        SharpDX.Windows.RenderForm _renderForm;
        #endregion

        #region Main device resources
        public SharpDX.Windows.RenderForm RenderForm { get => _renderForm; set => _renderForm = value; }
        #endregion

        public IntPtr Hook()
        {

            if (_d3d11VTblAddresses == null)
            {
                _d3d11VTblAddresses = new List<IntPtr>();
                _dxgiSwapChainVTblAddresses = new List<IntPtr>();

                #region Get Device and SwapChain method addresses
                // Create temporary device + swapchain and determine method addresses
                RenderForm = new SharpDX.Windows.RenderForm();
                SharpDX.Direct3D11.Device.CreateWithSwapChain(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    CreateSwapChainDescription(RenderForm.Handle),
                    out _device,
                    out _swapChain
                );
                if (_device != null && _swapChain != null)
                {
                    _d3d11VTblAddresses.AddRange(GetVTblAddresses(_device.NativePointer, D3D11_DEVICE_METHOD_COUNT));
                    _dxgiSwapChainVTblAddresses.AddRange(GetVTblAddresses(_swapChain.NativePointer, DXGI_SWAPCHAIN_METHOD_COUNT));
                }
                _device.Dispose();
                _swapChain.Dispose();
                #endregion
            }

            return _dxgiSwapChainVTblAddresses[8];
        }
    }
}
