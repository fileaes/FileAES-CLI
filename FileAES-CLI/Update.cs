using System;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.IO.Compression;

namespace FileAES_CLI
{
    public class Update
    {
        private UpdateStatus _appUpdateStatus;
        private string _latestVersion, _branch;
        private bool _updateThreadRunning = false;
        private bool _isUpdateAvailable = false;

        public Update(string branch)
        {
            _branch = branch;
            //CheckForUpdate();
        }

        public enum UpdateStatus
        {
            ServerError,
            AppOutdated,
            AppLatest,
            AppNewer
        };

        public void SetBranch(string branch)
        {
            _branch = branch;
        }

        public bool IsUpdate()
        {
            return _isUpdateAvailable;
        }

        public string GetLatestVersion()
        {
            try
            {string latestUrl = String.Format("https://api.mullak99.co.uk/FAES/IsUpdate.php?app=faes_cli&branch={0}&showver=true&version={1}", _branch, ConvertVersionToNonFormatted(Program.GetVersion()));

                WebClient client = new WebClient();
                byte[] html = client.DownloadData(latestUrl);
                UTF8Encoding utf = new UTF8Encoding();
                if (String.IsNullOrEmpty(utf.GetString(html)) || utf.GetString(html) == "null")
                    return "v0.0.0";
                else
                    return utf.GetString(html);
            }
            catch (Exception)
            {
                return "v0.0.0";
            }
        }

