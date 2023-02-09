using Microsoft.Win32;
using Spectre.Console;
using System;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            AnsiConsole.MarkupLine("[yellow]]Only windows platform supported[/]");
            Console.ReadKey();
        }

        Menu();
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

            string host = null;
            string port;
            var valueObj = Key.GetValue(_proxyServerKey);
            string strValue = "";
            if (valueObj != null)
            {
                strValue = valueObj.ToString();
                host = strValue.Split(":")[0];
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                host = "[red]is empty[/]";
            }

            AnsiConsole.MarkupLine($"[yellow]Host {host}[/]");

            if (strValue.Split(":").Length > 1)
                port = strValue.Split(":")[1];
            else
                port = "[red]is empty[/]";

            AnsiConsole.MarkupLine($"[yellow]Port {port}[/]");
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
    /// Display menu
    /// </summary>
    /// <param name="args"></param>
    static void Menu()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        AnsiConsole.MarkupLine($"[yellow]ProxySwitcher {version}[/]");
        Console.WriteLine();
        ShowStatus();
        Console.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Menu[/]");
        string choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
        .HighlightStyle(new Style(Color.Yellow))
            .AddChoices(new[] {
                "Proxy On",
                "Proxy Off",
                "Change host url",
                "Change port",
                "Help",
                "Exit"
            }));

        Console.Clear();

        switch (choice)
        {
            case "Proxy On":
                On();
                break;
            case "Proxy Off":
                Off();
                break;
            case "Change host url":
                string host = AnsiConsole.Ask<string>("[yellow]Enter your host:[/]");
                SetHost(host);
                break;
            case "Change port":
                string port = AnsiConsole.Ask<string>("[yellow]Enter your port:[/]");
                SetPort(port);
                break;
            case "Exit":
                Environment.Exit(0);
                break;
        }
    }
}