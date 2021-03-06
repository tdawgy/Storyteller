using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Oakton;

namespace StoryTeller.Remotes
{

    public class SocketConnection : IDisposable, ISocketConnection
    {
        private readonly Action<Socket, string> _onReceived;
        private readonly Socket _listener;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private Task _receiveLoop;

        private Socket _sender;

        public SocketConnection(int port, bool owner, Action<Socket, string> onReceived)
        {
            _onReceived = onReceived;
                
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, port);

            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);

            if (owner)
            {
                _listener.Bind(localEndPoint);
                _listener.Listen(100);
            }
            else
            {
                _listener.Connect(localEndPoint);
                _sender = _listener;
            }

            startReceiving(owner);
        }

        public void startReceiving(bool owner)
        {
            if (owner)
            {
                _receiveLoop = Task.Run(() =>
                {
                    while (!_cancellation.IsCancellationRequested)
                    {
                        var handler = _listener.Accept();
                        _sender = handler;

                        Console.WriteLine("Accepted a new connection");

                        var stream = new NetworkStream(handler);
                        var reader = new BinaryReader(stream);

                        while (handler.Connected && !_cancellation.IsCancellationRequested)
                        {
                            try
                            {
                                var json = reader.ReadString();
                                _onReceived(_listener, json);
                            }
                            catch (SocketException e)
                            {
                                if (e.Message.Contains("An existing connection was forcibly closed by the remote host"))
                                {
                                    // do nothing
                                }
                            }
                            catch (EndOfStreamException)
                            {
                                // nothing, it's an artifact of a client shutting down
                                handler.Dispose();
                            }
                            catch (Exception e)
                            {
                                if (handler.Connected)
                                {
                                    ConsoleWriter.Write(ConsoleColor.Yellow, e.ToString());
                                    throw;
                                }
                            }
                        }

                        handler.Dispose();
                    }
                }, _cancellation.Token);
            }
            else
            {
                _receiveLoop = Task.Run(() =>
                {
                    var stream = new NetworkStream(_listener);
                    var reader = new BinaryReader(stream);

                    while (!_cancellation.IsCancellationRequested)
                    {
                        while (_listener.Connected && !_cancellation.IsCancellationRequested)
                        {
                            var json = reader.ReadString();
                            _onReceived(_listener, json);
                        }
                    }
                }, _cancellation.Token);
            }
        }


        public void SendMessage(string json)
        {
            if (_sender != null && _sender.Connected)
            {
                var stream = new NetworkStream(_sender);
                var writer = new BinaryWriter(stream);

                writer.Write(json);

                writer.Flush();
            }
        }

        public void Dispose()
        {
            _cancellation.Cancel();

#if NET46

            _receiveLoop.SafeDispose();
#endif
            try
            {
                _listener.Shutdown(SocketShutdown.Both);
                _listener.Dispose();
            }
            catch (Exception e)
            {
#if DEBUG
                ConsoleWriter.Write(ConsoleColor.Gray, e.ToString());
#endif
            }
        }
    }
}
