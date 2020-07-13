using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;

namespace Agent
{
  static class Program
  {
    static void Main(string[] argv)
    {
      
      Utils utils = new Utils();
      utils.LogMsg("started");

      if (argv.Contains("-i"))
      {
        utils.LogMsg("installation invoked");
        if (utils.IsAdmin())
        {
          utils.InstallService();
        }
        else
        {
          utils.LogMsg("ERROR: not admin, fatal");
          MessageBox.Show("Program must be started with elevated access rights for installation.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        return;
      }

      if (argv.Contains("-s"))
      {
        utils.LogMsg("will run as a service");
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[]
        {
         new AgentSvc()
        };
        ServiceBase.Run(ServicesToRun);
        return;
      }

      if (argv.Contains("-u"))
      {
        if (utils.IsAdmin())
        {
          utils.LogMsg("will run service code in user space");
          AgentSvc src = new AgentSvc();
          src.DoWork(argv);
        }
        return;
      }

      if (argv.Contains("-p"))
      {
        // TODO: consider measures against TASKKILL /IM
        Process.Start(Process.GetCurrentProcess().MainModule.FileName);
        return;
      }

      // TODO: only start if not running already
      Process[] ps = Process.GetProcessesByName(Properties.Resources.ResourceManager.GetString("AgentName"));
      int count = 0;
      foreach (Process p in ps)
      {
        string tmp = Process.GetCurrentProcess().StartInfo.Environment["USERDOMAIN"] + "\\" + Process.GetCurrentProcess().StartInfo.Environment["USERNAME"];
        if (utils.GetProcessOwner(p.Id) == tmp) count++;
      }
      if (count >= 2)
      {
        utils.LogMsg("another instance already running, will now exit");

        return;
      }
      utils.LogMsg("running in user space");
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new UserLand());

    }
  }
}
