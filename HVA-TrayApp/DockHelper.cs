using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HVA_TrayApp
{
    public class DockHelper
    {

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool EnumChildWindows(IntPtr WindowHandle, EnumWindowProcess Callback, IntPtr lParam);

        private delegate int MouseHookProc(int code, IntPtr wParam, MSLLHOOKSTRUCT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr SetWindowsHookExW(Int32 idHook, MouseHookDelegate HookProc, IntPtr hInstance, Int32 wParam);

        private delegate Int32 MouseHookDelegate(Int32 nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam);

        //[MarshalAs(UnmanagedType.FunctionPtr)]
        //private MouseHookDelegate _mouseProc;

        private readonly uint m_processId, m_threadId;
        private readonly IntPtr m_target;
        // Needed to prevent the GC from sweeping up our callback
        private readonly WinEventDelegate m_winEventDelegate;
        private IntPtr m_hook;
        System.Threading.Timer m_timer;


        public static IntPtr WinGetHandle(string wName)
        {
            IntPtr hWnd = IntPtr.Zero;
            foreach (Process pList in Process.GetProcesses())
            {
                //Form1.RichTextBox1.AppendText(pList.MainWindowTitle & vbCrLf)
                if (pList.MainWindowTitle.Contains(wName))
                {
                    hWnd = pList.MainWindowHandle;
                }
            }
            return hWnd;
        }

        public void Form2(string windowName, string className)
        {
            if (windowName == null && className == null) throw new ArgumentException("Either windowName or className must have a value");

            m_target = FindWindow(className, windowName);
            ThrowOnWin32Error("Failed to get target window");

            m_threadId = GetWindowThreadProcessId(m_target, out m_processId);
            ThrowOnWin32Error("Failed to get process id");

            m_winEventDelegate = WhenWindowMoveStartsOrEnds;
        }

        private void ThrowOnWin32Error(string message)
        {
            int err = Marshal.GetLastWin32Error();
            if (err != 0)
            {
                throw new Win32Exception(err, message);
            }
        }

        private RECT GetWindowLocation(IntPtr m_target)
        {

            RECT loc = default(RECT);
            GetWindowRect(m_target, out loc);

            // Do something useful with this to handle if the target window closes, etc.
            if (Marshal.GetLastWin32Error() != 0)
            {

            }
            return loc;
        }

        public void Subscribe()
        {
            // 10 = window move start, 11 = window move end, 0 = fire out of context

            m_hook = SetWinEventHook(10, 11, m_target, m_winEventDelegate, m_processId, m_threadId, 0);
            // System.Windows.Forms.MessageBox.Show(m_hook.ToString());
        }

        private void PollWindowLocation(object state)
        {
            dynamic location = GetWindowLocation(m_target);

            // TODO: Reposition your window with the values from location (or fire an event with it attached)
        }

        public void Unsubscribe()
        {

            UnhookWinEvent(m_target);
        }

        public void WindowEval()
        {

            WindowHandleInfo childWindows = new WindowHandleInfo(GetForegroundWindow());


            foreach (Process i in Process.GetProcessesByName("T"))
            {
                StringBuilder className = new StringBuilder(255);
                //Form1.RichTextBox1.AppendText("Main Window: " & Form2.GetActiveWindowTitle(i.MainWindowHandle) & " Class: " & Form2.GetClassName(i.MainWindowHandle, className, className.Capacity).ToString & "  " & vbCrLf)

                if (GetChildWindows(i.MainWindowHandle).Count() > 1)
                {
                    StringBuilder wTitle = new StringBuilder(255);
                    GetClassName(GetChildWindows(i.MainWindowHandle)[1], wTitle, wTitle.Capacity);
                    // Form1.RichTextBox1.AppendText("Handle: " & GetChildWindows(i.MainWindowHandle)(1).ToString & " ")
                    //Form1.RichTextBox1.AppendText("First Child Window: " & Form2.GetActiveWindowTitle(GetChildWindows(i.MainWindowHandle)(1)) & " Classname: " & wTitle.ToString & vbCrLf)

                    if (i.MainWindowHandle != IntPtr.Zero)
                    {
                        WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                        GetWindowPlacement(i.MainWindowHandle, ref placement);
                        switch (placement.showCmd)
                        {
                            case 1:
                                // TODO: might not be correct. Was : Exit Select

                                break;
                            case 2:
                                break;
                            case 3:

                                //HideBar(ref Form1.showFlag);
                                break; // TODO: might not be correct. Was : Exit Select


                        }

                    }
                    if (GetActiveWindowTitle(GetChildWindows(i.MainWindowHandle)[1]).Contains("Review Patient's Orders"))
                    {

                        ShowBar(ref showFlag);
                    }
                    else if (GetActiveWindowTitle(GetChildWindows(i.MainWindowHandle)[1]).Contains("Rounding List"))
                    {
                        HideBar(ref showFlag);
                    }
                }

                //For Each k As IntPtr In GetChildWindows(i.MainWindowHandle)
                //    Dim wTitle As New StringBuilder(255)
                //    Form2.GetClassName(k, wTitle, wTitle.Capacity)
                //    Form1.RichTextBox1.AppendText("Handle: " & k.ToString & " ")
                //    Form1.RichTextBox1.AppendText("Name: " & Form2.GetActiveWindowTitle(k) & " Classname: " & wTitle.ToString & vbCrLf)
                //Next
            }
        }

        private void WhenWindowMoveStartsOrEnds(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            const int WM_USER = 0x400;

            if (hwnd != m_target)
            {
                // We only want events from our target window, not other windows owned by the thread.
                return; //CallNextHook(10, 11, m_target, m_winEventDelegate, m_processId, m_threadId, 0);
            }

            if (eventType == 10)
            {
                // Starts
                // This is always the original position of the window, so we don't need to do anything, yet.
                m_timer = new System.Threading.Timer(PollWindowLocation, null, 10, Timeout.Infinite);
            }
            else if (eventType == 11)
            {
                m_timer.Dispose();
                m_timer = null;
                // TODO: Reposition your window with the values from location (or fire an event with it attached)
                dynamic location = GetWindowLocation(m_target);
                //If showFlag = True Then

                // System.Windows.Forms.MessageBox.Show("Helloooo"); //.AppendText("Active Window title: " + GetActiveWindowTitle(GetForegroundWindow()) + " \r\n Initial Top: " + r.top + " Initial Bottom: " + r.bottom + " Initial Left: " + r.left + " Initial Right: " + r.right);

                foreach (Process i in Process.GetProcessesByName("T"))
                {
                    //If i.MainWindowHandle <> IntPtr.Zero Then

                    //    Dim placement As New WINDOWPLACEMENT()
                    //        GetWindowPlacement(i.MainWindowHandle, placement)
                    //        Select Case placement.showCmd
                    //            Case 1
                    //                Form1.RichTextBox1.AppendText("Normal")
                    //                Exit Select
                    //            Case 2
                    //                Form1.RichTextBox1.AppendText("Minimized")
                    //                Exit Select
                    //            Case 3
                    //                Form1.RichTextBox1.AppendText("Maximized")
                    //                Exit Select
                    //        End Select

                    //End If
                    if (i.HandleCount > 1)
                    {
                        dynamic innerLoc = GetWindowLocation(GetChildWindows(i.MainWindowHandle)[1]);
                        if ((location.bottom - innerLoc.bottom) > 100)
                        {
                            SendMessage(Form2.WinGetHandle("HVA Valuebar"), WM_USER + 1002, (IntPtr)innerLoc.left + (innerLoc.bottom << 16), (IntPtr)(innerLoc.right - innerLoc.left) + (0 << 16));
                        }
                        else
                        {
                            if (location.top < 100)
                            {
                                SendMessage(Form2.WinGetHandle("HVA Valuebar"), WM_USER + 1002, (IntPtr)location.left + (location.bottom << 16), (IntPtr)(location.right - location.left) + (0 << 16));
                            }
                            else
                            {
                                SendMessage(Form2.WinGetHandle("HVA Valuebar"), WM_USER + 1002, (IntPtr)location.left + (location.top - 18 << 16), (IntPtr)(location.right - location.left) + (80 << 16));
                                //Form2.MoveWindow(Form2.WinGetHandle("hva-valuebar"), location.Left, location.Top - 80, location.Right - location.Left, 80, True)
                            }
                        }
                    }
                }

            }
            // Form2.MoveWindow(Form2.WinGetHandle("Notepad"), location.Left, location.Top - 80, location.Right - location.Left, 80, True)

            //Form1.RichTextBox1.AppendText("Width x Height: " & location.Right - location.Left & " X " & location.Bottom - location.Top & vbCrLf)
            return; //CallNextHook(10, 11, m_target, m_winEventDelegate, m_processId, m_threadId, 0);


        }

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private IntPtr _mouseHook;

        private const Int32 WH_MOUSE_LL = 14;

        System.Threading.Timer mouseTimer;
        //public bool HookMouse()
        //{

        //    //Debug.Print("Mouse Hooked")

        //    if (_mouseHook == IntPtr.Zero)
        //    {

        //        // _mouseProc = new MouseHookProc(mouse;

        //        _mouseHook = SetWindowsHookExW(WH_MOUSE_LL, mHookProc, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
        //        //m_timer = new System.Threading.Timer(mouseTimerTick, null, 30000, 30000);
        //        System.Windows.Forms.MessageBox.Show("Mouse Hooked " + _mouseHook);
        //    }

        //    return _mouseHook != IntPtr.Zero;
        //}

        //private void mouseTimerTick(object e)
        //{

        //    if (_mouseHook == IntPtr.Zero)
        //    {
        //        System.Windows.Forms.MessageBox.Show("Mouse Unhooked");

        //    }
        //    else
        //    {
        //        System.Windows.Forms.MessageBox.Show("Mouse Hooked " + _mouseHook);
        //    }
        //}
        //public void UnHookMouse()
        //{

        //    if (_mouseHook == IntPtr.Zero)
        //        return;
        //    UnhookWindowsHookEx(_mouseHook);
        //    _mouseHook = IntPtr.Zero;
        //    System.Windows.Forms.MessageBox.Show("Mouse Called to Unhooked");
        //  m_timer.Dispose();

        //}




        //private Int32 mHookProc(Int32 nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam)
        //{

        //    if (wParam.ToInt32() == 513)
        //    {

        //    }
        //    if (wParam.ToInt32() == 514)
        //    {
        //        //AppendText("Mouse UP");
        //        try
        //        {
        //            WindowEval();
        //        }
        //        catch(Exception e)
        //        {
        //            System.Windows.Forms.MessageBox.Show(e.Message);
        //                }
        //    }
        //    return CallNextHookEx(0, nCode, wParam, ref lParam);
        //}



        public delegate bool EnumWindowProcess(IntPtr Handle, IntPtr Parameter);


        public static List<IntPtr> ChildrenList;
        private static IntPtr[] GetChildWindows(IntPtr ParentHandle)
        {
            //Dim ListHandle As GCHandle = GCHandle.Alloc(ChildrenList)

            //Try
            //    EnumChildWindows(ParentHandle, AddressOf EnumWindow, GCHandle.ToIntPtr(ListHandle))
            //Finally
            //    If ListHandle.IsAllocated Then ListHandle.Free()
            //End Try
            //Dim Childlist As String = String.Join(". ", ChildrenList)

            //Return ChildrenList.ToArray

            List<IntPtr> ChildrenList = new List<IntPtr>();
            GCHandle ListHandle = GCHandle.Alloc(ChildrenList);
            try
            {
                EnumWindowProcess childProc = new EnumWindowProcess(EnumWindow);
                EnumChildWindows(ParentHandle, childProc, GCHandle.ToIntPtr(ListHandle));
            }
            finally
            {
                if (ListHandle.IsAllocated)
                    ListHandle.Free();
            }
            return ChildrenList.ToArray();
        }

        private static bool EnumWindow(IntPtr Handle, IntPtr Parameter)
        {
            GCHandle gch = GCHandle.FromIntPtr(Parameter);
            List<IntPtr> ChildrenList = gch.Target as List<IntPtr>;
            if (ChildrenList == null)
                throw new Exception("GCHandle Target could not be cast as List(Of IntPtr)");
            ChildrenList.Add(Handle);
            return true;
        }
    }
}
