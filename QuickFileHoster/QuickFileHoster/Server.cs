﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QuickFileHoster
{
    public class Server
    {
        private byte[] NOT_FOUND = Encoding.UTF8.GetBytes("Not Found");
        private byte[] DENIED = Encoding.UTF8.GetBytes("Denied");
        private string ERRORED = "";

        private HttpListener _httpListener;
        private string[] _names;
        private byte[][] _files;
        private bool _hasPassword;
        private string _password;

        public void Go(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough parameters, use 2 or more parameters");
                return;
            }
            var ip = GetLocalIPAddress();
            if (ip == ERRORED)
            {
                Console.WriteLine("Local IP address not found");
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

            _httpListener = new HttpListener();
            var host = "http://" + ip + ":" + args[0] + "/";
            _httpListener.Prefixes.Add(host);
            _httpListener.Start();
            new Thread(ListenToRequests).Start();
            Console.WriteLine("Hosting files at " + host);
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
            if (!_hasPassword || _password.Equals(context.Request.Headers.Get("Authorization")))
            {
                Console.WriteLine("Accepted");
                var url = context.Request.Url.AbsolutePath;
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
                        context.Response.AddHeader("content-disposition", "attachment;    filename=" + _names[0]);
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
                Console.WriteLine("Denied");
                context.Response.StatusCode = 401;
                context.Response.OutputStream.Write(DENIED, 0, DENIED.Length);
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

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            return ERRORED;
        }
    }
}
