using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using FAES;
using FAES.Packaging;

namespace FileAES_CLI
{
    class Program
    {
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
        private static string _directory = null;
        private static string _passwordHint = null;
        private static string _password;
        private static string _compressionMethod = null;
        private static int _compressionLevel = 7;
        private static ushort _progressSleep = 5000;
        private static uint _csBuffer = FileAES_Utilities.GetCryptoStreamBuffer();
        private static List<string> _strippedArgs = new List<string>();

        private const bool _isDevBuild = true;
        private const string _devBuildTag = "DEV_190419-1";

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string strippedArg = args[i].ToLower();

                if (Directory.Exists(args[i])) _directory = args[i];
                else if (File.Exists(args[i])) _directory = args[i];

                strippedArg = strippedArg.TrimStart('-', '/', '\\');

                if (strippedArg == "verbose" || strippedArg == "v") _verbose = true;
                else if (String.IsNullOrEmpty(_password) && (strippedArg == "password" || strippedArg == "p") && !string.IsNullOrEmpty(args[i + 1])) _password = args[i + 1];
                else if (strippedArg == "purgetemp" || strippedArg == "deletetemp") _purgeTemp = true;
                else if (strippedArg == "help") _help = true;
                else if (String.IsNullOrEmpty(_passwordHint) && (strippedArg == "hint" || strippedArg == "passwordhint" || strippedArg == "h") && !string.IsNullOrEmpty(args[i + 1])) _passwordHint = args[i + 1];
                else if (strippedArg == "gethint" || strippedArg == "getpasswordhint") _getHint = true;
                else if (strippedArg == "gettimestamp" || strippedArg == "timestamp" || strippedArg == "encryptiondate") _getEncryptTimestamp = true;
                else if (strippedArg == "getcompression" || strippedArg == "getcompressionmethod") _getEncryptCompression = true;
                else if (strippedArg == "showprogress" || strippedArg == "progress" || strippedArg == "prog")
                {
                    if (!string.IsNullOrEmpty(args[i + 1]) && UInt16.TryParse(args[i + 1], out _progressSleep)) { }
                    _showProgress = true;
                }
                else if (strippedArg == "faesversion" || strippedArg == "faes" || strippedArg == "faesver") _getFaesVersion = true;
                else if (strippedArg == "faescliversion" || strippedArg == "faescliver" || strippedArg == "faescli" || strippedArg == "cliver" || strippedArg == "ver")
                {
                    _getVersion = true;
                    _getFaesVersion = true;
                }
                else if (String.IsNullOrEmpty(_compressionMethod) && (strippedArg == "compression" || strippedArg == "compressionmethod" || strippedArg == "c") && !string.IsNullOrEmpty(args[i + 1])) _compressionMethod = args[i + 1].ToUpper();
                else if ((strippedArg == "level" || strippedArg == "compressionlevel" || strippedArg == "l") && !string.IsNullOrEmpty(args[i + 1])) Int32.TryParse(args[i + 1], out _compressionLevel);
                else if (strippedArg == "buffer" || strippedArg == "cryptostreambuffer" || strippedArg == "csbuffer" && !string.IsNullOrEmpty(args[i + 1])) UInt32.TryParse(args[i + 1], out _csBuffer);
                else if (strippedArg == "overwrite" || strippedArg == "overwriteduplicates" || strippedArg == "o") _overwriteDuplicates = true;
                else if (strippedArg == "preserveoriginal" || strippedArg == "original" || strippedArg == "po") _deleteOriginalFile = false;

                _strippedArgs.Add(strippedArg);
            }

            if (_help)
            {
                Console.WriteLine("A FAES-based tool for encrypting and decrypting files using the command-line.\n\nPossible Launch Parameters:\n'--verbose' or '-v': Show more debugging information in the console (WIP)." +
                    "\n'--purgeTemp' or '-p': Purge the FileAES Temp folder to resolve possible errors.\n'--password <password>' or '-p <password>': Sets the password that will be used to encrypt/decrypt the file/folder." +
                    "\n'--hint <Password Hint>' or '-h <Password Hint>': Sets a password hint.\n'--compression <ZIP/TAR/LZMA/LEGACYZIP>' or '-c <ZIP/TAR/LZMA/LEGACYZIP>': Sets the compression method that will be used to encrypt the file/folder." +
                    "\n'--level <0-9>' or '-l <0-9>': Sets the compression level that will be used to encrypt the file/folder. (Only works for with the ZIP compression method)" +
                    ".\n'--getHint': Gets the password hint for the encrypted file.\n'--getTimestamp': Gets the encryption timestamp of the encrypted file." +
                    "\n'--getCompression': Gets the compression method of the encrypted file.\n'--ver': Gets the current version of FileAES-CLI and FAES being used." +
                    "\n'--FAES': Gets the current version of FAES being used.\n'--overwrite' or '-o': Overwrites any duplicate files found within the FAES process." +
                    "\n'--original' or '-po': Preserves the original file used in the encrypt/decrypt process." +
                    "\n'--progress <Polling Rate (ms)>': Outputs the current encryption/decryption progress to the console after desired time (Leaving polling rate blank defaults to 5000ms)." +
                    "\n'--buffer <Size (bytes)>': Sets the size of the FAES CryptoStream buffer.\n\n" +
                    "File/Folder names can be entered as a launch parameter to select what to encrypt/decrypt (also allows for dragging/dropping a file/folder on the .exe).\n\n" +
                    "Example: 'FileAES-CLI.exe File.txt -p password123'");
                return;
            }

