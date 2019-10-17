using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocksRelayServer.Relay
{
    internal class SocketRelay
    {
        private bool _receiving;
        private SocketRelay _other;

        private SocketAsyncEventArgs _recSaea;
        private SocketAsyncEventArgs _sendSaea;
        private Socket _source;
        private Socket _target;
        private byte[] _buffer;

        private int _received;
        private int _sendingOffset;

        private bool _disposed = false;
        private bool _shouldDispose = false;

        private SocketRelay(Socket source, Socket target)
        {
            _source = source;
            _target = target;
            _buffer = new byte[81920];
            _recSaea = new SocketAsyncEventArgs { UserToken = this };
            _sendSaea = new SocketAsyncEventArgs { UserToken = this };
            _recSaea.SetBuffer(_buffer, 0, _buffer.Length);
            _sendSaea.SetBuffer(_buffer, 0, _buffer.Length);
            _recSaea.Completed += OnAsyncOperationCompleted;
            _sendSaea.Completed += OnAsyncOperationCompleted;
            _receiving = true;
        }

        public static void RelayBiDirectionally(Socket s1, Socket s2)
        {
            var relayOne = new SocketRelay(s1, s2);
            var relayTwo = new SocketRelay(s2, s1);

            relayOne._other = relayTwo;
            relayTwo._other = relayOne;

            Task.Run(new Action(relayOne.Process));
            Task.Run(new Action(relayTwo.Process));
        }

        private static void OnAsyncOperationCompleted(object o, SocketAsyncEventArgs saea)
        {
            if (saea.UserToken is SocketRelay relay)
            {
                relay.Process();
            }
        }

        private void OnCleanup()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = _shouldDispose = true;

            _other._shouldDispose = true;
            _other = null;

            _source.TryDispose();
            _target.TryDispose();
            _recSaea.TryDispose();
            _sendSaea.TryDispose();

            _source = _target = null;
            _recSaea = _sendSaea = null;
            _buffer = null;
        }

        private void Process()
        {
            try
            {
                while (true)
                {
                    if (_shouldDispose)
                    {
                        OnCleanup();
                        return;
                    }

                    if (_receiving)
                    {
                        _receiving = false;
                        _sendingOffset = -1;

                        if (_source.ReceiveAsync(_recSaea))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (_sendingOffset == -1)
                        {
                            _received = _recSaea.BytesTransferred;
                            _sendingOffset = 0;

                            if (_received == 0)
                            {
                                _shouldDispose = true;
                                continue;
                            }
                        }
                        else
                        {
                            _sendingOffset += _sendSaea.BytesTransferred;
                        }

                        if (_sendingOffset != _received)
                        {
                            _sendSaea.SetBuffer(_buffer, _sendingOffset, _received - _sendingOffset);

                            if (_target.SendAsync(_sendSaea))
                            {
                                return;
                            }
                        }
                        else
                        {
                            _receiving = true;
                        }
                    }
                }
            }
            catch
            {
                OnCleanup();
            }
        }
    }
}