        public bool DoesVersionExist(string version, string branch)
        {
            try
            {
                string latestUrl = String.Format("https://api.mullak99.co.uk/FAES/DoesVersionExist.php?app=faes_cli&branch={0}&version={1}", branch, version);

                WebClient client = new WebClient();
                byte[] html = client.DownloadData(latestUrl);
                UTF8Encoding utf = new UTF8Encoding();
                string result = utf.GetString(html);
                if (String.IsNullOrEmpty(result) || result == "null")
                    return false;
                else if (result == "VersionExists")
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void CheckForUpdate(bool silent = false)
        {
            Thread checkUpdateThread = new Thread(() =>
            {
                if (!_updateThreadRunning)
                {
                    Thread threaddedUpdateCheck = new Thread(() => CheckForUpdateNonThreadded(silent));
                    threaddedUpdateCheck.Start();
                }
            });
            checkUpdateThread.Start();
        }

        public void CheckForUpdateNonThreadded(bool silent = false)
        {
            _updateThreadRunning = true;

            string updateVersion;

            if (!silent) Logging.Log(String.Format("Checking for update..."), Severity.DEBUG);
            UpdateStatus updateInfo = GetUpdateStatus(out updateVersion);

            if (!silent)
            {
                if (updateVersion != "v0.0.0")
                    Logging.Log(String.Format("Latest FAES_CLI version: {0}", updateVersion), Severity.DEBUG);
                else
                    Logging.Log(String.Format("Update check failed!"), Severity.WARN);
            }
                

            _appUpdateStatus = updateInfo;
            _latestVersion = updateVersion;
            _updateThreadRunning = false;

            if (_appUpdateStatus == UpdateStatus.AppOutdated)
            {
                _isUpdateAvailable = true;
                if (!silent) Logging.Log(String.Format("[UPDATE] FileAES-CLI {0} is available! You are on {1}. Run the program with '--update' to update.", _latestVersion, Program.GetVersion()));
            }
            else _isUpdateAvailable = false;
        }

        private UpdateStatus GetUpdateStatus(out string updateVersion)
        {
            try
            {
                string latestVer = GetLatestVersion();
                string currentVer = ConvertVersionToNonFormatted(Program.GetVersion());
                updateVersion = latestVer;

                if (latestVer == currentVer)
                {
                    return UpdateStatus.AppLatest;
                }
                else if (latestVer != "v0.0.0" && CheckServerConnection())
                {
                    string compareVersions = String.Format("https://api.mullak99.co.uk/FAES/CompareVersions.php?app=faes_cli&branch={0}&version1={1}&version2={2}", "dev", currentVer, latestVer);

                    WebClient client = new WebClient();
                    byte[] html = client.DownloadData(compareVersions);
                    UTF8Encoding utf = new UTF8Encoding();
                    string result = utf.GetString(html).ToLower();

                    if (String.IsNullOrEmpty(result) || result == "null")
                        return UpdateStatus.ServerError;
                    else if (result.Contains("not exist in the database!") || result == "version1 is newer than version2")
                        return UpdateStatus.AppNewer;
                    else if (result == "version1 is older than version2")
                        return UpdateStatus.AppOutdated;
                    else if (result == "version1 is equal to version2")
                        return UpdateStatus.AppLatest;
                    else
                        return UpdateStatus.ServerError;
                }
                else
                {
                    return UpdateStatus.ServerError;
                }
            }
            catch
            {
                updateVersion = "v0.0.0";
                return UpdateStatus.ServerError;
            }
        }

        public static bool CheckServerConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("https://api.mullak99.co.uk/"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateSelf(bool doCleanUpdate = false, string version = "latest")
        {
            string installDir = Directory.GetCurrentDirectory();

            if (CheckServerConnection())
                try
                {
                    if (File.Exists(Path.Combine(installDir, "FAES-Updater.exe")))
                        File.Delete(Path.Combine(installDir, "FAES-Updater.exe"));
                    if (File.Exists(Path.Combine(installDir, "updater.pack")))
                        File.Delete(Path.Combine(installDir, "updater.pack"));

                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(new Uri(String.Format("https://api.mullak99.co.uk/FAES/GetDownload.php?app=faes_updater&ver=latest&branch={0}&redirect=true", _branch)), Path.Combine(installDir, "updater.pack"));
                    ZipFile.ExtractToDirectory(Path.Combine(installDir, "updater.pack"), installDir);
                    File.Delete(Path.Combine(installDir, "updater.pack"));
                    Thread.Sleep(100);

                    string args = "";
                    if (doCleanUpdate) args += "--pure ";
                    //if (Program.programManager.GetFullInstall()) args += "--full ";
                    if (Program.IsVerbose()) args += "--verbose ";
                    else args += "--silent ";
                    args += "--branch " + _branch + " ";
                    args += "--tool faes_cli ";
                    args += "--version " + version + " ";
                    args += "--delay 10 ";
                    //args += "--run ";
                    Process.Start(Path.Combine(installDir, "FAES-Updater.exe"), args);

                    Environment.Exit(0);
                }
                catch (UnauthorizedAccessException)
                {
                    RunAsAdmin();
                }
                catch (Exception e)
                {
                    Logging.Log(e.ToString(), Severity.ERROR);

                    if (File.Exists(Path.Combine(installDir, "FAES-Updater.exe")))
                        File.Delete(Path.Combine(installDir, "FAES-Updater.exe"));
                    if (File.Exists(Path.Combine(installDir, "updater.pack")))
                        File.Delete(Path.Combine(installDir, "updater.pack"));
                }
            else
            {
                if (File.Exists(Path.Combine(installDir, "FAES-Updater.exe")))
                    File.Delete(Path.Combine(installDir, "FAES-Updater.exe"));
                if (File.Exists(Path.Combine(installDir, "updater.pack")))
                    File.Delete(Path.Combine(installDir, "updater.pack"));
            }
        }

        private string ConvertVersionToFormatted(string nonFormattedVersion)
        {
            string[] versionSplit = nonFormattedVersion.Replace("_", " ").Split('-');
            string formattedVersion;

            if (versionSplit.Length > 0)
            {
                formattedVersion = versionSplit[0];

                if (versionSplit.Length > 1)
                {
                    if (versionSplit[1].ToUpper()[0] == 'B')
                    {
                        string betaTag = versionSplit[1].ToUpper().Replace("BETA", "").Replace("B", "");
                        formattedVersion += String.Format(" (BETA {0}", betaTag.Replace(" ", ""));
                    }
                    else if (versionSplit[1].ToUpper()[0] == 'D')
                    {
                        string devTag = versionSplit[1].ToUpper().Replace("DEV", "").Replace("D", "");
                        formattedVersion += String.Format(" (DEV{0}", devTag);
                    }
                    if (versionSplit.Length > 2)
                    {
                        for (int i = 2; i < versionSplit.Length; i++)
                        {
                            formattedVersion += "-";
                            formattedVersion += versionSplit[i];
                        }
                    }
                    formattedVersion += ")";
                }
            }
            else formattedVersion = nonFormattedVersion;

            Logging.Log(String.Format("ToFormatted: Converted '{0}' to '{1}'.", nonFormattedVersion, formattedVersion), Severity.DEBUG);
            return formattedVersion;
        }

        private string ConvertVersionToNonFormatted(string formattedVersion)
        {

            string[] versionSplit = formattedVersion.Replace("(", "").Replace(")", "").Split(' ');
            string nonFormattedVersion;

            if (versionSplit.Length > 0)
            {
                nonFormattedVersion = versionSplit[0];

                if (versionSplit.Length > 1)
                {
                    if (versionSplit[1].ToUpper()[0] == 'B')
                    {
                        nonFormattedVersion += "-B";
                    }
                    else if (versionSplit[1].ToUpper()[0] == 'D')
                    {
                        nonFormattedVersion += "-DEV";
                    }
                    nonFormattedVersion += versionSplit[1].ToUpper().Replace("BETA", "").Replace("B", "").Replace("DEV", "").Replace("D", "");

                    if (versionSplit.Length > 2)
                    {
                        for (int i = 2; i < versionSplit.Length; i++)
                        {
                            nonFormattedVersion += "-";
                            nonFormattedVersion += versionSplit[i].ToUpper();
                        }
                    }
                }
            }
            else nonFormattedVersion = formattedVersion;

            if (nonFormattedVersion.Contains("-B-"))
                nonFormattedVersion = nonFormattedVersion.Replace("-B-", "-B");
            else if (nonFormattedVersion.Contains("-DEV-"))
                nonFormattedVersion = nonFormattedVersion.Replace("-DEV-", "-DEV");
            nonFormattedVersion = nonFormattedVersion.TrimEnd('-');

            Logging.Log(String.Format("ToNonFormatted: Converted '{0}' to '{1}'.", formattedVersion, nonFormattedVersion), Severity.DEBUG);
            return nonFormattedVersion;
        }

        internal static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RunAsAdmin()
        {
            if (!IsRunAsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                }
                catch
                {
                    return;
                }
                Environment.Exit(0);
            }
        }
    }
}