            if (_purgeTemp)
            {
                FileAES_Utilities.PurgeTempFolder();
            }

            if (_getVersion)
            {
                Console.WriteLine("Current FileAES-CLI Version: {0}", GetVersion());
                if (!_getFaesVersion) return;
            }

            if (_getFaesVersion)
            {
                Console.WriteLine("Current FAES Version: {0}", FileAES_Utilities.GetVersion());
                return;
            }

            if (_getHint)
            {
                if (File.Exists(_directory) && FileAES_Utilities.isFileDecryptable(_directory))
                {
                    string passHint = FileAES_Utilities.GetPasswordHint(_directory);

                    if (passHint != "No Password Hint Set")
                        Console.WriteLine("The hint for '{0}' is: {1}", Path.GetFileName(_directory), passHint);
                    else
                        Console.WriteLine("'{0}' does not contain a password hint!", Path.GetFileName(_directory));

                    if (String.IsNullOrEmpty(_password) && (!_getEncryptTimestamp || !_getEncryptCompression)) return;
                }
                else
                {
                    Console.WriteLine("You have not specified a valid encrypted file!");
                    return;
                }
            }

            if(_getEncryptTimestamp)
            {
                if (File.Exists(_directory) && FileAES_Utilities.isFileDecryptable(_directory))
                {
                    int timestamp = FileAES_Utilities.GetEncryptionTimeStamp(_directory);

                    if (timestamp >= 0)
                    {
                        DateTime dateTime = FileAES_Utilities.UnixTimeStampToDateTime((double)timestamp);
                        Console.WriteLine("'{0}' was encrypted on {1} at {2}.", Path.GetFileName(_directory), dateTime.ToString("dd/MM/yyyy"), dateTime.ToString("HH:mm:ss tt"));
                    }
                    else
                    {
                        Console.WriteLine("This file does not contain a encryption date. This is likely due to this file being encrypted using an older FAES version.");
                    }

                    if (String.IsNullOrEmpty(_password) && !_getEncryptCompression) return;
                }
                else
                {
                    Console.WriteLine("You have not specified a valid encrypted file!");
                    return;
                }
            }

            if (_getEncryptCompression)
            {
                if (File.Exists(_directory) && FileAES_Utilities.isFileDecryptable(_directory))
                {
                    string compressionMode = FileAES_Utilities.GetCompressionMode(_directory);

                    if (compressionMode != "LGYZIP")
                        Console.WriteLine("The Compression Mode used for '{0}' is: {1}", Path.GetFileName(_directory), compressionMode);
                    else
                        Console.WriteLine("The Compression Mode used for '{0}' is: LGYZIP (LEGACYZIP)", Path.GetFileName(_directory));

                    if (String.IsNullOrEmpty(_password)) return;
                }
                else
                {
                    Console.WriteLine("You have not specified a valid encrypted file!");
                    return;
                }
            }

