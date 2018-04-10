using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace QuickFileHost
{
    public class Server
    {
        private byte[] NOT_FOUND = Encoding.ASCII.GetBytes("Not Found");
        private byte[] DENIED = Encoding.ASCII.GetBytes("Denied");
        private byte[] GONE = Encoding.ASCII.GetBytes("Resource was Removed");

        private HttpListener _httpListener;
        private string[] _names;
        private byte[][] _files;
        private bool _hostsFolder = false;
        private string _folder;
        private bool _hasPassword;
        private string _password;

        public void Host(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough parameters, use 2 or more parameters");
                return;
            }
            var ip = GetLocalIPAddresses();
            if (ip.Count() == 0)
            {
                Console.WriteLine("No Local IP addresses found");
                return;
            }

            _hasPassword = args.Length > 2;
            if (_hasPassword)
                _password = args[2];

            _names = new string[_hasPassword ? args.Length - 2 : 1];
            _files = new byte[_hasPassword ? args.Length - 2 : 1][];
            if (!File.Exists(args[1]))
            {
                Console.WriteLine("File does not exist");
                return;
            }
            _names[0] = Path.GetFileName(args[1]);
            _files[0] = File.ReadAllBytes(args[1]);
            for (var i = 3; i < args.Length; i++)
            {
                if (!File.Exists(args[i]))
                {
                    Console.WriteLine("File does not exist");
                    return;
                }
                _names[i - 2] = Path.GetFileName(args[i]);
                _files[i - 2] = File.ReadAllBytes(args[i]);
            }

            SetupHttpListener(args[0], ip);
        }

        public void HostFolder(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough parameters, use 2 or more parameters");
                return;
            }
            var ip = GetLocalIPAddresses();
            if(ip.Count() == 0)
            {
                Console.WriteLine("No Local IP addresses found");
                return;
            }

            _hasPassword = args.Length > 2;
            if (_hasPassword)
                _password = args[2];

            if (!Directory.Exists(args[1]))
            {
                Console.WriteLine("Folder does not exist");
                return;
            }
            _hostsFolder = true;
            _folder = args[1];
            SetupHttpListener(args[0], ip);
        }

        private void SetupHttpListener(string port, IEnumerable<string> ip)
        {
            _httpListener = new HttpListener();
            var hostLocations = ip.Select(i => "http://" + i + ":" + port + "/");
            foreach (var location in hostLocations)
                _httpListener.Prefixes.Add(location);
            try
            {
                _httpListener.Start();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 5)
                    Console.WriteLine("Windows: Access Denied");
                else if (ex.NativeErrorCode == 32)
                    Console.WriteLine("Port " + port + " is unavailable.");
                return;
            }
            new Thread(ListenToRequests).Start();
            foreach(var location in hostLocations)
                Console.WriteLine("Hosting files at " + location);
        }

        private void ListenToRequests()
        {
            while (true)
            {
                HttpListenerContext context = _httpListener.GetContext();
                new Thread(() => ResponseToRequest(context)).Start();
            }
        }

        private void ResponseToRequest(HttpListenerContext context)
        {
            Console.WriteLine("Incoming request from " + context.Request.RemoteEndPoint.ToString());
            try
            {
                if (!_hasPassword || _password.Equals(context.Request.Headers.Get("Authorization")))
                {
                    Console.WriteLine("Accepted");
                    var url = context.Request.Url.LocalPath;
                    if (!_hostsFolder)
                    {
                        if (url == "/" || url == "")
                        {
                            Console.WriteLine("Index: 0");
                            context.Response.ContentType = "application/force-download";
                            context.Response.AddHeader("content-disposition", "attachment;    filename=" + _names[0]);
                            context.Response.OutputStream.Write(_files[0], 0, _files[0].Length);
                        }
                        else if (url.Length < 10 && IsDigitsOnly(url.Substring(1)))
                        {
                            var index = uint.Parse(url.Substring(1));
                            if (index < _files.Length)
                            {
                                Console.WriteLine("Index: " + url.Substring(1));
                                context.Response.ContentType = "application/force-download";
                                context.Response.AddHeader("content-disposition", "attachment;    filename=" + _names[index]);
                                context.Response.OutputStream.Write(_files[index], 0, _files[index].Length);
                            }
                            else
                            {
                                context.Response.StatusCode = 404;
                                context.Response.OutputStream.Write(NOT_FOUND, 0, NOT_FOUND.Length);
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            context.Response.OutputStream.Write(NOT_FOUND, 0, NOT_FOUND.Length);
                        }
                    }
                    else
                    {
                        if (new Regex("\\.\\.(\\/|$)").Match(url).Success)
                        {
                            //Since HttpListener unexpectedly responds to tree traversal attacks, it probably won't get here
                            Console.WriteLine("Path Traversal Attack Prevented");
                            context.Response.StatusCode = 401;
                            context.Response.OutputStream.Write(DENIED, 0, DENIED.Length);
                        }
                        else if (File.Exists(_folder + url))
                        {
                            Console.WriteLine("Relative Path: " + url);
                            try
                            {
                                var file = File.ReadAllBytes(_folder + url);
                                context.Response.ContentType = "application/force-download";
                                context.Response.AddHeader("content-disposition", "attachment;    filename=" + url.Substring(url.LastIndexOf("/" + 1)));
                                context.Response.OutputStream.Write(file, 0, file.Length);
                            }
                            catch
                            {
                                context.Response.StatusCode = 410;
                                context.Response.OutputStream.Write(GONE, 0, GONE.Length);
                            }
                        }
                        else if (Directory.Exists(_folder + url))
                        {
                            Console.WriteLine("Relative Path: " + url);
                            try
                            {
                                var entries = Directory.GetFileSystemEntries(_folder + url);
                                entries = entries.Select(e => e.Substring(Math.Max(e.LastIndexOf("\\"), e.LastIndexOf("/")) + 1)).ToArray();
                                var entriesAsString = string.Join("\r\n", entries);
                                var entriesAsBytes = Encoding.ASCII.GetBytes(entriesAsString);
                                context.Response.OutputStream.Write(entriesAsBytes, 0, entriesAsBytes.Length);
                            }
                            catch
                            {
                                context.Response.StatusCode = 410;
                                context.Response.OutputStream.Write(GONE, 0, GONE.Length);
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            context.Response.OutputStream.Write(NOT_FOUND, 0, NOT_FOUND.Length);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Denied");
                    context.Response.StatusCode = 401;
                    context.Response.OutputStream.Write(DENIED, 0, DENIED.Length);
                }
            }
            catch
            {
                Console.WriteLine("Aborted");
            }
            context.Response.KeepAlive = false;
            context.Response.Close();
            Console.WriteLine("Request Finished");
        }

        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        private IEnumerable<string> GetLocalIPAddresses()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    yield return ip.ToString();
        }
    }
}
