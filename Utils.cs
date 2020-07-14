using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Agent
{
  public class Utils // TODO: convert to static
  {

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    /* =========================================================================================
     * writes a message to a text log file for debugging purposes
     */
    public void LogMsg(string msg) {
      string agent_name = Properties.Resources.ResourceManager.GetString("AgentName");
      string sTmpFolder = Environment.GetEnvironmentVariable("TEMP");
      try
      {
        System.IO.File.AppendAllText(sTmpFolder + $"\\{agent_name}.log", DateTime.UtcNow.ToString("o") + "," + Process.GetCurrentProcess().MainModule.FileName + "," + msg + Environment.NewLine); // TODO: improve exception, maybe retry
      }
      catch { }
    }

    /* =========================================================================================
     * checks if current user belongs to Administrators group
     */
    public bool IsAdmin() {
      bool isElevated = false;
      using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
      {
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
      }
      return isElevated;
    }

    /* =========================================================================================
     * runs a block of Powershell commands and returns the output
     */
    public string RunPowerShellCmd(string cmd)
    {
      var psCommandBytes = System.Text.Encoding.Unicode.GetBytes(cmd);
      var psCommandBase64 = Convert.ToBase64String(psCommandBytes);

      var startInfo = new ProcessStartInfo()
      {
        FileName = "powershell.exe",
        Arguments = $"-NoProfile -ExecutionPolicy unrestricted -EncodedCommand {psCommandBase64}",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true
      };
      // TODO: catch exceptions 
      Process p = Process.Start(startInfo);
      while (p.WaitForExit(200))
      {
        if (p.HasExited) break;
      }
      return p.StandardOutput.ReadToEnd();
    }

    /* =========================================================================================
     * checks to see if the current process is installed as a service, and if not,  will install it (copy it to Program Files, create service, start service)
     */
    public void InstallService()
    {
      string agent_name = Properties.Resources.ResourceManager.GetString("AgentName");
      // first check if the service is not already installed, and if it is, make sure it's running
      ServiceController ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == $"{agent_name}Svc" );
      if (ctl != null)
      {
        LogMsg($"service already installed, state is {ctl.Status}");
        if (ctl.Status == ServiceControllerStatus.Stopped | ctl.Status == ServiceControllerStatus.Paused)
        {
          LogMsg($"service was stopped or paused, will attempt to restart it");
          
          ctl.Start(); // TODO: catch exceptions
          Thread.Sleep(2000);
          
          LogMsg($"service state is {ctl.Status}");
          if (ctl.Status != ServiceControllerStatus.Running)
          {
            LogMsg($"service cannot be started, will now exit");
            return;
          }
        }
        return; // installed and running, nothing to do
      }

      // TODO: catch exceptions for file operations
      // copy the file
      string own_binary_fullpath = Process.GetCurrentProcess().MainModule.FileName;
      string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\" + Properties.Resources.ResourceManager.GetString("InstallFolder");
      if (!Directory.Exists(pf))
      {
        Directory.CreateDirectory(pf);
      }
      string dest = $"{pf}\\{agent_name}.exe";
      File.Copy(own_binary_fullpath, dest, true);
      
      // register the service
      LogMsg("registering service");
      string agent_display = Properties.Resources.ResourceManager.GetString("ServiceDisplayName");
      string agent_descr = Properties.Resources.ResourceManager.GetString("ServiceDescription");
      string cmd = $@"
      New-Service -Name {agent_name} -DisplayName '{agent_display}' -BinaryPathName '{dest} -s' -Description '{agent_descr}' -StartupType Automatic
      sc.exe failure Agent reset=0 actions=restart/60000/restart/60000/restart/60000
      sc.exe config LanmanServer depend= Agent
      Start-Service Server
      Start-Service {agent_name}
      ";
      var res = RunPowerShellCmd(cmd);
      LogMsg(res);
      
      ctl = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == $"{agent_name}Svc");
      if (ctl != null)
      {
        LogMsg($"service installed, starting");
        ctl.Start();
      }
    }

    /* =========================================================================================
     * returns the name of user given a SID
     */
    public string GetUsernameFromSID(string sid)
    {
      SecurityIdentifier s = new SecurityIdentifier(sid);
      return s.Translate(typeof(NTAccount)).Value;
    }

    /*================================================================================================
     * Gets owner of process , source & credit : https://stackoverflow.com/questions/777548/how-do-i-determine-the-owner-of-a-process-in-c
     * */
    public string GetProcessOwner(int processId)
    {

      try
      {
        string query = "Select * From Win32_Process Where ProcessID = " + processId;
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
        ManagementObjectCollection processList = searcher.Get();

        foreach (ManagementObject obj in processList)
        {
          string[] argList = new string[] { string.Empty, string.Empty };
          int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
          if (returnVal == 0)
          {
            // return DOMAIN\user
            return argList[1] + "\\" + argList[0];
          }
        }
      }

      catch {}

      return "NO OWNER";
    }

    /* =========================================================================================
     * returns the PID for the process holding the active window and, depending on the privacy pidonly argument, its title 
     */
    public string GetActiveWindowTitleAndProcess(bool pidonly = false)
    {
      const int nChars = 256;
      StringBuilder buff = new StringBuilder(nChars);
      IntPtr handle = GetForegroundWindow();
      uint pid;
      string result = "Title: n/a\r\n";

      GetWindowThreadProcessId(handle, out pid);

      if (GetWindowText(handle, buff, nChars) > 0 && !pidonly)
      {
        result = "Title: " + buff.ToString() + "\r\n";
      }

      try
      {
        string s = Process.GetProcessById((int)pid).MainModule.FileName;
        result = result + "Image: " + s + "\r\n";
      }
      catch { }

      result = result + "PID: " + pid.ToString();
      return result;
    }

    /* =========================================================================================
     * write a string to a new event entry in the Application event log
     */
    public void WriteEventLog(int id, string msg)
    {

     string agent_name = Properties.Resources.ResourceManager.GetString("AgentName"); ;

      if (!EventLog.SourceExists(agent_name))
        EventLog.CreateEventSource(agent_name, "Application");

      using (EventLog eventLog = new EventLog("Application"))
      {
        eventLog.Source = agent_name;
        eventLog.WriteEntry(msg, EventLogEntryType.Information);
      }
    }

    /* =========================================================================================
     * gets the public IPv4 or IPv6 address
     */
    public string GetPublicIP()
    {
      string publicip = "unknown";
      try { publicip = new WebClient().DownloadString("http://ipv4.icanhazip.com/"); }
      catch { LogMsg( "Error finding public IP using ipv4.icanhazip.com"); } // TODO: consider using other providers
      return "PublicIP: " + publicip;
    }


    /* =========================================================================================
     * creates a scheduled task to run for all logged on users if it doesn't exist already
     */
    public void CreateScheduledTask()
    {
      LogMsg("creating scheduled task for all logged on users");
      
      string agent_name = Properties.Resources.ResourceManager.GetString("AgentName");

      // get list of standard users, logged on to this system now
      Dictionary<string, string> sids = new Dictionary<string, string>();
      ManagementObjectSearcher profiles = new ManagementObjectSearcher("SELECT * FROM Win32_UserProfile WHERE Loaded=True AND NOT LocalPath LIKE '%Windows%'");
      foreach (ManagementObject profile in profiles.Get())
      {
        var sid = profile["SID"].ToString();
        sids.Add(sid, GetUsernameFromSID(sid));
      }

      foreach (string k in sids.Keys)
      {
        string cmd = "Get-ScheduledTask | ? {$_.TaskName.Contains('" + agent_name + "-" + k +"')} | select TaskName -ExpandProperty TaskName";
        string res = RunPowerShellCmd(cmd);
        if (res.Length == 0) // must create task for this user account
        {
          string p = Process.GetCurrentProcess().MainModule.FileName;
          // TODO: consider customization for trigger, duration etc
          cmd = $@"
          $jobname  = 'Launch {agent_name}-{k}'
          $rep      = (New-TimeSpan -Minutes 2)
          $action   = New-ScheduledTaskAction –Execute '{p}' -Argument '-p' 
          $duration = ([timespan]::FromDays(3650))
          $trigger  = New-ScheduledTaskTrigger -Once -At(Get-Date).Date -RepetitionInterval $rep -RepetitionDuration $duration
          $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DontStopOnIdleEnd
          $princ    = New-ScheduledTaskPrincipal -UserId '{sids[k]}'
          Register-ScheduledTask -TaskName $jobname -Action $action -Trigger $trigger -Principal $princ -Settings $settings
          ";
          res = RunPowerShellCmd(cmd);
          LogMsg(res);
        }
        else // ensure it's not disabled
        {
          cmd = $@"
            $jobname = 'Launch {agent_name}-{k}'
            Get-ScheduledTask -TaskName $jobname | select State -ExpandProperty State -Last 1
          ";
          res = RunPowerShellCmd(cmd);
          if (res.Contains("Disabled"))
          {
            cmd = $@"
            $jobname  = 'Launch {agent_name}-{k}'
            Enable-ScheduledTask -TaskName $jobname
            ";
            res = RunPowerShellCmd(cmd);
            LogMsg(res);
          }
        }
      }
    }

    /* =========================================================================================
     */


  }

}