            if (String.IsNullOrEmpty(_directory))
            {
                while (true)
                {
                    Console.Write("File/Folder: ");
                    _directory = Console.ReadLine();

                    if (File.Exists(_directory) || Directory.Exists(_directory))
                    {
                        break;
                    }
                    Console.WriteLine("You have not specified a valid file or folder!");
                }
            }
            if (String.IsNullOrEmpty(_password))
            {
                while (true)
                {
                    Console.Write("Password: ");
                    string password = passwordInput();

                    if (new FAES_File(_directory).isFileEncryptable())
                    {
                        Console.Write("\nConf. Password: ");
                        string passwordConf = passwordInput();

                        Console.Write(Environment.NewLine);

                        if (password == passwordConf)
                        {
                            _password = password;
                            break;
                        }
                        Console.WriteLine("Passwords do not match!");
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
                Console.WriteLine("You have not specified a valid file or folder!");
            }
            else if (String.IsNullOrEmpty(_password))
            {
                Console.WriteLine("Please specify a password!");
            }
            else
            {
                FAES_File faesFile = new FAES_File(_directory);
                FileAES_Utilities.SetVerboseLogging(_verbose);
                FileAES_Utilities.SetCryptoStreamBuffer(_csBuffer);

                try
                {
                    if (_compressionLevel < 0 || _compressionLevel > 9)
                    {
                        Console.WriteLine("You have not specified a valid compression level! Please choose a value between 0 and 9.");
                        return;
                    }
                    else
                    {
                        if (_verbose) Console.WriteLine("[DEBUG] CryptoStream Buffer Size: {0} bytes", FileAES_Utilities.GetCryptoStreamBuffer());

                        if (faesFile.isFileEncryptable())
                        {
                            FileAES_Encrypt encrypt = new FileAES_Encrypt(faesFile, _password, _passwordHint, Optimise.Balanced, null, _deleteOriginalFile, _overwriteDuplicates);

                            if (!String.IsNullOrEmpty(_compressionMethod))
                            {
                                switch (_compressionMethod)
                                {
                                    case "ZIP":
                                        {
                                            encrypt.SetCompressionMode(CompressionMode.ZIP, _compressionLevel);
                                            break;
                                        }
                                    case "TAR":
                                        {
                                            encrypt.SetCompressionMode(CompressionMode.TAR, _compressionLevel);
                                            break;
                                        }
                                    case "LZMA":
                                        {
                                            encrypt.SetCompressionMode(CompressionMode.LZMA, _compressionLevel);
                                            break;
                                        }
                                    case "LGYZIP":
                                    case "LEGACYZIP":
                                    case "LEGACY":
                                        {
                                            encrypt.SetCompressionMode(CompressionMode.LGYZIP, _compressionLevel);
                                            break;
                                        }
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
                                    if (_verbose) Console.WriteLine("[INFO] Progress: {0}%", percentComplete);
                                    else Console.WriteLine("Progress: {0}%", percentComplete);
                                    Thread.Sleep(_progressSleep);
                                }
                            });

                            Thread eThread = new Thread(() =>
                            {
                                try
                                {
                                    if (encrypt.encryptFile())
                                    {
                                        if (_showProgress)
                                        {
                                            if (_verbose) Console.WriteLine("[INFO] Progress: 100%");
                                            else Console.WriteLine("Progress: 100%");
                                        }

                                        Console.WriteLine("Encryption on {0} succeeded!", faesFile.getFaesType().ToLower());
                                    }
                                    else
                                    {
                                        Console.WriteLine("Encryption on {0} failed!", faesFile.getFaesType().ToLower());
                                    }
                                }
                                catch (Exception e)
                                {
                                    progressThread.Abort();
                                    HandleException(e);
                                }
                            });

                            if (_showProgress) progressThread.Start();
                            eThread.Start();

                            while (eThread.ThreadState == ThreadState.Running)
                            { }

                            progressThread.Abort();
                        }
                        else
                        {
                            FileAES_Decrypt decrypt = new FileAES_Decrypt(faesFile, _password, _deleteOriginalFile, _overwriteDuplicates);

                            Thread progressThread = new Thread(() =>
                            {
                                while (_showProgress)
                                {
                                    ushort percentComplete = Convert.ToUInt16(decrypt.GetDecryptionPercentComplete());

                                    if (_verbose) Console.WriteLine("[INFO] Progress: {0}%", percentComplete);
                                    else Console.WriteLine("Progress: {0}%", percentComplete);
                                    Thread.Sleep(_progressSleep);
                                }
                            });

                            Thread dThread = new Thread(() =>
                            {
                                try
                                {
                                    if (decrypt.decryptFile())
                                    {
                                        if (_showProgress)
                                        {
                                            if (_verbose) Console.WriteLine("[INFO] Progress: 100%");
                                            else Console.WriteLine("Progress: 100%");
                                        }

                                        Console.WriteLine("Decryption on {0} succeeded!", faesFile.getFaesType().ToLower());
                                    }
                                    else
                                    {
                                        Console.WriteLine("Decryption on {0} failed!", faesFile.getFaesType().ToLower());
                                        Console.WriteLine("Ensure that you entered the correct password!");
                                        Console.WriteLine("Password Hint: {0}", faesFile.GetPasswordHint());
                                    }
                                }
                                catch (Exception e)
                                {
                                    progressThread.Abort();
                                    HandleException(e);
                                }
                            });

                            if (_showProgress) progressThread.Start();
                            dThread.Start();

                            while (dThread.ThreadState == ThreadState.Running)
                            { }

                            progressThread.Abort();
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
                Console.WriteLine(FileAES_Utilities.FAES_ExceptionHandling(e));
            else
            {
                Console.WriteLine("[ERROR] Verbose Mode Enabled: Showing Full Exception...\n");
                Console.WriteLine(e.ToString());
                Console.WriteLine("\n\nConsole held open. Press any key to exit.");
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
            if (_isDevBuild)
                return "v" + ver[0] + "." + ver[1] + "." + ver[2] + "-" + _devBuildTag;
            else
                return "v" + ver[0] + "." + ver[1] + "." + ver[2];
            #pragma warning restore CS0162 //Unreachable code detected
        }
    }
}
