using CommandLine.Text;
using CommandLine;
using Microsoft.Win32;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public class Options
    {
        [Option('s', "show status", Required = false, HelpText = "show status of proxy")]
        public bool ShowStatus { get; set; }

        [Option('c', "change status", Required = false, HelpText = "change status of proxy (on/off)")]
        public string ChangeStatus { get; set; }

        [Option('h', "host", Required = false, HelpText = "Set proxy host")]
        public string Host { get; set; }

        [Option('p', "port", Required = false, HelpText = "Set proxy port")]
        public string Port { get; set; }
    }

    static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            AnsiConsole.MarkupLine("[yellow]]Only windows platform supported[/]");
            Console.ReadKey();
        }

        var parser = new CommandLine.Parser(with => { with.AutoHelp = false; with.AutoVersion = false; });
        var parserResult = parser.ParseArguments<Options>(args);

        if (!args.Any())
            DisplayHelp(parserResult, null);

        parserResult.WithParsed(p =>
                   {
                       if (!string.IsNullOrWhiteSpace(p.ChangeStatus))
                       {
                           switch (p.ChangeStatus.ToLower().Trim())
                           {
                               case "on":
                                   On();
                                   break;
                               case "off":
                                   Off();
                                   break;
                           }
                           ShowStatus();
                       }

                       if (p.Host != null)
                       {
                           SetHost(p.Host);
                       }

                       if (p.Port != null)
                       {
                           SetPort(p.Port);
                       }

                       if (p.ShowStatus)
                       {
                           ShowStatus();
                       }

                   }).WithNotParsed(err => DisplayHelp(parserResult, err));
    }

    /// <summary>
    /// Registry keys
    /// </summary>
    const string _proxyServerKey = "ProxyServer";
    const string _proxyEnableKey = "ProxyEnable";

    /// <summary>
    /// Set proxy one
    /// </summary>
    static void On()
    {
        RegistryKey Key = GetRegKey();
        if (Key != null)
        {
            Key.SetValue(_proxyEnableKey, "1", RegistryValueKind.DWord);
        }
        Key.Close();
    }

    /// <summary>
    /// Set proxy off
    /// </summary>
    static void Off()
    {
        RegistryKey Key = GetRegKey();
        if (Key != null)
        {
            Key.SetValue(_proxyEnableKey, "0", RegistryValueKind.DWord);
        }
        Key.Close();
    }

    /// <summary>
    /// Initialize Registry key and value
    /// </summary>
    /// <returns></returns>
    static object ProxyServerInit()
    {
        RegistryKey Key = GetRegKey();

        var value = Key.GetValue(_proxyServerKey);

        if (value == null)
        {
            value = "";
        }
        Key.Close();
        return value;
    }

    /// <summary>
    /// Change host url
    /// </summary>
    static void SetHost(string host)
    {
        var value = ProxyServerInit();
        var arr = value.ToString().Split(':');
        string port = arr.Length > 1 ? arr[1] : "";

        Store(host, port);
    }

    /// <summary>
    /// Change port url
    /// </summary>
    static void SetPort(string port)
    {
        var value = ProxyServerInit();
        var arr = value.ToString().Split(':');
        string host = arr.Length > 0 ? arr[0] : "";

        Store(host, port);
    }

    /// <summary>
    /// Store proxy changes
    /// </summary>
    static void Store(string host, string port)
    {
        RegistryKey Key = GetRegKey();

        if (string.IsNullOrWhiteSpace(host) && string.IsNullOrWhiteSpace(port))
        {
            Key.DeleteValue("ProxyServer");
            return;
        }

        string value = string.IsNullOrWhiteSpace(port) ? host : $"{host}:{port}";
        Key.SetValue(_proxyServerKey, value, RegistryValueKind.String);
        Key.Close();
    }

    /// <summary>
    /// Get status of proxy config
    /// </summary>
    static void ShowStatus()
    {
        RegistryKey Key = GetRegKey();
        if (Key != null)
        {
            var statusObj = Key.GetValue(_proxyEnableKey);

            string status = "Off";
            if (statusObj != null)
            {
                status = statusObj.ToString() == "1" ? "[green]On[/]" : "[red]Off[/]";
            }
            AnsiConsole.MarkupLine($"[yellow]Status[/] {status}");


            var hostObj = Key.GetValue(_proxyServerKey);
            string host = hostObj != null ? hostObj.ToString() : "";
            AnsiConsole.MarkupLine($"[yellow]Host {host.Split(":")[0]}[/]");

            if (host.Split(":").Length > 1)
            {
                AnsiConsole.MarkupLine($"[yellow]Port {host.Split(":")[1]}[/]");
            }
        }
        Key.Close();
    }

    /// <summary>
    /// Get registry key
    /// </summary>
    static RegistryKey GetRegKey()
    {
        return Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
    }

    /// <summary>
    /// Display help
    /// </summary>
    static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
    {
        AnsiConsole.Write(
          new FigletText("Proxy Switcher").Centered()
              .Color(Color.Red));

        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.AutoVersion = false;
            h.Heading = "ProxySwitcher help";
            h.Copyright = "Copyright (c) github.com/HoseinHabibiyan/ProxySwitcher";
            return h;
        }, _ => _);
        AnsiConsole.MarkupLine($"[yellow]{helpText}[/]");
    }
}