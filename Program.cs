using Microsoft.Win32;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        Console.CancelKeyPress += OnExit;
        AppDomain.CurrentDomain.ProcessExit += OnExit;

        Menu();
        On();
    }

    /// <summary>
    /// Registry keys
    /// </summary>
    const string _proxyServerKey = "ProxyServer";
    const string _proxyEnableKey = "ProxyEnable";
    static string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

    /// <summary>
    /// Set proxy one
    /// </summary>
    static void On()
    {
        SetFromDefaultConfig();
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

        SetToRegistry(host, port);
    }

    /// <summary>
    /// Change port url
    /// </summary>
    static void SetPort(string port)
    {
        var value = ProxyServerInit();
        var arr = value.ToString().Split(':');
        string host = arr.Length > 0 ? arr[0] : "";

        SetToRegistry(host, port);
    }

    /// <summary>
    /// Store proxy changes
    /// </summary>
    static void SetToRegistry(string host, string port)
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
    /// Store default config
    /// </summary>
    static void DefineDefault()
    {
        var config = ReadConfig();

        foreach (var item in config)
        {
            AnsiConsole.MarkupLine($"[yellow]{item.Key} {item.Value}[/]");
        }
        Console.WriteLine();

        string host = AnsiConsole.Ask<string>("[yellow]Enter your host:[/]");
        string port = AnsiConsole.Ask<string>("[yellow]Enter your port:[/]");

        StoreConfig(new Dictionary<string, string>
        {
            { "default host", host },
            { "default port", port }
        });
    }

    /// <summary>
    /// Set proxy from default config
    /// </summary>
    static void SetFromDefaultConfig()
    {
        var config = ReadConfig();

        if (!config.Any())
        {
            AnsiConsole.MarkupLine($"[red]Default host and port is not set[/]");
            AnsiConsole.MarkupLine($"[yellow]Press any key to back...[/]");
            Console.ReadKey();
            return;
        }

        if (config["default host"] != null)
        {
            SetHost(config["default host"]);
        }

        if (config["default port"] != null)
        {
            SetPort(config["default port"]);
        }

        ShowStatus();
    }

    /// <summary>
    /// Store config to file
    /// </summary>
    static void StoreConfig(Dictionary<string, string> input)
    {
        var config = ReadConfig();

        foreach (var item in input)
        {
            config[item.Key] = item.Value;
        }

        File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"), config.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray());
    }

    /// <summary>
    /// Read from config file
    /// </summary>
    static Dictionary<string, string> ReadConfig()
    {
        if (!File.Exists(_configPath)) return new Dictionary<string, string>();
        string[] config = File.ReadAllLines(_configPath);
        var dict = config.Select(l => l.Split('=')).ToDictionary(c => c[0], c => c[1]);
        return dict;
    }

    /// <summary>
    /// Display menu
    /// </summary>
    static void Menu()
    {
        Console.Clear();
        AnsiConsole.Write(
         new FigletText($"Proxy Switcher")
         .Centered()
         .Color(Color.DeepSkyBlue3));

        string version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        AnsiConsole.MarkupLine($"[DeepSkyBlue3]v{version}[/]");
        AnsiConsole.MarkupLine($"[DeepSkyBlue3]github.com/HoseinHabibiyan/ProxySwitcher[/]");
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
                "Set from default config",
                "Define default",
                "Change host url",
                "Change port",
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
            case "Set from default config":
                SetFromDefaultConfig();
                break;
            case "Define default":
                DefineDefault();
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
        Menu();
    }

    /// <summary>
    /// On exit set proxy off
    /// </summary>
    static void OnExit(object sender, EventArgs e)
    {
        Off();
    }
}