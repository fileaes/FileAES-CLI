﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using FAES;
using FAES.AES;
using FAES.Packaging;

namespace FileAES_CLI
{
    class Program
    {
        private static Update _update;

        private static bool _verbose = false;
        private static bool _purgeTemp = false;
        private static bool _help = false;
        private static bool _getHint = false;
        private static bool _getEncryptTimestamp = false;
        private static bool _getEncryptCompression = false;
        private static bool _getFaesVersion = false;
        private static bool _getVersion = false;
        private static bool _showProgress = false;
        private static bool _overwriteDuplicates = false;
        private static bool _deleteOriginalFile = true;
        private static bool _showAllMetadata = false;
        private static bool _showAllMetadataString = false;
        private static string _directory = null;
        private static string _passwordHint = null;
        private static string _password;
        private static string _compressionMethod = null;
        private static int _compressionLevel = 7;
        private static ushort _progressSleep = 5000;
        private static uint _csBuffer = FileAES_Utilities.GetCryptoStreamBuffer();
        private static List<string> _strippedArgs = new List<string>();

        private static bool _checkForUpdate = true;
        private static bool _doUpdate = false;
        private static bool _forceUpdate = false;
        private static bool _showLatestVer = false;
        private static bool _useLocalEncrypt = true;
        private static string _showLatestBranch = "unknown";
        private static string _updateBranch = "unknown";
        private static string _updateToVer = "latest";

