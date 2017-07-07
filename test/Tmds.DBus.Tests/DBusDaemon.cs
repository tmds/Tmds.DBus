using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    public enum DBusDaemonProtocol
    {
        Unix,
        UnixAbstract,
        Tcp,
        Default = Unix
    }
    class DBusDaemon : IDisposable
    {
        // On an SELinux system the DBus daemon gets the security context
        // of the peer: the call 'getsockopt(fd, SOL_SOCKET, SO_PEERSEC, ...)'
        // returns ENOPROTOOPT and the daemon closes the connection.
        public static readonly bool IsSELinux = Directory.Exists("/sys/fs/selinux");

        private static string s_config =
 @"<!DOCTYPE busconfig PUBLIC ""-//freedesktop//DTD D-Bus Bus Configuration 1.0//EN""
 ""http://www.freedesktop.org/standards/dbus/1.0/busconfig.dtd"">
<busconfig>
  <type>session</type>
  <allow_anonymous/>
  <auth>ANONYMOUS</auth>
  <listen>$LISTEN</listen>
  <policy context='default'>
    <allow user='*'/>
    <allow own='*'/>
    <allow send_type='method_call'/>
    <allow send_type='signal'/>
    <allow send_requested_reply='true' send_type='method_return'/>
    <allow send_requested_reply='true' send_type='error'/>
    <allow receive_type='method_call'/>
    <allow receive_type='method_return'/>
    <allow receive_type='error'/>
    <allow receive_type='signal'/>
  </policy>
  <apparmor mode='disabled'/>
  <limit name='reply_timeout'>30000</limit>
</busconfig>";
        private enum State
        {
            Created,
            Started,
            Disposed
        }
        private Process _process;
        private string _configFile;
        private State _state;

        public DBusDaemon()
        {
            _state = State.Created;
        }

        public string Address { get; private set; }

        public void Dispose()
        {
            if (_configFile != null)
            {
                File.Delete(_configFile);
            }
            _state = State.Disposed;
            _process?.Kill();
            _process?.Dispose();
        }

        public Task StartAsync(DBusDaemonProtocol protocol = DBusDaemonProtocol.Default)
        {
            if (_state != State.Created)
            {
                throw new InvalidOperationException("Daemon has already been started or is disposed");
            }
            _state = State.Started;

            _configFile = Path.GetTempFileName();
            if (protocol == DBusDaemonProtocol.Unix)
            {
                var socketPath = Path.GetTempFileName();
                File.Delete(socketPath);
                File.WriteAllText(_configFile, s_config.Replace("$LISTEN", $"unix:path={socketPath}"));
            }
            else if (protocol == DBusDaemonProtocol.UnixAbstract)
            {
                var socketPath = Path.GetTempFileName();
                File.Delete(socketPath);
                File.WriteAllText(_configFile, s_config.Replace("$LISTEN", $"unix:abstract={socketPath}"));
            }
            else // DBusDaemonProtocol.Tcp
            {
                File.WriteAllText(_configFile, s_config.Replace("$LISTEN", "tcp:host=localhost,port=0"));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dbus-daemon",
                Arguments = $"--config-file={_configFile} --print-address",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            var tcs = new TaskCompletionSource<bool>();
            _process = new Process() { StartInfo = startInfo };
            _process.OutputDataReceived += (sender, dataArgs) =>
                {
                    if (Address == null)
                    {
                        Address = dataArgs.Data;
                        tcs.SetResult(true);
                    }
                };
            _process.Start();
            _process.BeginOutputReadLine();
            return tcs.Task;
        }
    }
}