using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using System.Management;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;


namespace HVA_TrayApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region "Signatures"
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextW")]
        public static extern int GetWindowTextW([InAttribute()]IntPtr hWnd, [OutAttribute(), MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "MoveWindow", CharSet = CharSet.Auto)]
        public static extern bool MoveWindow(IntPtr hWnd, Int32 X, Int32 Y, Int32 nWidth, Int32 nHeight, bool bRepaint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(System.IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        //private delegate Int32 MouseHookDelegate(Int32 nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam);
        //[MarshalAs(UnmanagedType.FunctionPtr)]

        //[DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        //public static extern bool UnhookWindowsHookEx(IntPtr hook);

        //[DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        //private static extern Int32 CallNextHookEx(Int32 idHook, Int32 nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetModuleHandleW(IntPtr fakezero);

        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        Form2 dockHlp;
        static IntPtr hvaHwnd;
        
        System.Windows.Forms.NotifyIcon trayIcon = new System.Windows.Forms.NotifyIcon()
        {
            ContextMenuStrip = new ContextMenuStrip(),
            Icon = HVA_TrayApp.Properties.Resources.HVA_Off,
            Text = "Meditech is not running.",
            Visible = true,
        };

        //private MouseListener _mListner;

        //private System.Windows.Forms.NotifyIcon notifyIcon = null;
        //private Dictionary<string, System.Drawing.Icon> IconHandles = null;

        public MainWindow()
        {
            InitializeComponent();

            trayIcon.Click += trayIcon_Click;
            trayIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            trayIcon.ContextMenuStrip.Items.Add("Debug Window", null, debugWindow_Click);
            trayIcon.ContextMenuStrip.Items.Add("Quit HVA Application", null, quitApp_Click);

            this.Title = ("HVA Tray App");
            dynamic monitoredProcess = "T.exe";
            WqlEventQuery queryStart = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1), "TargetInstance isa \"Win32_Process\" And TargetInstance.Name = \"" + monitoredProcess + "\"");
            WqlEventQuery queryEnd = new WqlEventQuery("__InstanceDeletionEvent", new TimeSpan(0, 0, 1), "TargetInstance isa \"Win32_Process\" And TargetInstance.Name = \"" + monitoredProcess + "\"");
            watcher = new ManagementEventWatcher();
            watcher2 = new ManagementEventWatcher();
            watcher.Query = queryStart;
            watcher2.Query = queryEnd;
            //This starts watching asynchronously, triggering EventArrived events every time a new event comes in
            watcher.Start();
            watcher2.Start();
        }

        //Tray Icon Click Event
        private void trayIcon_Click(object sender, EventArgs e)
        {
            if (trayIcon.ContextMenuStrip.Visible == true)
            {
                trayIcon.ContextMenuStrip.Hide();
            }
            else
            {
                trayIcon.ContextMenuStrip.Show(System.Windows.Forms.Control.MousePosition);
                //trayIcon.ContextMenuStrip.Visible = true;
            }
        }

        //Check for Context Menu Items
        private void ContextMenuStrip_Opening(object sender, EventArgs e)
        {
            if (trayIcon.ContextMenuStrip.Items.Count == 0)
            {
                trayIcon.ContextMenuStrip.Items.Add("Debug Window", null, debugWindow_Click);
                trayIcon.ContextMenuStrip.Items.Add("Quit HVA Application", null, quitApp_Click);
                //trayIcon.ContextMenuStrip.ItemClicked += new ToolStripItemClickedEventHandler(trayIconMenuItem_Clicked);
            }
        }

        //Close HVA App on exit
        private void quitApp_Click(object sender, EventArgs e)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure want to quit HVA Value Bar?", "Exit Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                if (System.Diagnostics.Process.GetProcessesByName("hva-valuebar") != null)
                {
                    Process[] pProcess = System.Diagnostics.Process.GetProcessesByName("hva-valuebar");
                    foreach (Process p in pProcess)
                    {
                        p.Kill();
                    }
                }

                if (hHook != 0)
                {
                    dockHlp.Unsubscribe();
                    DeactivateMouseHook();
                }

                trayIcon.Visible = false;
                trayIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            }

        }

        //Show Debug window on click
        private void debugWindow_Click(object sender, EventArgs e)
        {
            if (App.Current.MainWindow.IsVisible == true)
            {
                App.Current.MainWindow.Hide();
            }
            else
            {
                App.Current.MainWindow.Show();
                this.WindowState = WindowState.Normal;
            }    
        }

        //Hide Debug window on close
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            App.Current.MainWindow.Hide();
        }

        //Hide Debug window on minimize
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                App.Current.MainWindow.Hide();
            }
        }

        private ManagementEventWatcher withEventsField_watcher;
        private ManagementEventWatcher watcher
        {
            get { return withEventsField_watcher; }
            set
            {
                if (withEventsField_watcher != null)
                {
                    withEventsField_watcher.EventArrived -= Watcher_EventArrived;
                }
                withEventsField_watcher = value;
                if (withEventsField_watcher != null)
                {
                    withEventsField_watcher.EventArrived += Watcher_EventArrived;
                }
            }
        }
        private ManagementEventWatcher withEventsField_watcher2;
        private ManagementEventWatcher watcher2
        {
            get { return withEventsField_watcher2; }
            set
            {
                if (withEventsField_watcher2 != null)
                {
                    withEventsField_watcher2.EventArrived -= Watcher2_EventArrived;
                }
                withEventsField_watcher2 = value;
                if (withEventsField_watcher2 != null)
                {
                    withEventsField_watcher2.EventArrived += Watcher2_EventArrived;
                }
            }
        }
        public static bool showFlag = false;

        //Show HVA Bar
        public static void ShowBar(ref bool showFlag)
        {
            if (showFlag == false)
            {
                showFlag = true;
                RECT r = new RECT();
                const int WM_USER = 0x400;
                GetWindowRect(GetForegroundWindow(), ref r);
                SendMessage(hvaHwnd, WM_USER + 1000, (IntPtr)1, IntPtr.Zero);
                SendMessage(hvaHwnd, WM_USER + 1002, (IntPtr)r.left + (r.top - 18 << 16), (IntPtr)(r.right - r.left) + (80 << 16));
            }
            else
            {
                return;
            }
        }

        //Hide HVA Bar
        public static void HideBar(ref bool showFlag)
        {
            if (showFlag == true)
            {
                showFlag = false;
                const int WM_USER = 0x400;
                //Dim k As IntPtr = Form2.WinGetHandle("HVA Valuebar")
                // RichTextBox1.AppendText(vbCrLf & "!!Hide Bar!!" & vbCrLf)
                SendMessage(hvaHwnd, WM_USER + 1000, (IntPtr)0, IntPtr.Zero);
            }
            else
            {
                return;
            }

        }
        
        //Launch HVA on Meditech Launch Event
        private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //Do stuff with the startup event
            //TextBox1.AppendText("Watcher_EventArrived");
            trayIcon.Icon = HVA_TrayApp.Properties.Resources.HVA_On;
            trayIcon.Text = "Meditech is running.";
                const int WM_USER = 0x400;
            //MainWindow.VisibilityProperty.
            //Form2.GetWindowRect(Form2.GetForegroundWindow(), r)
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            //NotifyIcon1.Icon = My.Resources.HVA_On;
            //NotifyIcon1.Text = "Meditech is Running";
            this.Dispatcher.Invoke(() =>
            {
                TextBox1.Clear();
            });
            dynamic procName = Process.GetProcessesByName("T");
            foreach (Process procs in procName)
            {
                UpdateTextBox(procs.Id);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo("\\\\CWDVV1DVAC01\\C$\\Users\\HVU7259\\Desktop\\hva-valuebar.exe");
            //startInfo.FileName = "\\\\CWDVV1DVAC01\\C$\\Users\\HVU7259\\Desktop\\hva-valuebar.exe";
            //startInfo.RedirectStandardInput = true;
            //startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            proc = Process.Start(startInfo);  //Path to HVA GUI 

            Thread.Sleep(300); //Wait for 
           
            hvaHwnd = Form2.WinGetHandle("HVA Valuebar");
            // RichTextBox1.AppendText("HVA Handle: " & Form2.GetActiveWindowTitle(Form2.GetForegroundWindow()) & vbCrLf)
           
                SendMessage(hvaHwnd, WM_USER + 1000, (IntPtr)0, IntPtr.Zero);
            ProcessThreadCollection currentThreads = Process.GetCurrentProcess().Threads;
            this.Dispatcher.Invoke(() =>
            {
                TextBox1.AppendText("HVA Bar Handle: " + hvaHwnd.ToString() + "\r\n");
            });
            //, CType(Location.Left + (Location.Top - 80 << 16), IntPtr), CType((Location.Right - Location.Left) + (10 << 16), IntPtr)
        }

        //Invoke on UI Thread
        public void UpdateTextBox(int procName)
        {

            //Get AD groups membership for user
          
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, "HCA");
            UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, GetProcessOwner1(procName));
            PrincipalSearchResult<Principal> groups = usr.GetAuthorizationGroups();
            IEnumerable<string> groupNames = groups.Select(x => x.SamAccountName);
            GroupPrincipal spGroup = default(GroupPrincipal);
            GroupPrincipal spGroup2 = default(GroupPrincipal);

            //dockHlp = new Form2(GetActiveWindowTitle(GetForegroundWindow()), null);
            dynamic pName = Process.GetProcessesByName("T");
            foreach (Process procs in pName)
            {
                dockHlp = new Form2(procs.MainWindowTitle, null);
                System.Windows.Application.Current.Dispatcher.Invoke(       //Invoke hooks on main thread.
                () =>
                {
                    ActivateMouseHook();
                    dockHlp.Subscribe();
                });
            }

            this.Dispatcher.Invoke(() =>
            {
                TextBox1.AppendText("User ID: " + GetProcessOwner1(procName) + "\r\n");
            });

            foreach (string group in groupNames)
            {
                this.Dispatcher.Invoke(() =>
                {
                    TextBox1.AppendText("Group: " + group + "\r\n");
                });
            }

            //RichTextBox1.AppendText("Mouse hooked=" & dock.HookMouse() & vbCrLf)

            //try
            //{
            //    //spGroup = GroupPrincipal.FindByIdentity(ctx, "CWDV_AppAdmin_HVAValueBar");
            //    ////CWDV_AppAdmin_HVAValueBar
            //    //spGroup2 = GroupPrincipal.FindByIdentity(ctx, "Administrators");
            //    //TextBox1.AppendText("User is a member of HVA_Group: " + usr.IsMemberOf(spGroup) + "\r\n");
            //    //TextBox1.AppendText("User is a member of Administrators: " + usr.IsMemberOf(spGroup2) + "\r\n");
            //}
            //catch
            //{
            //    // RichTextBox1.AppendText("AD Group NOT FOUND" & vbCrLf)
            //}

            //TextBox1.AppendText("UpdateTextBox");
            

            //RichTextBox1.AppendText("Initial Top: " & r.top & " Initial Bottom: " & r.bottom & " Initial Left: " & r.left & " Initial Right: " & r.right & vbCrLf &
            //       "Initial Width x Height: " & r.right - r.left & " X " & r.bottom - r.top & vbCrLf)
        }

        //private void WindowUpdate()
        //{
        //    RECT r = new RECT();
        //    GetWindowRect(GetForegroundWindow(), ref r);
        //    this.Dispatcher.Invoke(() =>
        //    {
        //        TextBox1.AppendText("Mouse Up");//TextBox1.AppendText("Active Window title: " + GetActiveWindowTitle(GetForegroundWindow()) + " \r\n Initial Top: " + r.top + " Initial Bottom: " + r.bottom + " Initial Left: " + r.left + " Initial Right: " + r.right);
        //    });
        //}

        //Kill HVA on Meditech Exit Event
        private void Watcher2_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //Do stuff with the startup event
            //TextBox1.AppendText("Watcher2_EventArrived");
            trayIcon.Icon = HVA_TrayApp.Properties.Resources.HVA_Off;
            trayIcon.Text = "Meditech is not running.";
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            Process[] pProcess = System.Diagnostics.Process.GetProcessesByName("hva-valuebar");
            //NotifyIcon1.Icon = My.Resources.HVA_Off;
            //NotifyIcon1.Text = "Meditech is NOT Running";
            hvaHwnd = IntPtr.Zero;
            
            foreach (Process p in pProcess)
            {
                p.Kill();
                DeactivateMouseHook();
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (hvaHwnd != IntPtr.Zero)
            {
                DeactivateMouseHook();
                dockHlp.Unsubscribe();
            }
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
      
        //Get User by PID
        public string GetProcessOwner(int processId)
        {
           //TextBox1.AppendText("GetProcessOwner");
            dynamic query = "Select * From Win32_Process Where ProcessID = " + processId;
            dynamic searcher = new ManagementObjectSearcher(query);
            dynamic processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = {
                string.Empty,
                string.Empty
            };
                dynamic returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return ("User: " + argList[0] + "\r\n" + "Domain: \\\\" + argList[1] + "\r\n");
                }
            }
            return "NO OWNER";


        }

        public static string GetActiveWindowTitle(IntPtr hWnd)
        {
            //Dim hWnd As IntPtr = GetForegroundWindow()
            //Dim hChild As IntPtr = IntPtr.Zero
            //While (hChild = FindWindowEx(hParent, hChild, null, null)) IsNot IntPtr.Zero
            //End While

           //TextBox1.AppendText("GetActiveWindowTitle");
            if (!hWnd.Equals(IntPtr.Zero))
            {
                int lgth = GetWindowTextLength(hWnd);
                System.Text.StringBuilder wTitle = new System.Text.StringBuilder("", lgth + 1);
                if (lgth > 0)
                {
                    GetWindowTextW(hWnd, wTitle, wTitle.Capacity);
                }

                uint wProcID = 0;
                GetWindowThreadProcessId(hWnd, out wProcID);

                Process Proc = Process.GetProcessById((int)wProcID);
                string wFileName = "";
                try
                {
                    wFileName = Proc.MainModule.FileName;
                }
                catch (Exception ex)
                {
                    wFileName = "";
                }
                return (wTitle.ToString());
                //& vbCrLf & "Handle ID: " & hWnd.ToString & vbCrLf) 'GetForegroundWindow().ToString & vbCrLf & GetWindowThreadProcessId(hWnd, wProcID).ToString & vbCrLf & wTitle.ToString 
            }
            else
            {
                return null;
            }
        }

        //Get User by SSO creds
        public string GetProcessOwner1(int processId)
        {
            dynamic v = new VergenceSSOLIB.Vergence();
            if (VergenceSSOLIB.Vergence.IsSSOMachine())
            {
                dynamic user = VergenceSSOLIB.Vergence.GetSSOUser();
                return (user);
            }
            else
            {
                return (Environment.UserName);
            }
        }

        System.Threading.Timer m_timer;
       
        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private HookProc MouseHookProcedure;
        private const int WH_MOUSE_LL = 14;

        private void ActivateMouseHook()
        {
            if (hHook == 0)
            {
                MouseHookProcedure = new HookProc( MouseHookProc);
                hHook = SetWindowsHookEx(WH_MOUSE_LL,
                                 MouseHookProcedure,
                                 Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
                                0);
            }

            System.Windows.Application.Current.Dispatcher.Invoke(       //Invoke hooks on main thread.
              () =>
              {
                  TextBox1.AppendText("Mouse Hooked: " + hHook + "\r\n");
              });
            //System.Windows.Forms.MessageBox.Show("Mouse Hooked: " + hHook);

            //m_timer = new System.Threading.Timer(mouseTimerTick, null, 30000, 30000);
        }

        //private void mouseTimerTick(object e)
        //{

        //    if (hHook == 0)
        //    {
        //        System.Windows.Forms.MessageBox.Show("Mouse Unhooked");
        //    }
        //    else
        //    {
        //        System.Windows.Forms.MessageBox.Show("Mouse Hooked " + hHook);
        //    }
        //}
        static int hHook = 0;
        private void DeactivateMouseHook()
        {
            bool ret = UnhookWindowsHookEx(hHook);
            hHook = 0;
            System.Windows.Application.Current.Dispatcher.Invoke(       //Invoke hooks on main thread.
              () =>
              {
                  TextBox1.AppendText("Mouse Called Unhook: " + ret + "\r\n");
              });
        }

        private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //MouseHookStruct MyMouseHookStruct = (MouseHookStruct)
            //    Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));

            //if (wParam.ToInt32() == 513)
            //{

            //}
            if (wParam.ToInt32() == 514)
            {

                new Thread(delegate ()
                {
                    m_timer = new System.Threading.Timer(dockHlp.WindowEval, null, 10, 500);
                }).Start();
                //Thread thread = new Thread(WindowUpdate);
                //thread.Start();
            }
            return CallNextHookEx(WH_MOUSE_LL, nCode, wParam, lParam);
        }

        //Get Parent and all Child window handles.  Set Global Hooks.
        public class Form2
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

            public Form2(string windowName, string className)
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

            public void WindowEval(object state)
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
                                case 1: //Normal
                                   break;
                                case 2: //Minimized
                                    break;
                                case 3: //Maximized
                                    HideBar(ref showFlag);
                                    break; 
                            }
                        }
                        if (GetActiveWindowTitle(GetChildWindows(i.MainWindowHandle)[1]).Contains("Review Patient's Orders"))
                        {
                           ShowBar(ref showFlag);
                        }
                        else if (GetActiveWindowTitle(GetChildWindows(i.MainWindowHandle)[1]).Contains("Rounding List") || GetActiveWindowTitle(GetChildWindows(i.MainWindowHandle)[1]).Contains("Physician Desktop")) 
                        {
                            HideBar(ref showFlag);
                        }
                    }
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
                                }
                            }
                        }
                    }
                }
                return;
            }

            private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            private IntPtr _mouseHook;

            private const Int32 WH_MOUSE_LL = 14;

            System.Threading.Timer mouseTimer;
        
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

        public class WindowHandleInfo
        {
            private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

            [DllImport("user32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);


            private IntPtr _MainHandle;
            public WindowHandleInfo(IntPtr handle)
            {
                this._MainHandle = handle;
            }

            public List<IntPtr> GetAllChildHandles()
            {
                List<IntPtr> childHandles = new List<IntPtr>();

                GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
                IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

                try
                {
                    EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                    EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
                }
                finally
                {
                    gcChildhandlesList.Free();
                }
                return childHandles;
            }

            private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
            {
                GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

                if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
                {
                    return false;
                }

                List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
                childHandles.Add(hWnd);

                return true;
            }
        }

        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        public struct RECT
        {
            public Int32 left;
            public Int32 top;
            public Int32 right;
            public Int32 bottom;
        }

        public struct MSLLHOOKSTRUCT
        {
            public Point pt;
            public Int32 mouseData;
            public Int32 flags;
            public Int32 time;
            public IntPtr extra;
        }

        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }


        #endregion

        
    }
}