        private const bool _isDevBuild = true;
        private const bool _isBetaBuild = false;
        private const string _devBuildTag = "RC_1";

        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            for (int i = 0; i < args.Length; i++)
            {
                string strippedArg = args[i].ToLower();

                if (Directory.Exists(args[i])) _directory = args[i];
                else if (File.Exists(args[i])) _directory = args[i];

                strippedArg = strippedArg.TrimStart('-', '/', '\\');

                if (strippedArg == "verbose" || strippedArg == "v")
                {
                    _verbose = true;
                    FileAES_Utilities.SetVerboseLogging(_verbose);
                }
                else if (string.IsNullOrEmpty(_password) && (strippedArg == "password" || strippedArg == "p") && !string.IsNullOrEmpty(args[i + 1])) _password = args[i + 1];
                else if (strippedArg == "purgetemp" || strippedArg == "deletetemp") _purgeTemp = true;
                else if (strippedArg == "help") _help = true;
                else if (string.IsNullOrEmpty(_passwordHint) && (strippedArg == "hint" || strippedArg == "passwordhint" || strippedArg == "h") && !string.IsNullOrEmpty(args[i + 1])) _passwordHint = args[i + 1];
                else if (strippedArg == "gethint" || strippedArg == "getpasswordhint") _getHint = true;
                else if (strippedArg == "gettimestamp" || strippedArg == "timestamp" || strippedArg == "encryptiondate") _getEncryptTimestamp = true;
                else if (strippedArg == "getcompression" || strippedArg == "getcompressionmethod") _getEncryptCompression = true;
                else if (strippedArg == "showprogress" || strippedArg == "progress" || strippedArg == "prog")
                {
                    if (!string.IsNullOrEmpty(args[i + 1]) && UInt16.TryParse(args[i + 1], out _progressSleep)) { }
                    _showProgress = true;
                }
                else if (strippedArg == "faesversion" || strippedArg == "faes" || strippedArg == "faesver") _getFaesVersion = true;
                else if (strippedArg == "faescliversion" || strippedArg == "faescliver" || strippedArg == "faescli" || strippedArg == "cliver" || strippedArg == "ver" || strippedArg == "version")
                {
                    _getVersion = true;
                    _getFaesVersion = true;
                }
                else if (string.IsNullOrEmpty(_compressionMethod) && (strippedArg == "compression" || strippedArg == "compressionmethod" || strippedArg == "c") && !string.IsNullOrEmpty(args[i + 1])) _compressionMethod = args[i + 1].ToUpper();
                else if ((strippedArg == "level" || strippedArg == "compressionlevel" || strippedArg == "l") && !string.IsNullOrEmpty(args[i + 1])) Int32.TryParse(args[i + 1], out _compressionLevel);
                else if (strippedArg == "buffer" || strippedArg == "cryptostreambuffer" || strippedArg == "csbuffer" && !string.IsNullOrEmpty(args[i + 1])) UInt32.TryParse(args[i + 1], out _csBuffer);
                else if (strippedArg == "overwrite" || strippedArg == "overwriteduplicates" || strippedArg == "o") _overwriteDuplicates = true;
                else if (strippedArg == "preserveoriginal" || strippedArg == "original" || strippedArg == "po") _deleteOriginalFile = false;
                else if (strippedArg == "showallmetadata" || strippedArg == "showmetadata" || strippedArg == "metadata") _showAllMetadata = true;
                else if (strippedArg == "showallutf8metadata" || strippedArg == "showutf8metadata" || strippedArg == "utf8metadata") _showAllMetadataString = true;
                else if (strippedArg == "" || strippedArg == "showutf8metadata" || strippedArg == "utf8metadata") _showAllMetadataString = true;
                else if (strippedArg == "tempencrypt" || strippedArg == "temp") _useLocalEncrypt = false;
                else if (strippedArg == "localencrypt" || strippedArg == "local") _useLocalEncrypt = true;
                else if (strippedArg == "showlatest" || strippedArg == "getlatest" || strippedArg == "getlatestversion" || strippedArg == "latestversion" || strippedArg == "latest")
                {
                    _showLatestVer = true;

                    if (args.Length > (i + 1) && !string.IsNullOrEmpty(args[i + 1]))
                    {
                        string arg = args[i + 1];

                        if (arg.ToLower() == "dev" || arg.ToLower() == "developer" || arg.ToLower() == "d")
                        {
                            _showLatestBranch = "dev";
                        }
                        else if (arg.ToLower() == "beta" || arg.ToLower() == "b")
                        {
                            _showLatestBranch = "beta";
                        }
                        else if (arg.ToLower() == "stable" || arg.ToLower() == "s" || arg.ToLower() == "release" || arg.ToLower() == "r")
                        {
                            _showLatestBranch = "stable";
                        }
                    }
                }
                else if (strippedArg == "noupdatecheck" || strippedArg == "nocheck" || strippedArg == "offline" || strippedArg == "noupdate") _checkForUpdate = false;
                else if (strippedArg == "update" || strippedArg == "u")
                {
                    string branch = "";
                    string version = "";

                    if (args.Length > (i + 1) && !string.IsNullOrEmpty(args[i + 1]))
                    {
                        string arg = args[i + 1];

                        {
                            BranchFinder:
                            switch (arg.ToLower())
                            {
                                case "dev":
                                case "developer":
                                case "d":
                                    branch = "dev";
                                    break;
                                case "beta":
                                case "b":
                                    branch = "beta";
                                    break;
                                case "stable":
                                case "s":
                                case "release":
                                case "r":
                                    branch = "stable";
                                    break;
                                default:
                                {
                                    if (args.Length > (i + 2) && !string.IsNullOrEmpty(args[i + 2]) && string.IsNullOrWhiteSpace(version))
                                    {
                                        version = args[i + 1];
                                        arg = args[i + 2];
                                        goto BranchFinder;
                                    }
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(branch) && string.IsNullOrEmpty(version) && args.Length > (i + 1) && !string.IsNullOrEmpty(args[i + 1]))
                        {
                            version = args[i + 1];
                        }
                    }

                    if (args.Length > (i + 1) && string.IsNullOrEmpty(args[i + 1]) && args[i + 1].ToLower() == "force") _forceUpdate = true;
                    else if (args.Length > (i + 2) && string.IsNullOrEmpty(args[i + 2]) && args[i + 2].ToLower() == "force") _forceUpdate = true;
                    else if (args.Length > (i + 3) && string.IsNullOrEmpty(args[i + 3]) && args[i + 3].ToLower() == "force") _forceUpdate = true;

                    if (!string.IsNullOrWhiteSpace(branch)) _updateBranch = branch;
                    if (!string.IsNullOrWhiteSpace(version)) _updateToVer = version;

                    _doUpdate = true;
                }

                _strippedArgs.Add(strippedArg);
            }

            if (_checkForUpdate)
            {
                if (_updateBranch == "unknown")
                {
                    if (IsDevBuild())
                        _updateBranch = "dev";
                    else if (IsBetaBuild())
                        _updateBranch = "beta";
                    else
                        _updateBranch = "stable";
                }

                _update = new Update("dev");
            }

            if (_showLatestBranch == "unknown")
            {
                _showLatestBranch = _updateBranch;
            }

            if (_help)
            {
                Logging.Log("A FAES-based tool for encrypting and decrypting files using the command-line.\n\nPossible Launch Parameters:\n'--verbose' or '-v': Show more debugging information in the console (WIP)." +
                    "\n'--purgeTemp' or '-p': Purge the FileAES Temp folder to resolve possible errors.\n'--password <password>' or '-p <password>': Sets the password that will be used to encrypt/decrypt the file/folder." +
                    "\n'--hint <Password Hint>' or '-h <Password Hint>': Sets a password hint.\n'--compression <ZIP/TAR/LZMA/LEGACYZIP>' or '-c <ZIP/TAR/LZMA/LEGACYZIP>': Sets the compression method that will be used to encrypt the file/folder." +
                    "\n'--level <0-9>' or '-l <0-9>': Sets the compression level that will be used to encrypt the file/folder. (Only works for with the ZIP compression method)" +
                    ".\n'--getHint': Gets the password hint for the encrypted file.\n'--getTimestamp': Gets the encryption timestamp of the encrypted file." +
                    "\n'--getCompression': Gets the compression method of the encrypted file.\n'--ver': Gets the current version of FileAES-CLI and FAES being used." +
                    "\n'--FAES': Gets the current version of FAES being used.\n'--overwrite' or '-o': Overwrites any duplicate files found within the FAES process." +
                    "\n'--original' or '-po': Preserves the original file used in the encrypt/decrypt process." +
                    "\n'--progress [<Polling Rate (ms)>]': Outputs the current encryption/decryption progress to the console after desired time (Leaving polling rate blank defaults to 5000ms)." +
                    "\n'--buffer <Size (bytes)>': Sets the size of the FAES CryptoStream buffer.\n'--metadata': Shows the raw metadeta (Hex) of the encrypted file." +
                    "\n'--utf8metadata': Shows the raw metadeta (Decoded UTF8) of the encrypted file.\n'--offline': Disables auto-update checking." +
                    "\n'--latest [<branch>]': Outputs the latest version of FileAES-CLI on that branch (Leaving branch blank defaults to current builds branch)." +
                    "\n'--update [<version>] [<branch>] [force]': Updates FileAES-CLI as specified. Using no arguments updates to the latest version on the current branch.\n\n" +
                    "\n'--localEncrypt' or '-local': Encrypt files within the same folder as the source file (Default behaviour).\n\n" +
                    "\n'--tempEncrypt' or '-temp': Encrypt files within the OS' Temp folder (Old behaviour).\n\n" +
                    "File/Folder names can be entered as a launch parameter to select what to encrypt/decrypt (also allows for dragging/dropping a file/folder on the .exe).\n\n" +
                    "Example: 'FileAES-CLI.exe File.txt -p password123'");
                return;
            }

            if (_doUpdate)
            {
                if (_update != null && _checkForUpdate)
                {
                    _update.CheckForUpdateNonThreadded(true);

                    bool doUpdate = false;

                    if ((!_forceUpdate && _update.IsUpdate()) || _forceUpdate || _updateToVer != "latest")
                    {
                        if (_updateToVer == "latest")
                        {
                            string latestVer = _update.GetLatestVersion();
                            Logging.Log($"Installing FileAES-CLI {latestVer}...");
                            doUpdate = true;
                        }
                        else if (_update.DoesVersionExist(_updateToVer, "dev"))
                        {
                            Logging.Log($"Installing FileAES-CLI {_updateToVer}...");
                            doUpdate = true;
                        }
                        else
                        {
                            Logging.Log($"Could not find FileAES-CLI {_updateToVer}!", Severity.WARN);
                        }

                        if (doUpdate)
                        {
                            _update.UpdateSelf(true, _updateToVer);
                            return;
                        }
                    }
                }
            }
            else if (_checkForUpdate)
            {
                _update.CheckForUpdate();
            }

            if (_purgeTemp)
            {
                FileAES_Utilities.PurgeTempFolder();
            }

            if (_getVersion)
            {
                Logging.Log($"Current FileAES-CLI Version: {GetVersion()}");
                if (!_getFaesVersion) return;
            }

            if (_getFaesVersion)
            {
                Logging.Log($"Current FAES Version: {FileAES_Utilities.GetVersion()}");
                return;
            }

            if (_showLatestVer)
            {
                if (_checkForUpdate)
                {
                    _update.SetBranch(_showLatestBranch);

                    string latestVer = _update.GetLatestVersion();

                    if (latestVer != "v0.0.0")
                        Logging.Log(
                            $"The latest FileAES-CLI version on branch '{_showLatestBranch.ToUpper()}' is: {latestVer}");
                    else
                        Logging.Log("The latest FileAES-CLI version could not be found! Please check your internet connection.", Severity.WARN);

                    _update.SetBranch(_updateBranch);
                }

                else
                    Logging.Log("The latest version cannot be displayed when the program is in offline mode!");

                return;
            }

            if (_getHint)
            {
                if (File.Exists(_directory) && FileAES_Utilities.IsFileDecryptable(_directory))
                {
                    string passHint = FileAES_Utilities.GetPasswordHint(_directory);

                    if (passHint != "No Password Hint Set")
                        Logging.Log($"The hint for '{Path.GetFileName(_directory)}' is: {passHint}");
                    else
                        Logging.Log($"'{Path.GetFileName(_directory)}' does not contain a password hint!");

                    if (string.IsNullOrEmpty(_password) && (!_getEncryptTimestamp || !_getEncryptCompression)) return;
                }
                else
                {
                    Logging.Log("You have not specified a valid encrypted file!", Severity.WARN);
                    return;
                }
            }

            if (_getEncryptTimestamp)
            {
                if (File.Exists(_directory) && FileAES_Utilities.IsFileDecryptable(_directory))
                {
                    long timestamp = FileAES_Utilities.GetEncryptionTimeStamp(_directory);

                    if (timestamp >= 0)
                    {
                        DateTime dateTime = FileAES_Utilities.UnixTimeStampToDateTime((double)timestamp);
                        Logging.Log(
                            $"'{Path.GetFileName(_directory)}' was encrypted on {dateTime.ToString("dd/MM/yyyy")} at {dateTime.ToString("HH:mm:ss tt")}.");
                    }
                    else
                    {
                        Logging.Log("This file does not contain a encryption date. This is likely due to this file being encrypted using an older FAES version.");
                    }

                    if (string.IsNullOrEmpty(_password) && !_getEncryptCompression) return;
                }
                else
                {
                    Logging.Log("You have not specified a valid encrypted file!", Severity.WARN);
                    return;
                }
            }

            if (_getEncryptCompression)
            {
                if (File.Exists(_directory) && FileAES_Utilities.IsFileDecryptable(_directory))
                {
                    string compressionMode = FileAES_Utilities.GetCompressionMode(_directory);

                    if (compressionMode != "LGYZIP")
                        Logging.Log(
                            $"The Compression Mode used for '{Path.GetFileName(_directory)}' is: {compressionMode}");
                    else
                        Logging.Log(
                            $"The Compression Mode used for '{Path.GetFileName(_directory)}' is: LGYZIP (LEGACYZIP)");

                    if (string.IsNullOrEmpty(_password)) return;
                }
                else
                {
                    Logging.Log("You have not specified a valid encrypted file!", Severity.WARN);
                    return;
                }
            }

            if (_showAllMetadata)
            {
                if (File.Exists(_directory) && FileAES_Utilities.IsFileDecryptable(_directory))
                {
                    string compressionMode = FileAES_Utilities.GetCompressionMode(_directory);

                    MetaData faesMetaData = new MetaData(_directory);

                    if (faesMetaData.IsLegacyVersion())
                        Logging.Log(
                            $"The metadata (bytes) for '{Path.GetFileName(_directory)}' is (FAESv2):\n{BitConverter.ToString(faesMetaData.GetMetaData())}");
                    else
                        Logging.Log(
                            $"The metadata (bytes) for '{Path.GetFileName(_directory)}' is (FAESv3):\n{BitConverter.ToString(faesMetaData.GetMetaData())}");

                    if (string.IsNullOrEmpty(_password)) return;
                }
                else
                {
                    Logging.Log("You have not specified a valid encrypted file!", Severity.WARN);
                    return;
                }
            }

            if (_showAllMetadataString)
            {
                if (File.Exists(_directory) && FileAES_Utilities.IsFileDecryptable(_directory))
                {
                    string compressionMode = FileAES_Utilities.GetCompressionMode(_directory);

                    MetaData faesMetaData = new MetaData(_directory);

                    if (faesMetaData.IsLegacyVersion())
                        Logging.Log(
                            $"The metadata (string) for '{Path.GetFileName(_directory)}' is (FAESv2):\n{Encoding.UTF8.GetString(faesMetaData.GetMetaData())}");
                    else
                        Logging.Log(
                            $"The metadata (string) for '{Path.GetFileName(_directory)}' is (FAESv3):\n{Encoding.UTF8.GetString(faesMetaData.GetMetaData())}");

                    if (string.IsNullOrEmpty(_password)) return;
                }
                else
                {
                    Logging.Log("You have not specified a valid encrypted file!", Severity.WARN);
                    return;
                }
            }

            if (string.IsNullOrEmpty(_directory))
            {
                while (true)
                {
                    Console.Write("File/Folder: ");
                    _directory = Console.ReadLine();

                    if (File.Exists(_directory) || Directory.Exists(_directory))
                    {
                        break;
                    }
                    Logging.Log("You have not specified a valid file or folder!", Severity.WARN);
                }
            }
            if (string.IsNullOrEmpty(_password))
            {
                while (true)
                {
                    Console.Write("Password: ");
                    string password = passwordInput();

                    if (new FAES_File(_directory).IsFileEncryptable())
                    {
                        Console.Write("\nConf. Password: ");
                        string passwordConf = passwordInput();

                        Console.Write(Environment.NewLine);

                        if (password == passwordConf)
                        {
                            _password = password;
                            break;
                        }
                        Logging.Log("Passwords do not match!");
                    }
                    else
                    {
                        Console.Write(Environment.NewLine);
                        _password = password;
                        break;
                    }
                }
            }

            if (!File.Exists(_directory) && !Directory.Exists(_directory))
            {
                Logging.Log("You have not specified a valid file or folder!", Severity.WARN);
            }
            else if (string.IsNullOrEmpty(_password))
            {
                Logging.Log("Please specify a password!", Severity.WARN);
            }
            else
            {
                FAES_File faesFile = new FAES_File(_directory);
                FileAES_Utilities.SetCryptoStreamBuffer(_csBuffer);

                try
                {
                    if (_compressionLevel < 0 || _compressionLevel > 9)
                    {
                        Logging.Log("You have not specified a valid compression level! Please choose a value between 0 and 9.", Severity.WARN);
                        return;
                    }
                    else
                    {
                        if (_verbose) Logging.Log(
                            $"CryptoStream Buffer Size: {FileAES_Utilities.GetCryptoStreamBuffer()} bytes", Severity.DEBUG);

                        if (faesFile.IsFileEncryptable())
                        {
                            FileAES_Utilities.LocalEncrypt = _useLocalEncrypt;
                            FileAES_Encrypt encrypt = new FileAES_Encrypt(faesFile, _password, _passwordHint, Optimise.Balanced, null, _deleteOriginalFile, _overwriteDuplicates);

                            if (!string.IsNullOrEmpty(_compressionMethod))
                            {
                                switch (_compressionMethod)
                                {
                                    case "ZIP":
                                        encrypt.SetCompressionMode(CompressionMode.ZIP, _compressionLevel);
                                        break;
                                    case "TAR":
                                        encrypt.SetCompressionMode(CompressionMode.TAR, _compressionLevel);
                                        break;
                                    case "LZMA":
                                        encrypt.SetCompressionMode(CompressionMode.LZMA, _compressionLevel);
                                        break;
                                    case "GZIP":
                                        encrypt.SetCompressionMode(CompressionMode.GZIP, _compressionLevel);
                                        break;
                                    case "LGYZIP":
                                    case "LEGACYZIP":
                                    case "LEGACY":
                                        encrypt.SetCompressionMode(CompressionMode.LGYZIP, _compressionLevel);
                                        break;
                                    default:
                                        Console.WriteLine("Unknown Compression Method: {0}", _compressionMethod);
                                        return;
                                }
                            }

                            Thread progressThread = new Thread(() =>
                            {
                                while (_showProgress)
                                {
                                    ushort percentComplete = Convert.ToUInt16(encrypt.GetEncryptionPercentComplete());
                                    Logging.Log($"Progress: {percentComplete}%");
                                    Thread.Sleep(_progressSleep);
                                }
                            });

                            Thread eThread = new Thread(() =>
                            {
                                try
                                {
                                    if (encrypt.EncryptFile())
                                    {
                                        if (_showProgress)
                                        {
                                            Logging.Log("Progress: 100%");
                                        }

                                        Logging.Log($"Encryption on {faesFile.getFaesType().ToLower()} succeeded!");
                                    }
                                    else
                                    {
                                        Logging.Log($"Encryption on {faesFile.getFaesType().ToLower()} failed!");
                                    }
                                }
                                catch (Exception e)
                                {
                                    #if NETFRAMEWORK
                                    progressThread.Abort();
                                    #endif
                                    HandleException(e);
                                }
                            });

                            if (_showProgress) progressThread.Start();
                            eThread.Start();

                            while (eThread.ThreadState == ThreadState.Running)
                            { }
                            #if NETFRAMEWORK
                            progressThread.Abort();
                            #endif
                        }
                        else
                        {
                            FileAES_Decrypt decrypt = new FileAES_Decrypt(faesFile, _password, _deleteOriginalFile, _overwriteDuplicates);

                            Thread progressThread = new Thread(() =>
                            {
                                while (_showProgress)
                                {
                                    ushort percentComplete = Convert.ToUInt16(decrypt.GetDecryptionPercentComplete());

                                    Logging.Log($"Progress: {percentComplete}%");
                                    Thread.Sleep(_progressSleep);
                                }
                            });

                            Thread dThread = new Thread(() =>
                            {
                                try
                                {
                                    if (decrypt.DecryptFile())
                                    {
                                        if (_showProgress)
                                        {
                                            Logging.Log("Progress: 100%");
                                        }

                                        Logging.Log($"Decryption on {faesFile.getFaesType().ToLower()} succeeded!");
                                    }
                                    else
                                    {
                                        Logging.Log($"Decryption on {faesFile.getFaesType().ToLower()} failed!");
                                        Logging.Log("Ensure that you entered the correct password!");
                                        Logging.Log($"Password Hint: {faesFile.GetPasswordHint()}");
                                    }
                                }
                                catch (Exception e)
                                {
                                    #if NETFRAMEWORK
                                    progressThread.Abort();
                                    #endif
                                    HandleException(e);
                                }
                            });

                            if (_showProgress) progressThread.Start();
                            dThread.Start();

                            while (dThread.ThreadState == ThreadState.Running)
                            { }
                            #if NETFRAMEWORK
                            progressThread.Abort();
                            #endif
                        }
                    }
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
            }
        }

        internal static void HandleException(Exception e)
        {
            if (!_verbose)
                Logging.Log(FileAES_Utilities.FAES_ExceptionHandling(e), Severity.ERROR);
            else
            {
                Logging.Log(
                    $"Verbose Mode Enabled: Showing Full Exception...\n{e.ToString()}\n\nConsole held open. Press any key to exit.", Severity.ERROR);
                Console.ReadKey();
            }
        }

        internal static string passwordInput()
        {
            ConsoleKeyInfo inf;
            StringBuilder input = new StringBuilder();
            inf = Console.ReadKey(true);
            while (inf.Key != ConsoleKey.Enter)
            {
                if (inf.Key == ConsoleKey.Backspace && input.Length > 0) input.Remove(input.Length - 1, 1);
                else input.Append(inf.KeyChar);
                inf = Console.ReadKey(true);
            }

            return input.ToString();
        }

        public static string GetVersion()
        {
            #pragma warning disable CS0162 //Unreachable code detected
            string[] ver = (typeof(FileAES_CLI.Program).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version).Split('.');
            if (_isDevBuild || _isBetaBuild)
                return "v" + ver[0] + "." + ver[1] + "." + ver[2] + "-" + _devBuildTag;
            else
                return "v" + ver[0] + "." + ver[1] + "." + ver[2];
            #pragma warning restore CS0162 //Unreachable code detected
        }

        public static bool IsVerbose()
        {
            return _verbose;
        }

        public static bool IsDevBuild()
        {
            return _isDevBuild;
        }

        public static bool IsBetaBuild()
        {
            return _isBetaBuild;
        }

        public static bool IsReleaseBuild()
        {
            return !(_isDevBuild || _isBetaBuild);
        }
    }
}
