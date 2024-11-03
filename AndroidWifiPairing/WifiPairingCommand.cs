using Makaretu.Dns;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace AndroidWifiPairing
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class WifiPairingCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1096f233-40f3-4559-aa1a-ac770634cecb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="WifiPairingCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private WifiPairingCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static WifiPairingCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in WifiPairingCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new WifiPairingCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            WifiPairing wifiPairing = new WifiPairing();
            wifiPairing.Show();
        }

        private void MDNSTest()
        {
            var mdns = new MulticastService();
            var serviceDiscovery = new ServiceDiscovery(mdns);

            mdns.NetworkInterfaceDiscovered += (s, ex) =>
            {
                foreach (var nic in ex.NetworkInterfaces)
                {
                    Console.WriteLine($"NIC '{nic.Name}'");
                }

                // Ask for the name of all services.
                mdns.SendQuery("_adb._tcp.local", type: DnsType.PTR);

            };

            mdns.AnswerReceived += (s, en) =>
            {
                // Is this an answer to a service instance details?
                var servers = en.Message.Answers.OfType<SRVRecord>();
                foreach (var server in servers)
                {
                    Console.WriteLine($"host '{server.Target}' for '{server.Name}'");

                    // Ask for the host IP addresses.
                    mdns.SendQuery(server.Target, type: DnsType.A);
                    mdns.SendQuery(server.Target, type: DnsType.AAAA);
                }

                // Is this an answer to host addresses?
                var addresses = en.Message.Answers.OfType<AddressRecord>();
                foreach (var address in addresses)
                {
                    Console.WriteLine($"host '{address.Name}' at {address.Address}");
                }

            };

            // Event handler for when a service is found
            serviceDiscovery.ServiceInstanceDiscovered += (s, ev) =>
            {
                if (ev.ServiceInstanceName.ToString().Contains("_adb._tcp.local"))
                {
                    // Get IP address and port for the discovered ADB service
                    var adbService = ev.Message.Answers.OfType<SRVRecord>().FirstOrDefault();
                    var addresses = ev.Message.AdditionalRecords.OfType<ARecord>();

                    if (adbService != null && addresses.Any())
                    {
                        var ipAddress = addresses.First().Address.ToString();
                        var port = adbService.Port;
                        Console.WriteLine($"Found ADB Device - IP: {ipAddress}, Port: {port}");

                    }
                }
            };

            // Start browsing for _adb._tcp.local services on the network
            //    serviceDiscovery.QueryServiceInstances("_adb._tcp.local");
            //serviceDiscovery.QueryServiceInstances(null);
            //    serviceDiscovery.QueryAllServices();
            mdns.Start();
            Console.WriteLine("Searching for Android devices with Wi-Fi debugging enabled...");
            Console.ReadLine(); // Keep the app running to listen for mDNS responses
        }
    }
}
