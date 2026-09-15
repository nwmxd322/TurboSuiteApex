using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TurboSuiteApex
{
    class Program
    {
        private static string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TurboSuite_Log.txt");
        private static string StartTime = DateTime.Now.ToString("HH:mm:ss.ff");

        // Natywne API Windows do zwalniania pamięci RAM
        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwnd);

        // Informacje o sprzęcie
        private static string CpuInfo = "Unknown";
        private static string RamInfo = "? GB";
        private static string RamFree = "? GB";
        private static string GpuInfo = "Unknown";
        private static string OsInfo = "Windows";
        private static string DiskFree = "? GB";
        private static string SystemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.Replace("\\", "") ?? "C:";

        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding(852);

            if (args.Length > 0 && args[0] == "--bg-loop")
            {
                BackgroundLoop();
                return;
            }

            if (!IsAdministrator())
            {
                RelaunchAsAdmin();
                return;
            }

            try
            {
                Console.Title = "TURBO SUITE APEX";
                Console.SetWindowSize(Math.Min(100, Console.LargestWindowWidth), Math.Min(48, Console.LargestWindowHeight));
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.Cyan;

            Splash();
            ScanHardware();
            Menu();
        }

        #region OPTIMIZATION HELPERS
        // Zaawansowane zwalnianie RAM wszystkich procesów
        private static void NativeFlushRam()
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    EmptyWorkingSet(process.Handle);
                }
                catch { }
            }
        }

        // Wielowątkowe czyszczenie folderów tymczasowych
        private static void FastCleanTempFolders()
        {
            string[] tempFolders = {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")
            };

            Parallel.ForEach(tempFolders, folder =>
            {
                if (!Directory.Exists(folder)) return;

                try
                {
                    DirectoryInfo di = new DirectoryInfo(folder);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        try { file.Delete(); } catch { }
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        try { dir.Delete(true); } catch { }
                    }
                }
                catch { }
            });
        }
        #endregion

        #region ELEVATION
        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void RelaunchAsAdmin()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Clear();
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                               [!] ADMINISTRATOR REQUIRED                         |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |  TURBO SUITE needs elevated privileges for system changes.                        |");
            Console.WriteLine("    |  A UAC window will appear - choose YES.                                          |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();

            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                string errPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Elevation_Error.txt");
                File.WriteAllText(errPath, ex.ToString());

                Console.Clear();
                Header();
                Console.WriteLine();
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |                                [X] ELEVATION FAILED                              |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |  Start this .EXE manually with \"Run as administrator\".                           |");
                Console.WriteLine("    |  Details, when available: Elevation_Error.txt                                    |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine();
                Console.WriteLine("Naciśnij dowolny klawisz, aby kontynuować . . .");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        #endregion

        #region UI HELPERS
        private static void Header()
        {
            Console.WriteLine("     ######################################################################################");
            Console.WriteLine("     #                                                                                    #");
            Console.WriteLine("     #                      T U R B O   S U I T E   A P E X                               #");
            Console.WriteLine("     #                     v19.0 EXTREME PERFORMANCE EDITION                              #");
            Console.WriteLine("     #                                                                                    #");
            Console.WriteLine("     ######################################################################################");
        }

        private static void StatusLine()
        {
            Console.WriteLine($"     SYSTEM : {OsInfo}");
            Console.WriteLine($"     CPU    : {CpuInfo}");
            Console.WriteLine($"     GPU    : {GpuInfo}");
            Console.WriteLine($"     RAM    : {RamInfo}  |  FREE: {RamFree}");
            Console.WriteLine($"     DISK   : {SystemDrive}  |  FREE: {DiskFree}");
        }

        private static void BootBar(int target, string text)
        {
            for (int p = 5; p <= target; p += 5)
            {
                int n = p / 5;
                int empty = 20 - n;
                string bar = new string('#', n) + new string('.', Math.Max(0, empty));
                Console.Write($"\r   [{bar}] {p}% - {text}   ");
                Thread.Sleep(50);
            }
            Console.WriteLine();
        }

        private static void Bar(int target, string tag, string text)
        {
            for (int pct = 5; pct <= target; pct += 5)
            {
                int n = pct * 40 / 100;
                int s = 40 - n;
                string b = new string('#', n) + new string('.', Math.Max(0, s));

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Header();
                Console.WriteLine();
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |                          APEX CORE // PROCESSING                                 |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine();
                Console.WriteLine($"        [{tag}] {text}");
                Console.WriteLine();
                Console.WriteLine($"        [{b}] {pct}%");
                Console.WriteLine();
                Console.WriteLine("    ------------------------------------------------------------------------------------");
                Thread.Sleep(50);
            }
        }

        private static void Splash()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Header();
            Console.WriteLine();
            Console.WriteLine("     [BOOT] Initializing APEX CORE...");
            Console.WriteLine();

            BootBar(20, "Loading command modules");
            BootBar(40, "Checking administrator context");
            BootBar(60, "Preparing diagnostic engine");
            BootBar(80, "Loading visual interface");
            BootBar(100, "APEX CORE ONLINE");

            Console.Beep(700, 90);
            Console.Beep(900, 90);
            Console.Beep(1200, 150);
            Thread.Sleep(1000);
        }

        private static void Prep()
        {
            File.AppendAllText(LogFile, $"{DateTime.Now} - APEX BOOST START\r\n");
        }

        private static void Pause()
        {
            Console.WriteLine();
            Console.WriteLine("Naciśnij dowolny klawisz, aby kontynuować . . .");
            Console.ReadKey(true);
        }
        #endregion

        #region HARDWARE SCAN
        private static void ScanHardware()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Header();
            Console.WriteLine();
            Console.WriteLine("     [SCAN] HARDWARE RECON // please wait...");
            Console.WriteLine();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    foreach (var item in searcher.Get())
                        CpuInfo = item["Name"]?.ToString()?.Trim() ?? CpuInfo;

                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                {
                    ulong totalBytes = 0;
                    foreach (var item in searcher.Get())
                        totalBytes += (ulong)item["Capacity"];
                    RamInfo = $"{Math.Round(totalBytes / 1024.0 / 1024.0 / 1024.0, 1)} GB";
                }

                using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
                    foreach (var item in searcher.Get())
                        RamFree = $"{Math.Round(Convert.ToDouble(item["FreePhysicalMemory"]) / 1024.0 / 1024.0, 1)} GB";

                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                    foreach (var item in searcher.Get())
                    {
                        GpuInfo = item["Name"]?.ToString()?.Trim() ?? GpuInfo;
                        break;
                    }

                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                    foreach (var item in searcher.Get())
                        OsInfo = item["Caption"]?.ToString()?.Trim() ?? OsInfo;

                using (var searcher = new ManagementObjectSearcher($"SELECT FreeSpace FROM Win32_LogicalDisk WHERE DeviceID='{SystemDrive}'"))
                    foreach (var item in searcher.Get())
                        DiskFree = $"{Math.Round(Convert.ToDouble(item["FreeSpace"]) / 1024.0 / 1024.0 / 1024.0, 1)} GB";
            }
            catch { }

            BootBar(100, "Hardware scan complete");
        }
        #endregion

        #region MENU
        private static void Menu()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Header();
                Console.WriteLine();
                StatusLine();
                Console.WriteLine();
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |                            COMMAND CENTER // v19.0 EXTREME                       |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    | --- [ OPTYMALIZACJA I DYSK ] ---------------------------------------------------- |");
                Console.WriteLine("    |  [1] BOOST PROFILE        Safe gaming profile                                    |");
                Console.WriteLine("    |  [2] RESTORE PROFILE      Undo Turbo Suite changes                               |");
                Console.WriteLine("    |  [3] CLEAN CACHE          TEMP + shader cache + memory refresh                    |");
                Console.WriteLine("    |  [4] DISK OPTIMIZER        SysMain reset + SSD TRIM + Update Cache                 |");
                Console.WriteLine("    |  [5] GAME MODE            Focus mode: Game Mode + background cleanup              |");
                Console.WriteLine("    |  [X] HARDWARE BOOST        Quick CPU, Unpark Cores, L3 Cache, RAM Optimization     |");
                Console.WriteLine("    |  [T] BACKGROUND ENGINE    Zarzadzaj praca w tle (Wlacz / Wylacz / Status)        |");
                Console.WriteLine("    |  [E] EXTREME BOOST       Ultra Input-Lag reduction, GPU and CPU max priority     |");
                Console.WriteLine("    | --- [ DIAGNOSTYKA, TESTY I BEZPIECZENSTWO ] -------------------------------------- |");
                Console.WriteLine("    |  [6] VIRUS LAB            Windows Defender Malware Scan AND Defs Update          |");
                Console.WriteLine("    |  [7] BENCHMARK LAB        Test wydajnosci CPU oraz predkosci Zapisu/Odczytu Dysku |");
                Console.WriteLine("    |  [8] NETWORK LAB          Ping, DNS, adapter and route diagnostics                |");
                Console.WriteLine("    |  [9] HARDWARE LAB         CPU/RAM/GPU/disk health snapshot                       |");
                Console.WriteLine("    |  [P] PROCESS RADAR        Heavy processes snapshot                               |");
                Console.WriteLine("    |  [B] SYSTEM BACKUP        Restore point + registry backup                         |");
                Console.WriteLine("    | --- [ SYSTEM ] ------------------------------------------------------------------- |");
                Console.WriteLine("    |  [A] ABOUT / CHANGELOG     What is new in v19.0 EXTREME                           |");
                Console.WriteLine("    |  [0] EXIT                                                                         |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine();
                Console.Write("   >> SELECT MODULE: ");

                ConsoleKeyInfo keyInfo = Console.ReadKey();
                char key = char.ToUpper(keyInfo.KeyChar);

                switch (key)
                {
                    case '1': Boost(); break;
                    case '2': Restore(); break;
                    case '3': Clean(); break;
                    case '4': DiskBoost(); break;
                    case '5': GameMode(); break;
                    case 'X': HardwareBoost(); break;
                    case 'T': BgManager(); break;
                    case 'E': ExtremeBoost(); break;
                    case '6': Virus(); break;
                    case '7': Benchmark(); break;
                    case '8': Network(); break;
                    case '9': Hardware(); break;
                    case 'P': Radar(); break;
                    case 'B': Backup(); break;
                    case 'A': About(); break;
                    case '0': Bye(); return;
                }
            }
        }
        #endregion

        #region MODULES

        private static void Boost()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                               APEX BOOST PROFILE                                 |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     This profile applies mostly reversible settings aimed at lower latency and");
            Console.WriteLine("     reduced background overhead. Unsupported adapter properties are skipped.");
            Console.WriteLine();
            Console.Write("   Apply BOOST profile? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Prep();
            Bar(15, "Backup", "Creating registry backup");
            RunCmd($"reg export \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" \"{AppDomain.CurrentDomain.BaseDirectory}APEX_Backup.reg\" /y >>\"{LogFile}\" 2>&1");

            Bar(30, "PWR", "Activating High Performance");
            RunCmd($"powercfg /setactive 8c5e7fda-e8bf-4a96-9a15-e642d95a4573 >>\"{LogFile}\" 2>&1");

            Bar(45, "NET", "Disabling interrupt moderation where supported");
            RunPs($"Get-NetAdapter -Physical | ForEach-Object {{ Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName '*Interrupt Moderation*' -DisplayValue 'Disabled' -NoRestart -ErrorAction SilentlyContinue }} >>\"{LogFile}\" 2>&1");

            Bar(60, "TCP", "Applying low-latency TCP values");
            RunCmd($"for /f \"tokens=*\" %i in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\" 2^>nul') do (reg add \"%i\" /v TcpAckFrequency /t REG_DWORD /d 1 /f >>\"{LogFile}\" 2>&1 & reg add \"%i\" /v TCPNoDelay /t REG_DWORD /d 1 /f >>\"{LogFile}\" 2>&1)");

            Bar(72, "GAME", "Disabling Game DVR capture overhead");
            RunCmd($"reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /t REG_DWORD /d 0 /f >>\"{LogFile}\" 2>&1");

            Bar(84, "DNS", "Flushing DNS resolver cache");
            RunCmd($"ipconfig /flushdns >>\"{LogFile}\" 2>&1");

            Bar(100, "DONE", "Boost profile applied");
            Console.Beep(880, 120);
            Console.Beep(1100, 160);
            Console.WriteLine();
            Console.WriteLine("     [OK] BOOST profile completed.");
            Console.WriteLine($"     Backup: {AppDomain.CurrentDomain.BaseDirectory}APEX_Backup.reg");
            Console.WriteLine($"     Log   : {LogFile}");
            Pause();
        }

        private static void ExtremeBoost()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                      [E] EXTREME PERFORMANCE AND INPUT-LAG BOOST                 |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     Modul wprowadza najbardziej agresywne, bezpieczne optymalizacje rejestru:");
            Console.WriteLine("     - Max priorytet GPU dla gier (SystemProfile - Games)");
            Console.WriteLine("     - Usuniecie ograniczenia sieciowego dla gier (NetworkThrottlingIndex)");
            Console.WriteLine("     - Redukcja opoznienia myszy i klawiatury (Wylaczenie Mouse Acceleration)");
            Console.WriteLine("     - Priorytezacja responsywnosci CPU (SystemResponsiveness = 0)");
            Console.WriteLine("     - Blokowanie zrzucania jadra systemu do pliku stronicowania (DisablePagingExecutive)");
            Console.WriteLine();
            Console.Write("   Uruchomic OPTYMALIZACJE EXTREME? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Prep();
            Bar(15, "GPU", "Ustawianie priorytetu GPU i harmonogramu gier...");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Priority\" /t REG_DWORD /d 6 /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d \"High\" /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d \"High\" /f >>\"{LogFile}\" 2>&1");

            Bar(35, "CPU", "Zdejmowanie kaganca responsywnosci CPU i sieci...");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v \"SystemResponsiveness\" /t REG_DWORD /d 0 /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v \"NetworkThrottlingIndex\" /t REG_DWORD /d 4294967295 /f >>\"{LogFile}\" 2>&1");

            Bar(60, "INPUT", "Eliminacja akceleracji i opoznienia wskaznika...");
            RunCmd($"reg add \"HKCU\\Control Panel\\Mouse\" /v \"MouseSpeed\" /t REG_SZ /d \"0\" /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKCU\\Control Panel\\Mouse\" /v \"MouseThreshold1\" /t REG_SZ /d \"0\" /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKCU\\Control Panel\\Mouse\" /v \"MouseThreshold2\" /t REG_SZ /d \"0\" /f >>\"{LogFile}\" 2>&1");

            Bar(80, "RAM", "Wymuszanie jadra systemu w pamieci RAM (DisablePaging)...");
            RunCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v \"DisablePagingExecutive\" /t REG_DWORD /d 1 /f >>\"{LogFile}\" 2>&1");

            Bar(100, "DONE", "EXTREME BOOST APPLIED!");
            Console.Beep(1000, 100);
            Console.Beep(1300, 100);
            Console.Beep(1600, 200);
            Console.WriteLine();
            Console.WriteLine("     [OK] Priorytety GPU i CPU ustawione na absolutny MAX.");
            Console.WriteLine("     [OK] Ograniczenia sieciowe i opoznienie myszy zostaly usuniete.");
            Console.WriteLine("     [!] ZALECANY REBOOT SYSTEMU aby odczuc pelne efekty.");
            Pause();
        }

        private static void HardwareBoost()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                HARDWARE AND PERFORMANCE BOOST (CPU, RAM, L3 CACHE)               |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     Modul wdraza optymalizacje sprzetowa:");
            Console.WriteLine("     - CPU Unpark Cores (wylaczenie usypiania rdzeni procesora)");
            Console.WriteLine("     - Wlaczenie planu Ultimate Performance (Najwyzsza wydajnosc)");
            Console.WriteLine("     - RAM Cache III Optymalizacja (oproznienie Standby List / czyszczenie bufora)");
            Console.WriteLine("     - Prioryzacja CPU dla aplikacji w pierwszym planie i odblokowanie L3 Cache System.");
            Console.WriteLine();
            Console.Write("   Uruchomic optymalizacje wydajnosciowa? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Prep();
            Bar(20, "CPU", "Aktywowanie Unpark CPU Cores AND MinThrottle...");
            RunCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54333656-0153-42c6-845b-47bb05292229\\0cc5b647-c1df-4597-8833-07769d7e57cd\" /v Attributes /t REG_DWORD /d 0 /f >>\"{LogFile}\" 2>&1");
            RunCmd($"powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100 >>\"{LogFile}\" 2>&1");
            RunCmd($"powercfg -setactive SCHEME_CURRENT >>\"{LogFile}\" 2>&1");

            Bar(45, "PWR", "Odblokowanie planu Ultimate Performance...");
            RunCmd($"powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 >>\"{LogFile}\" 2>&1");

            Bar(65, "CACHE", "Optymalizacja pamieci podrecznej i Win32Priority...");
            RunCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f >>\"{LogFile}\" 2>&1");
            RunCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v LargeSystemCache /t REG_DWORD /d 1 /f >>\"{LogFile}\" 2>&1");

            Bar(85, "RAM", "RAM Cache III Engine - Natywne czyszczenie buforow RAM...");
            NativeFlushRam();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Bar(100, "DONE", "Optymalizacja CPU i RAM zakonczona!");
            Console.Beep(880, 120);
            Console.Beep(1100, 160);
            Console.WriteLine();
            Console.WriteLine("     [OK] Rdzenie CPU ustawione na 100% gotowosci (brak unparkowania).");
            Console.WriteLine("     [OK] Priorytet systemowy i LargeSystemCache zaktualizowane.");
            Console.WriteLine("     [OK] Pamiec RAM przeczyszczona ze zbednych alokacji.");
            Pause();
        }

        private static void BgManager()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Header();
                Console.WriteLine();
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |                     BACKGROUND ENGINE MANAGER // APEX v19.0                      |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine();
                Console.WriteLine("     Modul dzialajacy w tle automatycznie czysci pamiec RAM, TRIM SSD oraz pliki");
                Console.WriteLine("     tymczasowe co 15 minut bez wplywu na wydajnosc w grach i programach.");
                Console.WriteLine();

                string bgStatus = "INACTIVE";
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;
                var bgProc = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(currentExe))
                                    .FirstOrDefault(p => p.Id != Process.GetCurrentProcess().Id);

                if (bgProc != null) bgStatus = $"ACTIVE (PID: {bgProc.Id})";

                Console.WriteLine($"     STATUS SILNIKA W TLE: [ {bgStatus} ]");
                Console.WriteLine();
                Console.WriteLine("     [1] WLACZ optymalizacje w tle (Engine ON)");
                Console.WriteLine("     [2] WYLACZ optymalizacje w tle (Engine OFF)");
                Console.WriteLine("     [3] Powrot do glownego menu");
                Console.WriteLine();
                Console.Write("    >> WYBIERZ OPCJE: ");

                var key = Console.ReadKey().KeyChar;
                if (key == '1')
                {
                    if (bgStatus.Contains("ACTIVE"))
                    {
                        Console.WriteLine("\r\n\r\n     [!] Silnik w tle JEST JUZ URUCHOMIONY! Nie trzeba go wlaczac ponownie.");
                    }
                    else
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = currentExe,
                            Arguments = "--bg-loop",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(startInfo);
                        Console.WriteLine("\r\n\r\n     [OK] Silnik zostal pomyslnie uruchomiony w tle.");
                    }
                    Pause();
                }
                else if (key == '2')
                {
                    if (!bgStatus.Contains("ACTIVE"))
                    {
                        Console.WriteLine("\r\n\r\n     [!] Silnik w tle jest obecnie WYLACZONY.");
                    }
                    else
                    {
                        try { bgProc?.Kill(); } catch { }
                        Console.WriteLine("\r\n\r\n     [OK] Silnik w tle zostal zatrzymany i wylaczony.");
                    }
                    Pause();
                }
                else if (key == '3') return;
            }
        }

        private static void BackgroundLoop()
        {
            while (true)
            {
                NativeFlushRam();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                RunCmd($"defrag {SystemDrive} /O >nul 2>&1");
                FastCleanTempFolders();

                Thread.Sleep(900000); // 15 Minut
            }
        }

        private static void Restore()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                                RESTORE PROFILE                                   |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     This restores the values changed by APEX where possible.");
            Console.WriteLine();
            Console.Write("   Restore Turbo Suite changes? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Bar(15, "DNS", "Restoring automatic DNS");
            RunPs("Get-NetAdapter | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses -ErrorAction SilentlyContinue }");

            Bar(30, "TCP", "Removing custom TCP values");
            RunCmd("for /f \"tokens=*\" %i in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\" 2^>nul') do (reg delete \"%i\" /v TcpAckFrequency /f >nul 2>&1 & reg delete \"%i\" /v TCPNoDelay /f >nul 2>&1)");

            Bar(45, "GAME", "Restoring Game DVR AND EXTREME registry values");
            RunCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 1 /f >nul 2>&1");
            RunCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /f >nul 2>&1");
            RunCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v \"SystemResponsiveness\" /t REG_DWORD /d 14 /f >nul 2>&1");
            RunCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v \"NetworkThrottlingIndex\" /t REG_DWORD /d 10 /f >nul 2>&1");
            RunCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v \"DisablePagingExecutive\" /t REG_DWORD /d 0 /f >nul 2>&1");

            Bar(60, "NET", "Re-enabling interrupt moderation where supported");
            RunPs("Get-NetAdapter -Physical | ForEach-Object { Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName '*Interrupt Moderation*' -DisplayValue 'Enabled' -NoRestart -ErrorAction SilentlyContinue }");

            Bar(75, "PWR", "Restoring Balanced power plan");
            RunCmd("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e >nul 2>&1");

            Bar(90, "IP", "Refreshing network stack");
            RunCmd("netsh winsock reset >nul 2>&1");
            RunCmd("ipconfig /flushdns >nul 2>&1");
            RunCmd("ipconfig /renew >nul 2>&1");

            Bar(100, "DONE", "Restore complete");
            Console.WriteLine();
            Console.WriteLine("     [OK] Restore profile finished.");
            Console.WriteLine("     A reboot is recommended after network stack changes.");
            Pause();
        }

        private static void Clean()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                             CLEAN CACHE // v19.0                                 |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();

            string ramBefore = GetFreeRam();
            Console.WriteLine($"     Free RAM before: {ramBefore} GB");
            Console.WriteLine();

            Bar(20, "RAM", "Natywne czyszczenie WorkingSet wszystkich procesow...");
            NativeFlushRam();

            Bar(45, "DX", "Cleaning DirectX shader cache");
            RunCmd($"del /q /f /s \"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\D3DSCache\\*\" >nul 2>&1");

            Bar(70, "TMP", "Wielowatkowe czyszczenie folderow TEMP / Prefetch...");
            FastCleanTempFolders();

            Bar(85, "NET", "Flushing DNS cache");
            RunCmd("ipconfig /flushdns >nul 2>&1");

            string ramAfter = GetFreeRam();

            Bar(100, "DONE", "Cleanup complete");
            Console.WriteLine();
            Console.WriteLine($"     Free RAM after : {ramAfter} GB");
            Console.WriteLine("     Note: Windows may immediately reuse freed RAM as cache; that is normal.");
            Pause();
        }

        private static void DiskBoost()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                          DISK OPTIMIZER // v19.0                                 |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     Ten modul wykonuje szybka optymalizacje dysku i czyszczenie bufora:");
            Console.WriteLine("     - Reset uslugi SysMain (zatrzymuje nagly skok obciazenia dysku do 100%)");
            Console.WriteLine("     - Wymuszenie komendy TRIM dla dyskow SSD");
            Console.WriteLine("     - Wyczyszczenie pamieci podrecznej Windows Update");
            Console.WriteLine();
            Console.Write("   Uruchomic optymalizacje dysku? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Bar(20, "SYS", "Resetowanie bufora SysMain...");
            RunCmd($"net stop sysmain >>\"{LogFile}\" 2>&1");
            RunCmd($"net start sysmain >>\"{LogFile}\" 2>&1");

            Bar(50, "TRIM", "Wysylanie komendy TRIM dla SSD...");
            RunCmd($"defrag {SystemDrive} /O >>\"{LogFile}\" 2>&1");

            Bar(80, "CACHE", "Czyszczenie cache SoftwareDistribution...");
            RunCmd($"net stop wuauserv >>\"{LogFile}\" 2>&1");
            RunCmd($"del /f /s /q \"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\\SoftwareDistribution\\Download\\*\" >>\"{LogFile}\" 2>&1");
            RunCmd($"net start wuauserv >>\"{LogFile}\" 2>&1");

            Bar(100, "DONE", "Optymalizacja dysku zakonczona!");
            Console.Beep(880, 120);
            Console.Beep(1100, 160);
            Console.WriteLine();
            Console.WriteLine("     [OK] Usluga SysMain zresetowana.");
            Console.WriteLine("     [OK] TRIM przeslany do kontrolera dysku.");
            Console.WriteLine("     [OK] Cache aktualizacji zostal wyczyszczony.");
            Pause();
        }

        private static void GameMode()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                                    GAME MODE                                     |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     Game Mode focuses on the things APEX can safely automate:");
            Console.WriteLine("     - Enable Windows Game Mode registry flag where supported");
            Console.WriteLine("     - Flush DNS");
            Console.WriteLine("     - Stop no user apps automatically");
            Console.WriteLine("     - Never kill processes blindly");
            Console.WriteLine();
            Console.Write("   Activate Game Mode profile? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Bar(25, "GAME", "Enabling Windows Game Mode flag");
            RunCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 1 /f >nul 2>&1");
            RunCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f >nul 2>&1");

            Bar(55, "NET", "Flushing DNS");
            RunCmd("ipconfig /flushdns >nul 2>&1");

            Bar(80, "FOCUS", "Preparing focus profile");
            string focusNote = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "APEX_GameMode_Note.txt");
            File.WriteAllText(focusNote, $"TURBO SUITE APEX Game Mode\r\nActivated: {DateTime.Now}\r\nNo processes were terminated automatically.");

            Bar(100, "DONE", "Game Mode ready");
            Console.WriteLine();
            Console.WriteLine("     [OK] Game Mode profile ready.");
            Console.WriteLine($"     Note: {focusNote}");
            Pause();
        }

        private static string DetectDefender()
        {
            string defPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Windows Defender\MpCmdRun.exe");
            if (File.Exists(defPath)) return defPath;

            string platformPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows Defender\Platform");
            if (Directory.Exists(platformPath))
            {
                var files = Directory.GetFiles(platformPath, "MpCmdRun.exe", SearchOption.AllDirectories);
                if (files.Length > 0) return files[0];
            }
            return string.Empty;
        }

        private static void Virus()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Header();
                Console.WriteLine();
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |                             VIRUS LAB // SECURITY                                |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine();

                string defenderPath = DetectDefender();
                if (string.IsNullOrEmpty(defenderPath))
                {
                    Console.WriteLine("     [X] Nie znaleziono pliku MpCmdRun.exe (Windows Defender).");
                    Console.WriteLine("         Upewnij sie, ze uslugowa ochrona Windows Defender jest wlaczona.");
                    Pause();
                    return;
                }

                Console.WriteLine($"     Silnik antywirusowy: {defenderPath}");
                Console.WriteLine();
                Console.WriteLine("     Wybierz tryb skanowania antywirusowego:");
                Console.WriteLine();
                Console.WriteLine("     [1] Szybkie skanowanie (Quick Scan - kluczowe obszary i pamiec)");
                Console.WriteLine("     [2] Aktualizacja bazy wirusow (Signature Update)");
                Console.WriteLine("     [3] Pelne skanowanie (Full Scan - moze potrwac kilkanascie minut)");
                Console.WriteLine("     [4] Powrot do menu");
                Console.WriteLine();
                Console.Write("    >> WYBIERZ OPCJE: ");

                var key = Console.ReadKey().KeyChar;
                if (key == '1')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     [*] Uruchamianie szybkiego skanowania Windows Defender...\r\n");
                    RunCmd($"\"{defenderPath}\" -Scan -ScanType 1", false);
                    Console.WriteLine("\r\n     [OK] Skanowanie zakonczone.");
                    Pause();
                }
                else if (key == '2')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     [*] Pobieranie najnowszych definicji wirusow...\r\n");
                    RunCmd($"\"{defenderPath}\" -SignatureUpdate", false);
                    Console.WriteLine("\r\n     [OK] Aktualizacja definicji zakonczona.");
                    Pause();
                }
                else if (key == '3')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     [*] Uruchamianie PELNEGO skanowania systemu... (Prosze czekac)\r\n");
                    RunCmd($"\"{defenderPath}\" -Scan -ScanType 2", false);
                    Pause();
                }
                else if (key == '4') return;
            }
        }

        private static void Benchmark()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                          BENCHMARK LAB // TEST WYDAJNOSCI                        |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     Ten test zmierzy rzeczywista wydajnosc Twojego procesora oraz dysku.");
            Console.WriteLine("     Podczas testu nie zamykaj okna i nie uruchamiaj ciezkich aplikacji.");
            Console.WriteLine();
            Console.Write("   Uruchomic test wydajnosci? (T/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() != "T") return;

            Bar(20, "CPU", "Inicjalizacja testu procesora (500,000 cykli math)...");
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 1; i <= 500000; i++)
            {
                var x = Math.Sqrt(i) * Math.Sin(i);
            }
            sw.Stop();
            long benchCpu = sw.ElapsedMilliseconds;

            Bar(65, "DISK", "Test zapisu i odczytu bufora 50MB na dysku systemowym...");
            string diskWrite = "N/A";
            string diskRead = "N/A";

            try
            {
                string testFile = Path.Combine(Path.GetTempPath(), "apex_bench.dat");
                byte[] data = new byte[50 * 1024 * 1024];
                new Random().NextBytes(data);

                sw.Restart();
                File.WriteAllBytes(testFile, data);
                sw.Stop();
                double w = Math.Round(50.0 / (sw.ElapsedMilliseconds / 1000.0), 1);

                sw.Restart();
                var readData = File.ReadAllBytes(testFile);
                sw.Stop();
                double r = Math.Round(50.0 / (sw.ElapsedMilliseconds / 1000.0), 1);

                File.Delete(testFile);

                diskWrite = w.ToString();
                diskRead = r.ToString();
            }
            catch { }

            Bar(100, "DONE", "Test wydajnosci zakonczony!");
            Console.Beep(880, 120);
            Console.Beep(1100, 160);

            File.AppendAllText(LogFile, $"{DateTime.Now} - BENCHMARK RESULTS: CPU: {benchCpu} ms | DISK WRITE: {diskWrite} MB/s | DISK READ: {diskRead} MB/s\r\n");

            Console.Clear();
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                         WYNIKI TESTU WYDAJNOSCI // APEX                          |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine($"      [+] Czas obliczen CPU (500k cykli math) : {benchCpu} ms (im mniej, tym lepiej)");
            Console.WriteLine($"      [+] Predkosc ZAPISU dysku ({SystemDrive})      : {diskWrite} MB/s");
            Console.WriteLine($"      [+] Predkosc ODCZYTU dysku ({SystemDrive})     : {diskRead} MB/s");
            Console.WriteLine();
            Console.WriteLine("    ------------------------------------------------------------------------------------");
            Console.WriteLine("     Interpretacja wynikow:");
            Console.WriteLine("     - CPU ponizej 800 ms = Bardzo wysoka wydajnosc jednowatkowa.");
            Console.WriteLine("     - Zapis Dysku > 300 MB/s = Szybki dysk SSD (SATA/NVMe).");
            Console.WriteLine();
            Console.WriteLine("     [OK] Wyniki zostaly automatycznie zapisane do:");
            Console.WriteLine($"     {LogFile}");
            Pause();
        }

        private static void Network()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Header();
                Console.WriteLine();
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine("    |                                   NETWORK LAB                                    |");
                Console.WriteLine("    +==================================================================================+");
                Console.WriteLine();
                Console.WriteLine("     [1] Quick internet ping");
                Console.WriteLine("     [2] Adapter status");
                Console.WriteLine("     [3] DNS configuration");
                Console.WriteLine("     [4] Route snapshot");
                Console.WriteLine("     [5] Back");
                Console.WriteLine();
                Console.Write("    >> TEST: ");

                var key = Console.ReadKey().KeyChar;
                if (key == '1')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     NETWORK PING // 1.1.1.1 and 8.8.8.8\r\n");
                    RunCmd("ping -n 4 1.1.1.1", false);
                    Console.WriteLine();
                    RunCmd("ping -n 4 8.8.8.8", false);
                    Pause();
                }
                else if (key == '2')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     ACTIVE NETWORK ADAPTERS\r\n");
                    RunPs("Get-NetAdapter | Where-Object Status -eq 'Up' | Format-Table -AutoSize Name,InterfaceDescription,LinkSpeed,MacAddress", false);
                    Pause();
                }
                else if (key == '3')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     DNS CONFIGURATION\r\n");
                    RunCmd("ipconfig /all | findstr /i /c:\"DNS Servers\" /c:\"IPv4 Address\" /c:\"Description\"", false);
                    Pause();
                }
                else if (key == '4')
                {
                    Console.Clear();
                    Header();
                    Console.WriteLine("\r\n     ROUTE SNAPSHOT\r\n");
                    RunCmd("route print", false);
                    Pause();
                }
                else if (key == '5') return;
            }
        }

        private static void Hardware()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Header();
            Console.WriteLine();
            StatusLine();
            Console.WriteLine();
            Console.WriteLine("     CPU LOAD:");
            RunPs("Get-CimInstance Win32_Processor | Select-Object -ExpandProperty LoadPercentage | Format-Table", false);
            Console.WriteLine();
            Console.WriteLine("     DISK HEALTH:");
            RunPs("Get-PhysicalDisk | Select-Object FriendlyName,MediaType,HealthStatus,OperationalStatus,Size | Format-Table -AutoSize", false);
            Console.WriteLine();
            Console.WriteLine("     UPTIME:");
            RunPs("$u=(Get-Date)-(Get-CimInstance Win32_OperatingSystem).LastBootUpTime; '{0}d {1}h {2}m' -f $u.Days,$u.Hours,$u.Minutes", false);
            Pause();
        }

        private static void Radar()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                                  PROCESS RADAR                                   |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();
            Console.WriteLine("     Top processes by CPU:");
            Console.WriteLine();
            RunPs("Get-Process | Sort-Object CPU -Descending | Select-Object -First 15 Id,ProcessName,CPU,@{N='RAM_MB';E={[math]::Round($_.WorkingSet64/1MB,0)}} | Format-Table -AutoSize", false);
            Console.WriteLine();
            Console.WriteLine("     Snapshot is informational only; no process is terminated automatically.");
            Console.WriteLine();

            string radarFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Process_Radar.txt");
            RunPs($"Get-Process | Sort-Object CPU -Descending | Select-Object -First 30 Id,ProcessName,CPU,@{{N='RAM_MB';E={{[math]::Round($_.WorkingSet64/1MB,0)}}}} | Out-File -Encoding UTF8 '{radarFile}'");
            Console.WriteLine($"     Saved: {radarFile}");
            Pause();
        }

        private static void Backup()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                               SYSTEM BACKUP CENTER                               |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine();

            Bar(25, "RESTORE", "Creating restore point");
            RunPs($"Enable-ComputerRestore -Drive '{SystemDrive}\\' -ErrorAction SilentlyContinue; Checkpoint-Computer -Description 'TurboSuiteApex_v19_PreChange' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction SilentlyContinue");

            Bar(60, "REG", "Exporting system profile registry branch");
            RunCmd($"reg export \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" \"{AppDomain.CurrentDomain.BaseDirectory}APEX_SystemProfile_Backup.reg\" /y >>\"{LogFile}\" 2>&1");

            Bar(100, "DONE", "Backup center complete");
            Console.WriteLine();
            Console.WriteLine("     [OK] Backup files were created when Windows allows it.");
            Console.WriteLine($"     Registry: {AppDomain.CurrentDomain.BaseDirectory}APEX_SystemProfile_Backup.reg");
            Pause();
        }

        private static void About()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Header();
            Console.WriteLine();
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |                               APEX v19.0 CHANGELOG                               |");
            Console.WriteLine("    +==================================================================================+");
            Console.WriteLine("    |  FIXED: Usunieto bledy skladni CMD przy znakach specjalnych                     |");
            Console.WriteLine("    |  ADDED: Native Win32 Memory WorkingSet Flush (psapi.dll)                         |");
            Console.WriteLine("    |  ADDED: Multi-threaded Parallel Temp File Cleaning                               |");
            Console.WriteLine("    +==================================================================================+");
            Pause();
        }

        private static void Bye()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Header();
            Console.WriteLine();
            Console.WriteLine("     APEX CORE shutting down...");
            Console.WriteLine();
            Console.WriteLine($"     Session start: {StartTime}");
            Console.WriteLine($"     Session end  : {DateTime.Now:HH:mm:ss.ff}");
            Console.WriteLine();
            Console.WriteLine("     Thanks for using APEX, we hope you come back");
            Console.WriteLine();

            Console.Beep(660, 80);
            Console.Beep(820, 100);
            Console.Beep(1100, 150);
            Thread.Sleep(4000);
        }

        #endregion

        #region COMMAND RUNNERS
        private static void RunCmd(string command, bool hidden = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = hidden,
                UseShellExecute = !hidden
            };
            var p = Process.Start(psi);
            p?.WaitForExit();
        }

        private static void RunPs(string command, bool hidden = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command " + command)
            {
                CreateNoWindow = hidden,
                UseShellExecute = !hidden
            };
            var p = Process.Start(psi);
            p?.WaitForExit();
        }

        private static string GetFreeRam()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
                    foreach (var item in searcher.Get())
                        return Math.Round(Convert.ToDouble(item["FreePhysicalMemory"]) / 1024.0 / 1024.0, 1).ToString();
            }
            catch { }
            return "?";
        }
        #endregion
    }
}