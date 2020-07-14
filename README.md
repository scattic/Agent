Agent
=====

What is it?
-----------

A small agent which logs activity from User Space into Windows EventLog. Log entries can then be shipped to a SIEM by a collector such as WinLogBeat.
The agent has a basic persistence mechanism, as it can run both as a Windows service and as a User Space application. When running as a service it will make sure 
that a Scheduled Task exists to start the Agent as a normal application every X minutes under the/any logged on user account. 

Syntax
------

**agent.exe [-i] [-s] [-u] [-p]**

-i : installs the application (copies the exe to Program Files, registers & starts the service). Requires elevated access rights.

-s : runs as a service. Don't use this argument when starting normally (from User Space). 

-u : runs service code, but from the User Space. Use this for debugging purposes.

-p : spawns itself then quit, used when started by Task Scheduler

Source notes
------------

Code shows how to:
* Build a Windows Service, and run the Service from User land to troubleshoot
* Threading with Timers
* Create EventLog entries, Scheduled Tasks
* Run scripts (blocks of code) using PowerShell, and WMI queries
* Calling a Web service for info

To customize the code, one could start to:

* Change Resources\AgentName, which will define: 
  * name of service (\<AgentName\>Svc)
  * EventLog source
  * name of ScheduledTask ("Launch \<AgentName\>\-\<SID\>")
  * name of executable file installed under \\Program Files

* Change configurable strings in Resources:
  * InstallFolder for the folder name under \\Program Files
  * ServiceDisplay: display name for service
  * ServiceDescription: description for service
