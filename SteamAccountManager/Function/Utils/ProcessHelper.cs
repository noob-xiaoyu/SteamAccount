using System;
using System.Diagnostics;
using System.Linq;

namespace SteamAccountManager.Utils
{
    public static class ProcessHelper
    {
        private const string SteamProcessName = "steam";

        /// <summary>
        /// 检查 Steam 进程是否正在运行
        /// </summary>
        public static bool IsSteamRunning()
        {
            return Process.GetProcessesByName(SteamProcessName).Any();
        }

        /// <summary>
        /// 终止所有正在运行的 Steam 进程
        /// </summary>
        public static void KillSteamProcesses()
        {
            var processes = Process.GetProcessesByName(SteamProcessName);
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000); // 等待最多5秒
                    }
                }
                catch (Exception)
                {
                    // 静默忽略错误
                }
            }
        }
    }
}